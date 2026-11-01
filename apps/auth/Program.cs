using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

var isMigration = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));

    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.SetAuthorizationEndpointUris("/connect/authorize");

        options.AllowAuthorizationCodeFlow()
            .RequireProofKeyForCodeExchange();

        options.AllowClientCredentialsFlow();

        options.AcceptAnonymousClients();

        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        var instance = options.UseAspNetCore();
        if (builder.Environment.IsDevelopment())
        {
            instance.DisableTransportSecurityRequirement();
        }
        instance
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

if (!isMigration)
{
    AddBuilderServices(builder.Services, builder.Configuration);
}

var app = builder.Build();

if (isMigration)
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "service-worker",
            ClientType = ClientTypes.Public,
            DisplayName = "Service Worker",
        };

        descriptor.RedirectUris.Add(new Uri("http://localhost:5118/callback"));
        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);

        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);

        descriptor.Permissions.Add(Permissions.Scopes.Profile);

        var swaggerDescriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "swagger-ui",
            ClientType = ClientTypes.Public,
            DisplayName = "Swagger UI"
        };

        swaggerDescriptor.RedirectUris.Add(
            new Uri("http://localhost:5260/swagger/oauth2-redirect.html"));

        swaggerDescriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        swaggerDescriptor.Permissions.Add(Permissions.Endpoints.Token);

        swaggerDescriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        swaggerDescriptor.Permissions.Add(Permissions.ResponseTypes.Code);

        var application = await manager.FindByClientIdAsync(descriptor.ClientId);
        if (application is null)
        {
            await manager.CreateAsync(descriptor);
        }
        else
        {
            await manager.UpdateAsync(application, descriptor);
        }

        application = await manager.FindByClientIdAsync(swaggerDescriptor.ClientId);
        if (application is null)
        {
            await manager.CreateAsync(swaggerDescriptor);
        }
        else
        {
            await manager.UpdateAsync(application, swaggerDescriptor);
        }
    }
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1");
        options.OAuthClientId("swagger-ui");
        options.OAuthUsePkce();
        options.OAuthScopes("openid");
    });

}

app.UseDeveloperExceptionPage();

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors(options =>
{
    options.WithOrigins("http://localhost:5118")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/login", (HttpContext context) =>
{
    var returnUrl = context.Request.Query["ReturnUrl"].ToString();
    if (!IsLocalReturnUrl(returnUrl))
    {
        returnUrl = "/";
    }

    return Results.Content($"""
        <!doctype html>
        <html>
        <head><title>Log in</title></head>
        <body>
            <h1>Log in</h1>
            <form method="post" action="/login">
                <input type="hidden" name="returnUrl"
                       value="{System.Net.WebUtility.HtmlEncode(returnUrl)}">
                <label>
                    Username
                    <input name="userName" autocomplete="username" required>
                </label>
                <br>
                <label>
                    Password
                    <input name="password" type="password"
                           autocomplete="current-password" required>
                </label>
                <br>
                <label>
                    <input name="rememberMe" type="checkbox">
                    Remember me
                </label>
                <br>
                <button type="submit">Log in</button>
            </form>
        </body>
        </html>
        """, "text/html");
});

app.MapPost("/login", async (
    HttpContext context,
    SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var returnUrl = form["returnUrl"].ToString();
    if (!IsLocalReturnUrl(returnUrl))
    {
        return Results.BadRequest("Invalid return URL.");
    }

    var result = await signInManager.PasswordSignInAsync(
        form["userName"].ToString(),
        form["password"].ToString(),
        string.Equals(form["rememberMe"], "on", StringComparison.Ordinal),
        lockoutOnFailure: true);

    if (!result.Succeeded)
    {
        return Results.Content(
            "<h1>Login failed</h1><p>Invalid username or password.</p>",
            "text/html",
            statusCode: StatusCodes.Status401Unauthorized);
    }

    return Results.Redirect(returnUrl);
});

app.MapControllers();

await app.RunAsync();

static void AddBuilderServices(
    IServiceCollection services,
    IConfiguration configuration)
{
    services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();
    services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/login";
    });

    var issuer = configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Missing Jwt:Issuer");

    var audience = configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Missing Jwt:Audience");

    var signingKey = configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("Missing Jwt:SigningKey");

    services.AddAuthentication(options =>
    {
        options.DefaultScheme =
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    });

    services.AddAuthorization();

    services.AddControllers();
    services.AddProblemDetails();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("OpenIddict", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Sign in with the OpenIddict authorization code flow.",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(
                        "http://localhost:5260/connect/authorize"),
                    TokenUrl = new Uri("http://localhost:5260/connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "Sign in"
                    }
                }
            }
        });

        options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            [new OpenApiSecuritySchemeReference("OpenIddict", document)] = []
        });
    });

    services.AddScoped<IJwtService, JwtService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ISecurityService, SecurityService>();
    services.AddScoped<IEmailServiceClient, EmailServiceClient>();
}

static bool IsLocalReturnUrl(string returnUrl)
{
    return returnUrl.StartsWith("/", StringComparison.Ordinal) &&
        !returnUrl.StartsWith("//", StringComparison.Ordinal) &&
        !returnUrl.StartsWith("/\\", StringComparison.Ordinal);
}

public partial class Program { }
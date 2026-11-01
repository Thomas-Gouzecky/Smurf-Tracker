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

        var application = await manager.FindByClientIdAsync(descriptor.ClientId);
        if (application is null)
        {
            await manager.CreateAsync(descriptor);
        }
        else
        {
            await manager.UpdateAsync(application, descriptor);
        }
    }
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

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
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token."
        });

        options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });

    services.AddScoped<IJwtService, JwtService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ISecurityService, SecurityService>();
    services.AddScoped<IEmailServiceClient, EmailServiceClient>();
}

public partial class Program { }
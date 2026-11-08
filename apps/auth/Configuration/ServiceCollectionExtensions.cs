using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Server;
using Microsoft.Extensions.Options;
using static OpenIddict.Abstractions.OpenIddictConstants;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"));

            options.UseOpenIddict();
        });

        return services;
    }

    public static IServiceCollection AddOpenIddictServices(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token");
                options.SetAuthorizationEndpointUris("/connect/authorize");
                options.SetEndSessionEndpointUris("/connect/logout");

                options.AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange();

                options.AllowClientCredentialsFlow();
                options.AcceptAnonymousClients();

                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                var instance = options.UseAspNetCore();
                if (environment.IsDevelopment())
                {
                    instance.DisableTransportSecurityRequirement();
                }

                instance
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddSingleton<OpenIddictScopeCatalog>();
        services.AddSingleton<IConfigureOptions<OpenIddictServerOptions>,
            DatabaseOpenIddictScopeOptions>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
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
            options.Events.OnSigningIn = async context =>
            {
                var userId = context.Principal?.FindFirstValue(
                    ClaimTypes.NameIdentifier);
                if (userId is null)
                {
                    throw new InvalidOperationException(
                        "A user ID is required to create an authentication session.");
                }

                var sessionService = context.HttpContext.RequestServices
                    .GetRequiredService<ISessionService>();
                var sessionId = await sessionService.CreateAsync(
                    userId,
                    "identity-cookie",
                    context.HttpContext.Request.Headers.UserAgent.ToString(),
                    context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    context.HttpContext.RequestAborted);

                context.Principal!.AddIdentity(new ClaimsIdentity(
                [
                    new Claim(SessionClaimTypes.SessionId, sessionId.ToString())
                ]));
            };
        });

        // _ = configuration["Jwt:Issuer"]
        //     ?? throw new InvalidOperationException("Missing Jwt:Issuer");
        // _ = configuration["Jwt:Audience"]
        //     ?? throw new InvalidOperationException("Missing Jwt:Audience");
        // _ = configuration["Jwt:SigningKey"]
        //     ?? throw new InvalidOperationException("Missing Jwt:SigningKey");

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
            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "Sign in with the OpenIddict authorization code flow.",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(
                            "http://localhost:5260/connect/authorize"),
                        TokenUrl = new Uri(
                            "http://localhost:5260/connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            ["openid"] = "Sign in"
                        }
                    }
                }
            };
            options.AddSecurityDefinition("OpenIddict", securityScheme);

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
        services.AddScoped<ISessionService, SessionService>();

        return services;
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenIddict.Validation.AspNetCore;
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
        IWebHostEnvironment environment)
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
                    .EnableTokenEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

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
                        TokenUrl = new Uri(
                            "http://localhost:5260/connect/token"),
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

        return services;
    }
}

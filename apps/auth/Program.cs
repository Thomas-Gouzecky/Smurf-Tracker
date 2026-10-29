using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var isMigration = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

if (!isMigration)
{
    AddBuilderServices(builder.Services, builder.Configuration);
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
}

if (isMigration)
    return;

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void AddBuilderServices(
    IServiceCollection services,
    IConfiguration configuration)
{
    services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager();

    var issuer = configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("Missing Jwt:Issuer");

    var audience = configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Missing Jwt:Audience");

    var signingKey = configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("Missing Jwt:SigningKey");

    services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultScheme =
                JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(signingKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    services.AddAuthorization();

    services.AddControllers();
    services.AddProblemDetails();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    // Register your JWT creation service here.
}

public partial class Program { }
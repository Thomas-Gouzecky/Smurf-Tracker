using Microsoft.EntityFrameworkCore;
using auth;

var builder = WebApplication.CreateBuilder(args);
var isMigration = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);

builder.Services.AddApplicationDatabase(builder.Configuration);
builder.Services.AddOpenIddictServices(
    builder.Environment,
    builder.Configuration);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

if (!isMigration)
{
    builder.Services.AddApplicationServices(builder.Configuration);
}

var app = builder.Build();

if (isMigration)
{
    await app.SeedOpenIddictApplicationsAsync();
    return;
}

app.UseApplicationPipeline();

app.MapLoginEndpoints();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

public partial class Program { }

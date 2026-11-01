using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var isMigration = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);

builder.Services.AddApplicationDatabase(builder.Configuration);
builder.Services.AddOpenIddictServices(builder.Environment);

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

if (app.Environment.IsDevelopment())
{
    app.MapLoginEndpoints();
}
app.MapControllers();

await app.RunAsync();

public partial class Program { }

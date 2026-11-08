using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

public static class OpenIddictApplicationSeeder
{
    public static async Task SeedOpenIddictApplicationsAsync(
        this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }

        var manager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictApplicationManager>();

        var scopes = OpenIddictScopes.GetRequiredDefinitions(app.Configuration);
        await UpsertAsync(manager, CreateServiceWorkerDescriptor(scopes));
        await UpsertAsync(manager, CreateSwaggerDescriptor(scopes));
        var scopeManager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictScopeManager>();
        await RegisterScopesAsync(scopeManager, scopes);
    }

    private static OpenIddictApplicationDescriptor CreateServiceWorkerDescriptor(
        OpenIddictScopesOptions scopes)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "service-worker",
            ClientType = ClientTypes.Public,
            DisplayName = "Service Worker"
        };

        descriptor.RedirectUris.Add(new Uri("http://localhost:5118/callback"));
        AddAuthorizationCodePermissions(descriptor);
        descriptor.Permissions.Add(Permissions.Scopes.Profile);
        AddScopePermissions(descriptor, scopes);

        return descriptor;
    }

    private static OpenIddictApplicationDescriptor CreateSwaggerDescriptor(
        OpenIddictScopesOptions scopes)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = "swagger-ui",
            ClientType = ClientTypes.Public,
            DisplayName = "Swagger UI"
        };

        descriptor.RedirectUris.Add(
            new Uri("http://localhost:5260/swagger/oauth2-redirect.html"));
        descriptor.PostLogoutRedirectUris.Add(
            new Uri("https://localhost:5260/swagger/"));
        AddAuthorizationCodePermissions(descriptor);
        AddScopePermissions(descriptor, scopes);

        return descriptor;
    }

    private static void AddAuthorizationCodePermissions(
        OpenIddictApplicationDescriptor descriptor)
    {
        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
        descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
    }

    private static void AddScopePermissions(
        OpenIddictApplicationDescriptor descriptor,
        OpenIddictScopesOptions scopes)
    {
        foreach (var scope in scopes.Definitions)
        {
            descriptor.Permissions.Add(
                Permissions.Prefixes.Scope + scope.Name);
        }
    }

    private static async Task UpsertAsync(
        IOpenIddictApplicationManager manager,
        OpenIddictApplicationDescriptor descriptor)
    {
        var application = await manager.FindByClientIdAsync(descriptor.ClientId!);
        if (application is null)
        {
            await manager.CreateAsync(descriptor);
        }
        else
        {
            await manager.UpdateAsync(application, descriptor);
        }
    }

    private static async Task RegisterScopesAsync(
        IOpenIddictScopeManager manager,
        OpenIddictScopesOptions options)
    {
        var descriptors = OpenIddictScopes.GetDescriptors(options);
        foreach (var descriptor in descriptors)
        {
            var scope = await manager.FindByNameAsync(descriptor.Name!);
            if (scope is null)
            {
                await manager.CreateAsync(descriptor);
            }
            else
            {
                await manager.UpdateAsync(scope, descriptor);
            }
        }
    }
}

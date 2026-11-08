using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;

public sealed class OpenIddictScopeCatalog
{
    private IReadOnlyList<string> _names = [];

    public IReadOnlyList<string> Names => _names;

    public void SetNames(IEnumerable<string> names)
    {
        _names = names.ToArray();
    }
}

public sealed class DatabaseOpenIddictScopeOptions
    : IConfigureOptions<OpenIddictServerOptions>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OpenIddictScopeCatalog _catalog;

    public DatabaseOpenIddictScopeOptions(
        IServiceScopeFactory scopeFactory,
        OpenIddictScopeCatalog catalog)
    {
        _scopeFactory = scopeFactory;
        _catalog = catalog;
    }

    public void Configure(OpenIddictServerOptions options)
    {
        using var scope = _scopeFactory.CreateScope();
        var manager = scope.ServiceProvider
            .GetRequiredService<IOpenIddictScopeManager>();
        var names = new List<string>();

        LoadNamesAsync(manager, names).GetAwaiter().GetResult();
        if (names.Count == 0)
        {
            throw new InvalidOperationException(
                "No OpenIddict scopes were found in the database.");
        }

        foreach (var name in names)
        {
            options.Scopes.Add(name);
        }
        _catalog.SetNames(names);
    }

    private static async Task LoadNamesAsync(
        IOpenIddictScopeManager manager,
        ICollection<string> names)
    {
        await foreach (var scope in manager.ListAsync())
        {
            var name = await manager.GetNameAsync(scope);
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }
        }
    }
}

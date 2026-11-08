using OpenIddict.Abstractions;

public sealed class OpenIddictScopesOptions
{
    public List<OpenIddictScopeOptions> Scopes { get; set; } = [];
}

public sealed class OpenIddictScopeOptions
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Resources { get; set; } = [];
}

public static class OpenIddictScopes
{
    public static OpenIddictScopesOptions GetRequired(IConfiguration configuration)
    {
        var options = configuration
            .GetRequiredSection("OpenIddict")
            .Get<OpenIddictScopesOptions>()
            ?? throw new InvalidOperationException(
                "The OpenIddict configuration is invalid.");

        if (options.Scopes.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one OpenIddict scope must be configured.");
        }

        if (options.Scopes.Any(scope =>
            string.IsNullOrWhiteSpace(scope.Name) ||
            string.IsNullOrWhiteSpace(scope.DisplayName) ||
            scope.Resources.Count == 0 ||
            scope.Resources.Any(string.IsNullOrWhiteSpace)))
        {
            throw new InvalidOperationException(
                "Every OpenIddict scope must have a name, display name, and resource.");
        }

        if (options.Scopes
            .GroupBy(scope => scope.Name, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "OpenIddict scope names must be unique.");
        }

        return options;
    }

    public static IEnumerable<OpenIddictScopeDescriptor> GetDescriptors(
        OpenIddictScopesOptions options)
    {
        return options.Scopes.Select(scope =>
        {
            var descriptor = new OpenIddictScopeDescriptor
            {
                Name = scope.Name,
                DisplayName = scope.DisplayName
            };
            foreach (var resource in scope.Resources)
            {
                descriptor.Resources.Add(resource);
            }

            return descriptor;
        });
    }
}

using OpenIddict.Abstractions;

public sealed class OpenIddictScopesOptions
{
    public List<OpenIddictScopeOptions> Definitions { get; set; } = [];
}

public sealed class OpenIddictScopeOptions
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Resources { get; set; } = [];
}

public static class OpenIddictScopes
{
    public static OpenIddictScopesOptions GetRequiredDefinitions(
        IConfiguration configuration)
    {
        var options = configuration
            .GetRequiredSection("OpenIddict")
            .Get<OpenIddictScopesOptions>()
            ?? throw new InvalidOperationException(
                "The OpenIddict scope definitions configuration is invalid.");

        if (options.Definitions.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one OpenIddict scope definition must be configured.");
        }

        if (options.Definitions.Any(scope =>
            string.IsNullOrWhiteSpace(scope.Name) ||
            string.IsNullOrWhiteSpace(scope.DisplayName) ||
            scope.Resources.Count == 0 ||
            scope.Resources.Any(string.IsNullOrWhiteSpace)))
        {
            throw new InvalidOperationException(
                "Every OpenIddict scope must have a name, display name, and resource.");
        }

        if (options.Definitions
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
        return options.Definitions.Select(scope =>
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

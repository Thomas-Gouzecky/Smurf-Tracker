using OpenIddict.Abstractions;

public static class OpenIddictScopes
{
    public const string Read = "smurftracker-read";
    public const string Write = "smurftracker-write";
    public const string Resource = "smurftracker-api";

    public static IEnumerable<OpenIddictScopeDescriptor> GetScopes()
    {
        yield return CreateSmurfTrackerReadScope();
        yield return CreateSmurfTrackerWriteScope();
    }

    private static OpenIddictScopeDescriptor CreateSmurfTrackerReadScope()
    {
        return new OpenIddictScopeDescriptor
        {
            Name = Read,
            DisplayName = "Read SmurfTracker data",
            Resources =
        {
            Resource
        }
        };
    }

    private static OpenIddictScopeDescriptor CreateSmurfTrackerWriteScope()
    {
        return new OpenIddictScopeDescriptor
        {
            Name = Write,
            DisplayName = "Write SmurfTracker data",
            Resources =
        {
            Resource
        }
        };
    }
} 
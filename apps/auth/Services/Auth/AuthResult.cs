public sealed record AuthError(
    string Code,
    string Description,
    string InputField = "general");

public sealed record AuthResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    IReadOnlyCollection<AuthError>? Errors = null,
    string? UserId = null,
    string? Username = null)
{
    public static AuthResult Success(string userId, string username) =>
        new(
            true,
            Errors: Array.Empty<AuthError>(),
            UserId: userId,
            Username: username);

    public static AuthResult Success() => new(true, Errors: Array.Empty<AuthError>());

    public static AuthResult Failure(string errorMessage) =>
        new(false, errorMessage, Array.Empty<AuthError>());

    public static AuthResult Failure(IEnumerable<AuthError> errors) =>
        new(
            false,
            string.Join(", ", errors.Select(error => error.Description)),
            errors.ToArray());
}
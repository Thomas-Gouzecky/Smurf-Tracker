public sealed record AuthError(
    string Code,
    string Description,
    string InputField = "general");

public sealed record AuthResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    IReadOnlyCollection<AuthError>? Errors = null,
    string? UserId = null,
    string? Username = null,
    string? AccessToken = null,
    string? TokenType = null,
    int? ExpiresIn = null)
{
    public static AuthResult Success(string accessToken, string tokenType = "Bearer", int expiresIn = 3600) =>
        new(
            true,
            Errors: Array.Empty<AuthError>(),
            AccessToken: accessToken,
            TokenType: tokenType,
            ExpiresIn: expiresIn);

    public static AuthResult Success() => new(true, Errors: Array.Empty<AuthError>());

    public static AuthResult Failure(string errorMessage) =>
        new(false, errorMessage, Array.Empty<AuthError>());

    public static AuthResult Failure(IEnumerable<AuthError> errors) =>
        new(
            false,
            string.Join(", ", errors.Select(error => error.Description)),
            errors.ToArray());
}
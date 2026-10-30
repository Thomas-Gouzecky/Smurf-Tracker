public sealed class ResendVerificationEmailRequest
{
    /// <summary>
    /// The email address for the user requesting email verification. This should match the email of the user.
    /// </summary>
    public required string Email { get; init; }
}
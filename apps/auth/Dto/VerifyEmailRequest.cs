public sealed class VerifyEmailRequest
{
    /// <summary>
    /// The email address for the user requesting email verification. This should match the email of the user.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// The code sent to the user's email to verify the email address. To get the verification code, first make a "/register" request.
    /// </summary>
    public required string VerificationCode { get; init; }
}
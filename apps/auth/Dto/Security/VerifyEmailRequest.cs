public sealed class VerifyEmailRequest
{
    /// <summary>
    /// The code sent to the user's email to verify the email address. To get the verification code, first make a "/register" request.
    /// </summary>
    public required string VerificationCode { get; init; }
}
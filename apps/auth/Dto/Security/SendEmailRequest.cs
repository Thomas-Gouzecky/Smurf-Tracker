using System.Text.Json.Serialization;

public sealed class SendEmailRequest
{
    /// <summary>
    /// The email address to send the email to.
    /// </summary>
    [JsonPropertyName("to")]
    public required string To { get; init; }

    /// <summary>
    /// The template to use for the email. This should correspond to a template that the email service recognizes.
    /// </summary>
    [JsonPropertyName("template")]
    public required string Template { get; init; }

    /// <summary>
    /// The data for the template. This should be a JSON object that the email service can use to populate the template.
    /// </summary>
    [JsonPropertyName("data")]
    public required EmailData Data { get; init; }

    /// <summary>
    /// The subject of the email. This is used if the template does not specify a subject.
    /// </summary>
    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    /// <summary>
    /// The body of the email. This is used if the template does not specify a body.
    /// </summary>
    [JsonPropertyName("body")]
    public required string Body { get; init; }
}
public sealed class EmailData
{
    /// <summary>
    /// The name of the user. This can be used in the email template to personalize the email.
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; init; }

    /// <summary>
    /// The verification link to include in the email. This is used for email verification purposes.
    /// </summary>
    [JsonPropertyName("verification_link")]
    public string? VerificationLink { get; init; }

    /// <summary>
    /// The password reset link to include in the email. This is used for password reset purposes.
    /// </summary>
    [JsonPropertyName("reset_link")]
    public string? ResetLink { get; init; }

    /// <summary>
    /// The expiration time of the verification or reset link. This can be used to inform the user of how long the link is valid for.
    /// </summary>
    [JsonPropertyName("expiration_time")]
    public string? ExpirationTime { get; init; }
}

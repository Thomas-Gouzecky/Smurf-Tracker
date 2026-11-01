public sealed class SendEmailRequest
{
    /// <summary>
    /// The email address to send the email to.
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// The template to use for the email. This should correspond to a template that the email service recognizes.
    /// </summary>
    public required string template { get; init; }

    /// <summary>
    /// The data for the template. This should be a JSON object that the email service can use to populate the template.
    /// </summary>
    public required object data { get; init; }
}
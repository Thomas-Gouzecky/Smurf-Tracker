public sealed class EmailServiceClient : IEmailServiceClient
{
    public EmailServiceClient()
    {
    }

    public async Task<bool> SendEmailAsync(SendEmailRequest request)
    {
        // Implement the logic to send an email using the external email service API.
        return await Task.FromResult(true);
    }
}
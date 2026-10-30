public sealed class EmailServiceClient : IEmailServiceClient
{
    private readonly HttpClient _httpClient;

    public EmailServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> SendEmailAsync(SendEmailRequest request)
    {
        // Implement the logic to send an email using the external email service API.
        return await Task.FromResult(true);
    }
}
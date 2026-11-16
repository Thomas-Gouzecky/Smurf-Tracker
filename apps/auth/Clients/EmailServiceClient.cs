public sealed class EmailServiceClient : IEmailServiceClient
{
    private readonly HttpClient _emailServiceHttpClient;

    public EmailServiceClient(IHttpClientFactory httpClientFactory)
    {
        _emailServiceHttpClient = httpClientFactory.CreateClient("EmailService");
    }

    public async Task<HttpResponseMessage> SendEmailAsync(SendEmailRequest request)
    {
        var response = await _emailServiceHttpClient.PostAsJsonAsync("/email/send", request);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
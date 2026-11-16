public interface IEmailServiceClient
{
    Task<HttpResponseMessage> SendEmailAsync(SendEmailRequest request);
}
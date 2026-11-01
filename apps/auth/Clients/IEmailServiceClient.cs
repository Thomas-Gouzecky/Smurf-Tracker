public interface IEmailServiceClient
{
    Task<bool> SendEmailAsync(SendEmailRequest request);
}
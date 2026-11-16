public interface IEmailServiceClient
{
    Task<object> SendEmailAsync(SendEmailRequest request);
}
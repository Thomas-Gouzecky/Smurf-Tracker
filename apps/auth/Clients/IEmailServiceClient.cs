public interface IEmailServiceClient
{
    Task<bool> SendEmailAsync();
}
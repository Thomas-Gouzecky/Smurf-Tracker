public interface IOutboxService
{
    Task AddMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
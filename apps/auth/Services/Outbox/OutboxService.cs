public class OutboxService : IOutboxService
{
    private readonly ApplicationDbContext _dbContext;

    public OutboxService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddMessageAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        message.Id = Guid.NewGuid();
        message.CreatedAt = DateTime.UtcNow;
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

}
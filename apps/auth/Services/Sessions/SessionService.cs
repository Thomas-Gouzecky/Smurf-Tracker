using Microsoft.EntityFrameworkCore;

public sealed class SessionService : ISessionService
{
    private readonly ApplicationDbContext _dbContext;

    public SessionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> CreateAsync(
        string userId,
        string clientId,
        string? deviceName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ClientId = clientId,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        };

        _dbContext.UserSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return session.Id;
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _dbContext.UserSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId && session.RevokedAt == null)
            .ToListAsync(cancellationToken);

        return sessions
            .OrderByDescending(session => session.LastActivityAt ?? session.CreatedAt)
            .ToList();
    }

    public async Task<bool> RevokeAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions
            .SingleOrDefaultAsync(
                item => item.Id == sessionId &&
                    item.UserId == userId &&
                    item.RevokedAt == null,
                cancellationToken);

        if (session is null)
        {
            return false;
        }

        session.RevokedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _dbContext.UserSessions
            .Where(session => session.UserId == userId && session.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var revokedAt = DateTimeOffset.UtcNow;
        foreach (var session in sessions)
        {
            session.RevokedAt = revokedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> IsActiveAsync(
        Guid sessionId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserSessions.AnyAsync(
            session => session.Id == sessionId &&
                session.UserId == userId &&
                session.RevokedAt == null,
            cancellationToken);
    }
}

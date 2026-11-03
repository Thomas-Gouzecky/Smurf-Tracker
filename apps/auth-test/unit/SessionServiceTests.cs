using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace auth_test.unit;

public sealed class SessionServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;
    private readonly SessionService _service;

    public SessionServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();
        _dbContext.Users.AddRange(
            new ApplicationUser { Id = "user-1", UserName = "user-1" },
            new ApplicationUser { Id = "user-2", UserName = "user-2" });
        _dbContext.SaveChanges();
        _service = new SessionService(_dbContext);
    }

    [Fact]
    public async Task CreateAsync_PersistsSessionMetadata()
    {
        var createdAtBefore = DateTimeOffset.UtcNow;

        var sessionId = await _service.CreateAsync(
            "user-1",
            "web",
            "Chrome on Windows",
            "192.0.2.1");

        var session = await _dbContext.UserSessions
            .SingleAsync(session => session.Id == sessionId);

        Assert.Equal("user-1", session.UserId);
        Assert.Equal("web", session.ClientId);
        Assert.Equal("Chrome on Windows", session.DeviceName);
        Assert.Equal("192.0.2.1", session.IpAddress);
        Assert.Null(session.RevokedAt);
        Assert.True(session.CreatedAt >= createdAtBefore);
        Assert.NotNull(session.LastActivityAt);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveSessionsForUser()
    {
        var activeSessionId = await _service.CreateAsync(
            "user-1", "web", "Active", null);
        var revokedSessionId = await _service.CreateAsync(
            "user-1", "mobile", "Revoked", null);
        await _service.CreateAsync("user-2", "web", "Other user", null);

        await _service.RevokeAsync("user-1", revokedSessionId);

        var sessions = await _service.GetActiveAsync("user-1");

        var session = Assert.Single(sessions);
        Assert.Equal(activeSessionId, session.Id);
    }

    [Fact]
    public async Task RevokeAsync_OnlyRevokesSessionOwnedByUser()
    {
        var sessionId = await _service.CreateAsync(
            "user-1", "web", "Device", null);

        Assert.False(await _service.RevokeAsync("user-2", sessionId));
        Assert.True(await _service.IsActiveAsync(sessionId, "user-1"));

        Assert.True(await _service.RevokeAsync("user-1", sessionId));
        Assert.False(await _service.IsActiveAsync(sessionId, "user-1"));

        var session = await _dbContext.UserSessions
            .SingleAsync(session => session.Id == sessionId);
        Assert.NotNull(session.RevokedAt);
    }

    [Fact]
    public async Task RevokeAllAsync_RevokesAllActiveSessionsWithoutAffectingOtherUsers()
    {
        var firstSessionId = await _service.CreateAsync(
            "user-1", "web", "First", null);
        var secondSessionId = await _service.CreateAsync(
            "user-1", "mobile", "Second", null);
        var otherUserSessionId = await _service.CreateAsync(
            "user-2", "web", "Other user", null);

        await _service.RevokeAllAsync("user-1");

        Assert.False(await _service.IsActiveAsync(firstSessionId, "user-1"));
        Assert.False(await _service.IsActiveAsync(secondSessionId, "user-1"));
        Assert.True(await _service.IsActiveAsync(otherUserSessionId, "user-2"));
        Assert.Empty(await _service.GetActiveAsync("user-1"));
    }

    [Fact]
    public async Task RevokeAsync_WhenSessionDoesNotExist_ReturnsFalse()
    {
        Assert.False(await _service.RevokeAsync("user-1", Guid.NewGuid()));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}

using System.Security.Claims;

public interface ISessionService
{
    Task<Guid> CreateAsync(
        string userId,
        string clientId,
        string? deviceName,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSession>> GetActiveAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task RevokeAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(
        Guid sessionId,
        string userId,
        CancellationToken cancellationToken = default);

    static string? GetSessionId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(SessionClaimTypes.SessionId);
}

public static class SessionClaimTypes
{
    public const string SessionId = "session_id";
}

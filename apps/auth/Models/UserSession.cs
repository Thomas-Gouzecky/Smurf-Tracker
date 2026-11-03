public class UserSession
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? OpenIddictAuthorizationId { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
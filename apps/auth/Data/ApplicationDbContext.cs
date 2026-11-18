using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseOpenIddict();

        builder.Entity<UserSession>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.Property(session => session.UserId).IsRequired();
            entity.Property(session => session.ClientId).IsRequired();
            entity.HasIndex(session => new { session.UserId, session.RevokedAt });
            entity.HasOne(session => session.User)
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");
            entity.Property(message => message.AggregateId)
                .HasColumnName("aggregate_id");
            entity.Property(message => message.AggregateType)
                .HasColumnName("aggregate_type")
                .HasMaxLength(255);
            entity.Property(message => message.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(255);
            entity.Property(message => message.Payload)
                .HasColumnName("payload");
            entity.Property(message => message.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
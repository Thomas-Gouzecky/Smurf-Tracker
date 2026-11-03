using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public DbSet<UserSession> UserSessions => Set<UserSession>();

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
    }
}

using Fundoo.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fundoo.User.Infrastructure.Persistence;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) {}
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique();

            entity.Property(user => user.Email)
                .HasMaxLength(256);

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(256);
        });
        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.Property(login => login.Provider)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(login => login.ProviderSubject)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(login => new
            {
                login.Provider,
                login.ProviderSubject
            })
                .IsUnique();

            entity.HasIndex(login => login.UserId);
        });
    }
}

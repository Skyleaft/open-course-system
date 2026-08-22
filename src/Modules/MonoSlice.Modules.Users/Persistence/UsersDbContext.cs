using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Users.Domain;
using MonoSlice.Shared.Abstractions.Interfaces;

namespace MonoSlice.Modules.Users.Persistence;

public sealed class UsersDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IUnitOfWork
{
    public const string DefaultSchema = "identity";

    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        if (Database.IsRelational())
        {
            builder.HasDefaultSchema(DefaultSchema);
        }

        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("users");
            b.Property(u => u.FirstName).HasMaxLength(100);
            b.Property(u => u.LastName).HasMaxLength(100);
            b.Property(u => u.RefreshToken).HasMaxLength(500);
            b.Ignore(u => u.FullName);
        });

        builder.Entity<ApplicationRole>(b =>
        {
            b.ToTable("roles");
            b.Property(r => r.Description).HasMaxLength(256);
        });

        builder.Entity<RefreshToken>(b =>
        {
            b.ToTable("refresh_tokens");
            b.HasKey(r => r.Id);
            b.Property(r => r.Token).HasMaxLength(500).IsRequired();
            b.Property(r => r.ReplacedByToken).HasMaxLength(500);
            b.HasIndex(r => r.UserId);
            b.HasIndex(r => r.Token);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<ApplicationUser>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

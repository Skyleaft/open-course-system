using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Orders.Domain;

namespace MonoSlice.Modules.Orders.Persistence;

public sealed class PaymentsDbContext : DbContext
{
    public const string DefaultSchema = "payments";

    public DbSet<Order> Orders => Set<Order>();

    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);

        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("orders", DefaultSchema);

            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).ValueGeneratedNever();

            builder.Property(o => o.UserId).IsRequired();
            builder.Property(o => o.CourseId).IsRequired();

            builder.Property(o => o.Amount)
                .HasPrecision(12, 2)
                .IsRequired();

            builder.Property(o => o.Currency)
                .HasMaxLength(10)
                .IsRequired()
                .HasDefaultValue("IDR");

            builder.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(o => o.ExternalPaymentReference)
                .HasMaxLength(255);

            builder.Property(o => o.CreatedAtUtc)
                .IsRequired();

            builder.Property(o => o.PaidAtUtc);

            builder.HasIndex(o => o.UserId);
            builder.HasIndex(o => o.CourseId);
            builder.HasIndex(o => o.ExternalPaymentReference)
                .IsUnique()
                .HasFilter("\"ExternalPaymentReference\" IS NOT NULL");
        });
    }
}

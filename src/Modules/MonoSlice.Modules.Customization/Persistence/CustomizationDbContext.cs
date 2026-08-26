using Microsoft.EntityFrameworkCore;
using MonoSlice.Modules.Customization.Domain;

namespace MonoSlice.Modules.Customization.Persistence;

public sealed class CustomizationDbContext : DbContext
{
    public const string DefaultSchema = "customization";

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<LandingSection> LandingSections => Set<LandingSection>();
    public DbSet<SettingsAuditLog> SettingsAuditLogs => Set<SettingsAuditLog>();

    public CustomizationDbContext(DbContextOptions<CustomizationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);

        modelBuilder.Entity<SiteSetting>(builder =>
        {
            builder.ToTable("site_settings");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id).HasColumnName("id");
            builder.Property(s => s.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
            builder.Property(s => s.SettingKey).HasColumnName("setting_key").HasMaxLength(100).IsRequired();
            builder.Property(s => s.ValueJson).HasColumnName("value").HasColumnType("jsonb").IsRequired();
            builder.Property(s => s.IsPublic).HasColumnName("is_public").HasDefaultValue(false).IsRequired();
            builder.Property(s => s.Description).HasColumnName("description");
            builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");
            builder.Property(s => s.CreatedAt).HasColumnName("created_at_utc").IsRequired();
            builder.Property(s => s.UpdatedAt).HasColumnName("updated_at_utc");

            builder.HasIndex(s => s.SettingKey).IsUnique().HasDatabaseName("uq_settings_key");
            builder.HasIndex(s => s.Category).HasDatabaseName("idx_settings_category");
            builder.HasIndex(s => s.IsPublic).HasDatabaseName("idx_settings_public");

            builder.Ignore(s => s.DomainEvents);
        });

        modelBuilder.Entity<LandingSection>(builder =>
        {
            builder.ToTable("landing_sections");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id).HasColumnName("id");
            builder.Property(s => s.SectionType).HasColumnName("section_type").HasMaxLength(50).IsRequired();
            builder.Property(s => s.Title).HasColumnName("title").HasMaxLength(255);
            builder.Property(s => s.Subtitle).HasColumnName("subtitle");
            builder.Property(s => s.OrderIndex).HasColumnName("order_index").HasDefaultValue(1).IsRequired();
            builder.Property(s => s.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
            builder.Property(s => s.ConfigJson).HasColumnName("config").HasColumnType("jsonb").IsRequired();
            builder.Property(s => s.CreatedAt).HasColumnName("created_at_utc").IsRequired();
            builder.Property(s => s.UpdatedAt).HasColumnName("updated_at_utc");

            builder.HasIndex(s => new { s.IsActive, s.OrderIndex }).HasDatabaseName("idx_landing_sections_active");

            builder.Ignore(s => s.DomainEvents);
        });

        modelBuilder.Entity<SettingsAuditLog>(builder =>
        {
            builder.ToTable("settings_audit_logs");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id).HasColumnName("id");
            builder.Property(a => a.SettingKey).HasColumnName("setting_key").HasMaxLength(100).IsRequired();
            builder.Property(a => a.OldValueJson).HasColumnName("old_value").HasColumnType("jsonb");
            builder.Property(a => a.NewValueJson).HasColumnName("new_value").HasColumnType("jsonb").IsRequired();
            builder.Property(a => a.ChangedBy).HasColumnName("changed_by").IsRequired();
            builder.Property(a => a.ChangedAtUtc).HasColumnName("changed_at_utc").IsRequired();
            builder.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(45);

            builder.HasIndex(a => a.SettingKey).HasDatabaseName("idx_settings_audit_key");

            builder.Ignore(a => a.CreatedAt);
            builder.Ignore(a => a.UpdatedAt);
        });

        base.OnModelCreating(modelBuilder);
    }
}

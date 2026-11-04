using Microsoft.EntityFrameworkCore;
using Worker.Shared.Entities;

namespace Worker.Shared.Data;

/// <summary>
/// Shared DbContext for user status operations.
/// Maps only the tables needed for inactive schedule processing.
/// Used by Worker project to execute scheduled jobs, and can also be used by Web.
/// Both projects share the same database.
/// </summary>
public class AccountStatusDbContext : DbContext
{
    public AccountStatusDbContext(DbContextOptions<AccountStatusDbContext> options)
        : base(options) { }

    public DbSet<AppUserSimplied> UserStatusRecords => Set<AppUserSimplied>();
    public DbSet<InactiveSchedule> InactiveSchedules => Set<InactiveSchedule>();
    public DbSet<AccountStatusLog> AccountStatusLogs => Set<AccountStatusLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUserSimplied>(entity =>
        {
            // Table name is set via [Table("AspNetUsers")] attribute on the entity
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<InactiveSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.Comment).HasMaxLength(1000);
            entity.Property(e => e.DeactivateJobId).HasMaxLength(100);
            entity.Property(e => e.ReactivateJobId).HasMaxLength(100);
        });

        modelBuilder.Entity<AccountStatusLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(200);
            entity.Property(e => e.Comment).HasMaxLength(1000);
        });
    }
}

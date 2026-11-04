using Microsoft.EntityFrameworkCore;
using Worker.Shared.Entities;
using Worker.Shared.Entities.Workflow.Core;

namespace Worker.Data;

public class WebDbContext(DbContextOptions<WebDbContext> options) : DbContext(options)
{
    public DbSet<AppUserSimplied> UserStatusRecords => Set<AppUserSimplied>();

    public DbSet<InactiveSchedule> InactiveSchedules => Set<InactiveSchedule>();
    public DbSet<AccountStatusLog> AccountStatusLogs => Set<AccountStatusLog>();

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalDetail> ApprovalDetails => Set<ApprovalDetail>();   
    public DbSet<ApproverInfo> ApproverInfos => Set<ApproverInfo>();
    public DbSet<ApprovalRole> ApprovalRoles => Set<ApprovalRole>();
    public DbSet<ApprovalCase> ApprovalCases => Set<ApprovalCase>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();


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

        /* ---------------------- Config Approval Models---------------------- */

        modelBuilder.Entity<ApprovalDetail>()
            .HasOne(d => d.NextStep)
            .WithMany()
            .HasForeignKey(d => d.NextStepId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ApprovalDetail>()
        .HasOne(d => d.ReturnToStep)
        .WithMany()
        .HasForeignKey(d => d.ReturnToStepId)
        .OnDelete(DeleteBehavior.NoAction);


        modelBuilder.Entity<ApprovalStep>()
        .HasOne(d => d.ReturnStep)
        .WithMany()
        .HasForeignKey(d => d.ReturnStepId)
        .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ApprovalStep>()
            .HasOne(d => d.NextStep)
            .WithMany()
            .HasForeignKey(d => d.NextStepId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ApprovalStep>()
        .HasOne(d => d.BypassToStep)
        .WithMany()
        .HasForeignKey(d => d.BypassToStepId)
        .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ApprovalDetail>()
            .HasOne(d => d.BypassFromStep)
            .WithMany()
            .HasForeignKey(d => d.BypassFromStepId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ApprovalDetail>()
            .HasOne(d => d.BypassToStep)
            .WithMany()
            .HasForeignKey(d => d.BypassToStepId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ApproverInfo>()
            .HasOne(a => a.ApprovalDetail)
            .WithMany(d => d.ApproverInfos)
            .HasForeignKey(a => a.ApprovalDetailId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure unique constraint for ApplicationNo (business key)
        modelBuilder.Entity<ApprovalRequest>()
            .HasIndex(r => r.ApplicationNo)
            .IsUnique();

        // ApprovalDetail.RequestId references the int PK (Id)
        modelBuilder.Entity<ApprovalRequest>()
            .HasMany(r => r.ApprovalDetails)
            .WithOne(d => d.ApprovalRequest)
            .HasForeignKey(d => d.RequestId)
            .OnDelete(DeleteBehavior.Cascade);


    }
}

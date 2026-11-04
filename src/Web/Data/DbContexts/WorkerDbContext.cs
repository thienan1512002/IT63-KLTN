using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Worker.Shared.Entities;

namespace Web.Data.DbContexts
{

    public class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
    {
        public DbSet<EmailMasterData> EmailMasterDatas => Set<EmailMasterData>();
        public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("worker");

            modelBuilder.Entity<EmailMasterData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NotificationType).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.WorkflowAction).HasConversion<string>().HasMaxLength(50);
                entity.Property(e => e.ToAddress).HasMaxLength(500);
                entity.Property(e => e.CcAddresses).HasMaxLength(2000);
                entity.Property(e => e.Subject).HasMaxLength(1000);
                entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasMany(e => e.Logs)
                    .WithOne(l => l.EmailMasterData)
                    .HasForeignKey(l => l.EmailMasterDataId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EmailLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.EmailMasterDataId);
                entity.HasIndex(e => e.CreatedAt);
            });

        }
    }

}

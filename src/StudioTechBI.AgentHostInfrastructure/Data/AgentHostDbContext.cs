using Microsoft.EntityFrameworkCore;
using StudioTechBI.AgentHostDomain.Entities;
using StudioTechBI.AgentHostDomain.Enums;

namespace StudioTechBI.AgentHostInfrastructure.Data;

public sealed class AgentHostDbContext(DbContextOptions<AgentHostDbContext> options) : DbContext(options)
{
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<SubscriptionHistory> SubscriptionHistory => Set<SubscriptionHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plan>(e =>
        {
            e.ToTable("Plans", "agenthost");
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Name).IsUnique();
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.PlanType).HasConversion<string>();
            e.Property(p => p.ResetFrequency).HasConversion<string>();
        });

        modelBuilder.Entity<TenantSubscription>(e =>
        {
            e.ToTable("TenantSubscriptions", "agenthost");
            e.HasKey(s => s.Id);
            e.HasIndex(s => s.TenantId).IsUnique();
            e.Property(s => s.TenantId).HasMaxLength(200).IsRequired();
            e.Property(s => s.TenantName).HasMaxLength(500).IsRequired();
            e.HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UsageRecord>(e =>
        {
            e.ToTable("UsageRecords", "agenthost");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.TenantId);
            e.HasIndex(u => u.Timestamp);
            e.Property(u => u.TenantId).HasMaxLength(200).IsRequired();
            e.Property(u => u.Provider).HasMaxLength(100).IsRequired();
            e.Property(u => u.Model).HasMaxLength(200).IsRequired();
            e.Property(u => u.Status).HasConversion<string>();
            e.HasOne(u => u.Subscription)
                .WithMany(s => s.UsageRecords)
                .HasForeignKey(u => u.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CreditTransaction>(e =>
        {
            e.ToTable("CreditTransactions", "agenthost");
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.TenantId);
            e.Property(t => t.TenantId).HasMaxLength(200).IsRequired();
            e.Property(t => t.TransactionType).HasConversion<string>();
            e.Property(t => t.Description).HasMaxLength(500).IsRequired();
            e.Property(t => t.ReferenceId).HasMaxLength(200);
            e.HasOne(t => t.Subscription)
                .WithMany(s => s.CreditTransactions)
                .HasForeignKey(t => t.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubscriptionHistory>(e =>
        {
            e.ToTable("SubscriptionHistories", "agenthost");
            e.HasKey(h => h.Id);
            e.HasIndex(h => h.TenantId);
            e.Property(h => h.TenantId).HasMaxLength(200).IsRequired();
            e.Property(h => h.ChangedBy).HasMaxLength(500).IsRequired();
            e.Property(h => h.Notes).HasMaxLength(1000);
            e.HasOne(h => h.Subscription)
                .WithMany(s => s.History)
                .HasForeignKey(h => h.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Plan>()
                .WithMany()
                .HasForeignKey(h => h.NewPlanId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Plan>()
                .WithMany()
                .HasForeignKey(h => h.PreviousPlanId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });
    }
}

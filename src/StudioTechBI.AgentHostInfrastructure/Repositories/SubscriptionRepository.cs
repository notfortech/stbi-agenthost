using Microsoft.EntityFrameworkCore;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostDomain.Entities;
using StudioTechBI.AgentHostInfrastructure.Data;

namespace StudioTechBI.AgentHostInfrastructure.Repositories;

public sealed class SubscriptionRepository(AgentHostDbContext db) : ISubscriptionRepository
{
    public Task<TenantSubscription?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default) =>
        db.TenantSubscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive, ct);

    public async Task<IReadOnlyList<TenantSubscription>> GetAllAsync(int page, int pageSize, CancellationToken ct = default) =>
        await db.TenantSubscriptions
            .Include(s => s.Plan)
            .OrderBy(s => s.TenantName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<TenantSubscription> AddAsync(TenantSubscription subscription, CancellationToken ct = default)
    {
        db.TenantSubscriptions.Add(subscription);
        await db.SaveChangesAsync(ct);
        return subscription;
    }

    public async Task UpdateAsync(TenantSubscription subscription, CancellationToken ct = default)
    {
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        db.TenantSubscriptions.Update(subscription);
        await db.SaveChangesAsync(ct);
    }
}

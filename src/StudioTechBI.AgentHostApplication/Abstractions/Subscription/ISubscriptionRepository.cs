using StudioTechBI.AgentHostDomain.Entities;

namespace StudioTechBI.AgentHostApplication.Abstractions.Subscription;

public interface ISubscriptionRepository
{
    Task<TenantSubscription?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantSubscription>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<TenantSubscription> AddAsync(TenantSubscription subscription, CancellationToken ct = default);
    Task UpdateAsync(TenantSubscription subscription, CancellationToken ct = default);
}

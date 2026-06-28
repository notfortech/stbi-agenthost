using StudioTechBI.AgentHostDomain.Entities;

namespace StudioTechBI.AgentHostApplication.Abstractions.Subscription;

public interface IUsageRepository
{
    Task AddAsync(UsageRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<UsageRecord>> GetByTenantAsync(string tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<UsageRecord>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(string? tenantId = null, CancellationToken ct = default);
}

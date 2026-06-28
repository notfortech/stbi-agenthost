using StudioTechBI.AgentHostDomain.Entities;

namespace StudioTechBI.AgentHostApplication.Abstractions.Subscription;

public interface ICreditRepository
{
    Task AddTransactionAsync(CreditTransaction transaction, CancellationToken ct = default);
    Task<IReadOnlyList<CreditTransaction>> GetByTenantAsync(string tenantId, int page, int pageSize, CancellationToken ct = default);
}

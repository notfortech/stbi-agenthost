using StudioTechBI.AgentHostDomain.Entities;

namespace StudioTechBI.AgentHostApplication.Abstractions.Subscription;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Plan?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken ct = default);
    Task UpdateAsync(Plan plan, CancellationToken ct = default);
}

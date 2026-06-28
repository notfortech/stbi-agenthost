using Microsoft.EntityFrameworkCore;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostDomain.Entities;
using StudioTechBI.AgentHostInfrastructure.Data;

namespace StudioTechBI.AgentHostInfrastructure.Repositories;

public sealed class PlanRepository(AgentHostDbContext db) : IPlanRepository
{
    public Task<Plan?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Plan?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.Plans.FirstOrDefaultAsync(p => p.Name == name, ct);

    public async Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken ct = default) =>
        await db.Plans.Where(p => p.IsActive).ToListAsync(ct);

    public async Task UpdateAsync(Plan plan, CancellationToken ct = default)
    {
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        db.Plans.Update(plan);
        await db.SaveChangesAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostDomain.Entities;
using StudioTechBI.AgentHostInfrastructure.Data;

namespace StudioTechBI.AgentHostInfrastructure.Repositories;

public sealed class CreditRepository(AgentHostDbContext db) : ICreditRepository
{
    public async Task AddTransactionAsync(CreditTransaction transaction, CancellationToken ct = default)
    {
        db.CreditTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CreditTransaction>> GetByTenantAsync(string tenantId, int page, int pageSize, CancellationToken ct = default) =>
        await db.CreditTransactions
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
}

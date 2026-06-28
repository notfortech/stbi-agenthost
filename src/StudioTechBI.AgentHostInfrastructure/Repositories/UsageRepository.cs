using Microsoft.EntityFrameworkCore;
using StudioTechBI.AgentHostApplication.Abstractions.Subscription;
using StudioTechBI.AgentHostDomain.Entities;
using StudioTechBI.AgentHostInfrastructure.Data;

namespace StudioTechBI.AgentHostInfrastructure.Repositories;

public sealed class UsageRepository(AgentHostDbContext db) : IUsageRepository
{
    public async Task AddAsync(UsageRecord record, CancellationToken ct = default)
    {
        db.UsageRecords.Add(record);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<UsageRecord>> GetByTenantAsync(string tenantId, int page, int pageSize, CancellationToken ct = default) =>
        await db.UsageRecords
            .Where(u => u.TenantId == tenantId)
            .OrderByDescending(u => u.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UsageRecord>> GetAllAsync(int page, int pageSize, CancellationToken ct = default) =>
        await db.UsageRecords
            .OrderByDescending(u => u.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> GetTotalCountAsync(string? tenantId = null, CancellationToken ct = default) =>
        tenantId is null
            ? db.UsageRecords.CountAsync(ct)
            : db.UsageRecords.CountAsync(u => u.TenantId == tenantId, ct);
}

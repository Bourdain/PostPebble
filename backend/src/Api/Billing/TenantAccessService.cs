using Api.Domain;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Billing;

public sealed class TenantAccessService(AppDbContext dbContext) : ITenantAccessService
{
    public async Task<bool> IsTenantAdminAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var membership = await dbContext.Memberships
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);
        return membership is not null && (membership.Role == TenantRole.Owner || membership.Role == TenantRole.Admin);
    }

    public Task<bool> IsTenantMemberAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Memberships.AnyAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);
    }
}


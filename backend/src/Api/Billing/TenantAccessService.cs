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

    public async Task<bool> IsTenantReviewerAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var membership = await dbContext.Memberships
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);
        return membership is not null && (membership.Role == TenantRole.Owner || membership.Role == TenantRole.Admin || membership.Role == TenantRole.Reviewer);
    }

    public async Task<bool> IsTenantDrafterAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var membership = await dbContext.Memberships
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);
        return membership is not null; // All roles are at least a drafter
    }

    public Task<bool> IsTenantMemberAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        return dbContext.Memberships.AnyAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);
    }
}


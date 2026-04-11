using System.Security.Claims;
using Api.Domain;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Tenants;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tenants").RequireAuthorization();
        group.MapGet("/", ListMyTenantsAsync);
        group.MapPost("/{tenantId:guid}/members", InviteMemberAsync);
        return app;
    }

    private static async Task<IResult> ListMyTenantsAsync(
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var tenants = await dbContext.Memberships
            .Where(x => x.UserId == userId)
            .Include(x => x.Tenant)
            .Select(x => new
            {
                x.TenantId,
                TenantName = x.Tenant!.Name,
                Role = x.Role.ToString()
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(tenants);
    }

    private static async Task<IResult> InviteMemberAsync(
        Guid tenantId,
        InviteMemberRequest request,
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var actorMembership = await dbContext.Memberships
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);
        if (actorMembership is null || (actorMembership.Role != TenantRole.Owner && actorMembership.Role != TenantRole.Admin))
        {
            return Results.Forbid();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var role = Enum.TryParse<TenantRole>(request.Role, true, out var parsedRole)
            ? parsedRole
            : TenantRole.Drafter;

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null)
        {
            return Results.NotFound("User not found. Create user first, then invite.");
        }

        var exists = await dbContext.Memberships.AnyAsync(x => x.UserId == user.Id && x.TenantId == tenantId, cancellationToken);
        if (exists)
        {
            return Results.Conflict("User is already a member of this tenant.");
        }

        dbContext.Memberships.Add(new Membership
        {
            UserId = user.Id,
            TenantId = tenantId,
            Role = role
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { message = "Member added.", tenantId, user.Email, role = role.ToString() });
    }

    private static Guid? TryGetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private static async Task<IResult> ListTenantMembersAsync(
        Guid tenantId,
        int page,
        int pageSize,
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var actorMembership = await dbContext.Memberships
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);

        if (actorMembership is null || (actorMembership.Role != TenantRole.Owner && actorMembership.Role != TenantRole.Admin))
        {
            return Results.Forbid();
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

        var query = dbContext.Memberships
            .Where(x => x.TenantId == tenantId)
            .Include(x => x.User);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.JoinedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.UserId,
                x.User!.Email,
                Role = x.Role.ToString(),
                x.JoinedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { items, totalCount, page, pageSize });
    }

    private static async Task<IResult> UpdateMemberRoleAsync(
        Guid tenantId,
        Guid memberUserId,
        UpdateRoleRequest request,
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var actorMembership = await dbContext.Memberships
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);
            
        if (actorMembership is null || (actorMembership.Role != TenantRole.Owner && actorMembership.Role != TenantRole.Admin))
        {
            return Results.Forbid();
        }

        var targetMembership = await dbContext.Memberships
            .SingleOrDefaultAsync(x => x.UserId == memberUserId && x.TenantId == tenantId, cancellationToken);

        if (targetMembership is null)
        {
            return Results.NotFound("Member not found in this tenant.");
        }

        if (!Enum.TryParse<TenantRole>(request.NewRole, true, out var newRole))
        {
            return Results.BadRequest("Invalid role.");
        }

        if (newRole == TenantRole.Owner)
        {
            return Results.BadRequest("Use the transfer-ownership endpoint to set a new owner.");
        }

        // Only Owners can assign Admin role. Admins can only assign Reviewer/Drafter.
        if (newRole == TenantRole.Admin && actorMembership.Role != TenantRole.Owner)
        {
            return Results.Forbid();
        }
        
        // Cannot modify an Owner's role unless you are that Owner, but even then, it's better to transfer ownership.
        if (targetMembership.Role == TenantRole.Owner)
        {
             return Results.BadRequest("Cannot modify the role of the tenant owner.");
        }
        
        // Admins cannot modify other Admins
        if (actorMembership.Role == TenantRole.Admin && targetMembership.Role == TenantRole.Admin)
        {
             return Results.Forbid();
        }

        targetMembership.Role = newRole;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { message = "Role updated.", targetMembership.UserId, Role = targetMembership.Role.ToString() });
    }

    private static async Task<IResult> TransferOwnershipAsync(
        Guid tenantId,
        TransferOwnershipRequest request,
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var actorMembership = await dbContext.Memberships
            .SingleOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);

        if (actorMembership is null || actorMembership.Role != TenantRole.Owner)
        {
            return Results.Forbid();
        }

        var targetMembership = await dbContext.Memberships
            .SingleOrDefaultAsync(x => x.UserId == request.NewOwnerUserId && x.TenantId == tenantId, cancellationToken);

        if (targetMembership is null)
        {
            return Results.NotFound("Target user is not a member of this tenant.");
        }

        // Swap roles
        actorMembership.Role = TenantRole.Admin;
        targetMembership.Role = TenantRole.Owner;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { message = "Ownership transferred successfully." });
    }
}

public sealed record InviteMemberRequest(string Email, string Role = "Drafter");
public sealed record UpdateRoleRequest(string NewRole);
public sealed record TransferOwnershipRequest(Guid NewOwnerUserId);

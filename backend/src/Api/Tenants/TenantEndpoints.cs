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
}

public sealed record InviteMemberRequest(string Email, string Role = "Drafter");

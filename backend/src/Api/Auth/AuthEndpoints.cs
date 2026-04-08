using System.Security.Claims;
using Api.Domain;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapGet("/me", GetCurrentUserAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        AppDbContext dbContext,
        PasswordService passwordService,
        JwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.TenantName))
        {
            return Results.BadRequest("Email, password, and tenant name are required.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            return Results.Conflict("A user with this email already exists.");
        }

        var (hash, salt) = passwordService.HashPassword(request.Password);
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        var tenant = new Tenant { Name = request.TenantName.Trim() };
        var membership = new Membership
        {
            User = user,
            Tenant = tenant,
            Role = TenantRole.Owner
        };

        dbContext.Users.Add(user);
        dbContext.Tenants.Add(tenant);
        dbContext.Memberships.Add(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtTokenService.CreateToken(user);
        return Results.Ok(new AuthResponse(
            token,
            DateTime.UtcNow.AddMinutes(120),
            [new TenantSummary(tenant.Id, tenant.Name, TenantRole.Owner.ToString())]
        ));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AppDbContext dbContext,
        PasswordService passwordService,
        JwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .Include(x => x.Memberships)
            .ThenInclude(x => x.Tenant)
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        var verified = passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);
        if (!verified)
        {
            return Results.Unauthorized();
        }

        var token = jwtTokenService.CreateToken(user);
        var tenants = user.Memberships
            .Where(x => x.Tenant is not null)
            .Select(x => new TenantSummary(x.TenantId, x.Tenant!.Name, x.Role.ToString()))
            .ToArray();

        return Results.Ok(new AuthResponse(token, DateTime.UtcNow.AddMinutes(120), tenants));
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new { x.Id, x.Email, x.CreatedAtUtc })
            .SingleOrDefaultAsync(cancellationToken);

        return user is null ? Results.NotFound() : Results.Ok(user);
    }
}

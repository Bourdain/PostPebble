using Api.Billing;
using Api.Domain;
using Api.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Api.LinkedIn;

public static class LinkedInEndpoints
{
    public static IEndpointRouteBuilder MapLinkedInEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/v1/integrations/linkedin").RequireAuthorization();
        authGroup.MapPost("/authorize", CreateAuthorizeUrlAsync);
        authGroup.MapGet("/connections/{tenantId:guid}", ListConnectionsAsync);
        authGroup.MapPut("/connections/{tenantId:guid}/member-urn", UpsertMemberUrnAsync);

        app.MapGet("/api/v1/integrations/linkedin/callback", HandleCallbackAsync);
        return app;
    }

    private static async Task<IResult> CreateAuthorizeUrlAsync(
        CreateLinkedInAuthorizeRequest request,
        ClaimsPrincipal principal,
        IntegrationsDbContext dbContext,
        ITenantAccessService tenantAccessService,
        LinkedInOAuthService oAuthService,
        IOptions<LinkedInOptions> optionsAccessor,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var isMember = await tenantAccessService.IsTenantMemberAsync(userId.Value, request.TenantId, cancellationToken);
        if (!isMember)
        {
            return Results.Forbid();
        }

        var stateToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');

        var options = optionsAccessor.Value;
        dbContext.LinkedInOAuthStates.Add(new LinkedInOAuthState
        {
            State = stateToken,
            TenantId = request.TenantId,
            UserId = userId.Value,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(options.StateTtlMinutes)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var authorizeUrl = oAuthService.BuildAuthorizeUrl(stateToken);
        return Results.Ok(new { authorizeUrl, state = stateToken });
    }

    private static async Task<IResult> HandleCallbackAsync(
        HttpRequest request,
        IntegrationsDbContext dbContext,
        LinkedInOAuthService oAuthService,
        IOptions<LinkedInOptions> optionsAccessor,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        var callbackError = request.Query["error"].ToString();
        if (!string.IsNullOrWhiteSpace(callbackError))
        {
            var description = request.Query["error_description"].ToString();
            var stateFromError = request.Query["state"].ToString();
            var encodedReason = Uri.EscapeDataString(callbackError);
            var encodedDescription = Uri.EscapeDataString(description);
            var encodedState = Uri.EscapeDataString(stateFromError);
            return Results.Redirect($"{options.FrontendErrorRedirectUrl}?reason={encodedReason}&description={encodedDescription}&state={encodedState}");
        }

        var code = request.Query["code"].ToString();
        var state = request.Query["state"].ToString();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return Results.Redirect($"{options.FrontendErrorRedirectUrl}?reason=missing_code_or_state");
        }

        var oauthState = await dbContext.LinkedInOAuthStates
            .SingleOrDefaultAsync(x => x.State == state, cancellationToken);
        if (oauthState is null || oauthState.ExpiresAtUtc < DateTime.UtcNow || oauthState.ConsumedAtUtc is not null)
        {
            return Results.Redirect($"{options.FrontendErrorRedirectUrl}?reason=invalid_or_expired_state");
        }

        var logger = loggerFactory.CreateLogger("LinkedInOAuth");
        LinkedInTokenResponse token;
        try
        {
            token = await oAuthService.ExchangeCodeAsync(code, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LinkedIn token exchange failed for state {State}", state);
            var message = Uri.EscapeDataString(ex.Message);
            return Results.Redirect($"{options.FrontendErrorRedirectUrl}?reason=token_exchange_failed&description={message}");
        }

        var existing = await dbContext.LinkedInConnections
            .SingleOrDefaultAsync(x => x.TenantId == oauthState.TenantId, cancellationToken);

        DateTime? expiresAt = token.ExpiresIn > 0 ? DateTime.UtcNow.AddSeconds(token.ExpiresIn) : null;
        if (existing is null)
        {
            dbContext.LinkedInConnections.Add(new LinkedInConnection
            {
                TenantId = oauthState.TenantId,
                ConnectedByUserId = oauthState.UserId,
                AccessToken = token.AccessToken,
                AccessTokenExpiresAtUtc = expiresAt,
                Scope = token.Scope ?? string.Empty,
                MemberUrn = TryExtractMemberUrnFromToken(token.AccessToken)
            });
        }
        else
        {
            existing.AccessToken = token.AccessToken;
            existing.AccessTokenExpiresAtUtc = expiresAt;
            existing.Scope = token.Scope ?? existing.Scope;
            existing.MemberUrn ??= TryExtractMemberUrnFromToken(token.AccessToken);
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        oauthState.ConsumedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Redirect($"{options.FrontendSuccessRedirectUrl}?tenantId={oauthState.TenantId}");
    }

    private static async Task<IResult> ListConnectionsAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        IntegrationsDbContext dbContext,
        ITenantAccessService tenantAccessService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var isMember = await tenantAccessService.IsTenantMemberAsync(userId.Value, tenantId, cancellationToken);
        if (!isMember)
        {
            return Results.Forbid();
        }

        var rows = await dbContext.LinkedInConnections
            .Where(x => x.TenantId == tenantId)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.ConnectedByUserId,
                x.AccessTokenExpiresAtUtc,
                x.Scope,
                x.MemberUrn,
                x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(rows);
    }

    private static async Task<IResult> UpsertMemberUrnAsync(
        Guid tenantId,
        UpdateMemberUrnRequest request,
        ClaimsPrincipal principal,
        IntegrationsDbContext dbContext,
        ITenantAccessService tenantAccessService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var isAdmin = await tenantAccessService.IsTenantAdminAsync(userId.Value, tenantId, cancellationToken);
        if (!isAdmin)
        {
            return Results.Forbid();
        }

        var connection = await dbContext.LinkedInConnections.SingleOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        if (connection is null)
        {
            return Results.NotFound("LinkedIn connection not found.");
        }

        var urn = request.MemberUrn?.Trim();
        if (string.IsNullOrWhiteSpace(urn) || !urn.StartsWith("urn:li:person:", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Member URN must start with urn:li:person:");
        }

        connection.MemberUrn = urn;
        connection.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { tenantId, connection.MemberUrn });
    }

    private static string? TryExtractMemberUrnFromToken(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrWhiteSpace(sub))
            {
                return null;
            }

            return $"urn:li:person:{sub}";
        }
        catch
        {
            return null;
        }
    }
}

public sealed record CreateLinkedInAuthorizeRequest(Guid TenantId);
public sealed record UpdateMemberUrnRequest(string MemberUrn);

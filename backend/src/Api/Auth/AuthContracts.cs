namespace Api.Auth;

public sealed record RegisterRequest(string Email, string Password, string TenantName);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, IEnumerable<TenantSummary> Tenants);
public sealed record TenantSummary(Guid TenantId, string TenantName, string Role);
public sealed record InviteLookupResponse(string Email, string TenantName, string Role, DateTime ExpiresAtUtc, string Status);
public sealed record AcceptInviteRequest(string Code, string Email, string Password);

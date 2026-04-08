namespace Api.Billing;

public sealed record CreateCheckoutSessionRequest(Guid TenantId, int Credits);
public sealed record DevGrantCreditsRequest(Guid TenantId, int Credits, string Reason);

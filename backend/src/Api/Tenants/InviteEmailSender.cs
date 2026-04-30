using Microsoft.Extensions.Logging;

namespace Api.Tenants;

public interface IInviteEmailSender
{
    Task<InviteEmailDispatchResult> SendInviteAsync(
        string toEmail,
        string tenantName,
        string role,
        string inviteCode,
        CancellationToken cancellationToken);
}

public sealed class PlaceholderInviteEmailSender(ILogger<PlaceholderInviteEmailSender> logger) : IInviteEmailSender
{
    public Task<InviteEmailDispatchResult> SendInviteAsync(
        string toEmail,
        string tenantName,
        string role,
        string inviteCode,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Placeholder invite email prepared for {Email}. Tenant: {TenantName}. Role: {Role}. Code: {InviteCode}",
            toEmail,
            tenantName,
            role,
            inviteCode);

        return Task.FromResult(new InviteEmailDispatchResult(
            "placeholder",
            $"Mail server not configured. Placeholder invite prepared for {toEmail}.",
            inviteCode));
    }
}

public sealed record InviteEmailDispatchResult(string Mode, string Message, string InviteCode);

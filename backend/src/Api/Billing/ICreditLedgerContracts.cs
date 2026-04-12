using Api.Domain;

namespace Api.Billing;

public interface ITenantAccessService
{
    Task<bool> IsTenantAdminAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
    Task<bool> IsTenantReviewerAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
    Task<bool> IsTenantDrafterAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
    Task<bool> IsTenantMemberAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);
}

public interface IReservationLedgerService
{
    Task<CreditReservation?> ReserveAsync(Guid tenantId, int credits, string referenceId, string description, CancellationToken cancellationToken);
    Task<bool> ConsumeReservationAsync(Guid reservationId, string referenceId, CancellationToken cancellationToken);
    Task<bool> ReleaseReservationAsync(Guid reservationId, string referenceId, CancellationToken cancellationToken);
}


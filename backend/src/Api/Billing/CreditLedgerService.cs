using Api.Domain;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Billing;

public sealed class CreditLedgerService(BillingDbContext dbContext) : IReservationLedgerService
{
    public async Task<int> GetBalanceAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var wallet = await EnsureWalletAsync(tenantId, cancellationToken);
        return wallet.BalanceCredits;
    }

    public async Task AddPurchaseAsync(Guid tenantId, int credits, string referenceId, string description, CancellationToken cancellationToken)
    {
        var exists = await dbContext.CreditTransactions.AnyAsync(x => x.ReferenceId == referenceId, cancellationToken);
        if (exists)
        {
            return;
        }

        var wallet = await EnsureWalletAsync(tenantId, cancellationToken);
        wallet.BalanceCredits += credits;
        wallet.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.CreditTransactions.Add(new CreditTransaction
        {
            TenantId = tenantId,
            Type = CreditTransactionType.Purchase,
            AmountCredits = credits,
            ReferenceId = referenceId,
            Description = description
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CreditReservation?> ReserveAsync(Guid tenantId, int credits, string referenceId, string description, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var wallet = await EnsureWalletAsync(tenantId, cancellationToken);
        if (wallet.BalanceCredits < credits)
        {
            return null;
        }

        wallet.BalanceCredits -= credits;
        wallet.UpdatedAtUtc = DateTime.UtcNow;

        var reservation = new CreditReservation
        {
            TenantId = tenantId,
            AmountCredits = credits,
            ReferenceId = referenceId
        };
        dbContext.CreditReservations.Add(reservation);
        dbContext.CreditTransactions.Add(new CreditTransaction
        {
            TenantId = tenantId,
            Type = CreditTransactionType.Reserve,
            AmountCredits = -credits,
            ReferenceId = referenceId,
            Description = description
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return reservation;
    }

    public async Task<bool> ConsumeReservationAsync(Guid reservationId, string referenceId, CancellationToken cancellationToken)
    {
        var reservation = await dbContext.CreditReservations.SingleOrDefaultAsync(x => x.Id == reservationId, cancellationToken);
        if (reservation is null || reservation.Status != CreditReservationStatus.Reserved)
        {
            return false;
        }

        reservation.Status = CreditReservationStatus.Consumed;
        reservation.SettledAtUtc = DateTime.UtcNow;

        dbContext.CreditTransactions.Add(new CreditTransaction
        {
            TenantId = reservation.TenantId,
            Type = CreditTransactionType.Consume,
            AmountCredits = 0,
            ReferenceId = referenceId,
            Description = "Reserved credits consumed by successful publish."
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReleaseReservationAsync(Guid reservationId, string referenceId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var reservation = await dbContext.CreditReservations.SingleOrDefaultAsync(x => x.Id == reservationId, cancellationToken);
        if (reservation is null || reservation.Status != CreditReservationStatus.Reserved)
        {
            return false;
        }

        var wallet = await EnsureWalletAsync(reservation.TenantId, cancellationToken);
        wallet.BalanceCredits += reservation.AmountCredits;
        wallet.UpdatedAtUtc = DateTime.UtcNow;

        reservation.Status = CreditReservationStatus.Released;
        reservation.SettledAtUtc = DateTime.UtcNow;

        dbContext.CreditTransactions.Add(new CreditTransaction
        {
            TenantId = reservation.TenantId,
            Type = CreditTransactionType.Release,
            AmountCredits = reservation.AmountCredits,
            ReferenceId = referenceId,
            Description = "Reserved credits released back after failed publish."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<CreditWallet> EnsureWalletAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var wallet = await dbContext.CreditWallets.SingleOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        if (wallet is not null)
        {
            return wallet;
        }

        wallet = new CreditWallet
        {
            TenantId = tenantId,
            BalanceCredits = 0
        };
        dbContext.CreditWallets.Add(wallet);
        await dbContext.SaveChangesAsync(cancellationToken);
        return wallet;
    }
}

namespace Api.Domain;

public enum CreditTransactionType
{
    Purchase = 0,
    Reserve = 1,
    Consume = 2,
    Release = 3,
    Refund = 4,
    Adjustment = 5
}

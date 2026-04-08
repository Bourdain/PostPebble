namespace Api.Billing;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public decimal UnitPriceUsd { get; set; } = 0.5m;
    public string SuccessUrl { get; set; } = "http://localhost:5173/billing/success";
    public string CancelUrl { get; set; } = "http://localhost:5173/billing/cancel";
}

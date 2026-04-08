using Api.Domain;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Stripe;
using Stripe.Checkout;

namespace Api.Billing;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/billing").RequireAuthorization();
        group.MapGet("/wallets/{tenantId:guid}", GetWalletAsync);
        group.MapGet("/wallets/{tenantId:guid}/transactions", GetTransactionsAsync);
        group.MapGet("/stripe/webhook-events/{tenantId:guid}", GetWebhookEventsAsync);
        group.MapPost("/credit-packs/checkout-session", CreateCheckoutSessionAsync);
        group.MapPost("/dev/grant", GrantCreditsAsync);

        app.MapPost("/api/v1/billing/stripe/webhook", HandleStripeWebhookAsync);
        return app;
    }

    private static async Task<IResult> GetWalletAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        ITenantAccessService tenantAccessService,
        CreditLedgerService ledgerService,
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

        var balance = await ledgerService.GetBalanceAsync(tenantId, cancellationToken);
        return Results.Ok(new { tenantId, balanceCredits = balance });
    }

    private static async Task<IResult> GetTransactionsAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        ITenantAccessService tenantAccessService,
        BillingDbContext dbContext,
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

        var rows = await dbContext.CreditTransactions
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                type = x.Type.ToString(),
                x.AmountCredits,
                x.ReferenceId,
                x.Description,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        ClaimsPrincipal principal,
        ITenantAccessService tenantAccessService,
        CreditLedgerService ledgerService,
        IOptions<StripeOptions> optionsAccessor,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (request.Credits <= 0)
        {
            return Results.BadRequest("Credits must be greater than zero.");
        }

        var isAdmin = await tenantAccessService.IsTenantAdminAsync(userId.Value, request.TenantId, cancellationToken);
        if (!isAdmin)
        {
            return Results.Forbid();
        }

        var options = optionsAccessor.Value;
        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            return Results.BadRequest("Stripe SecretKey is not configured.");
        }

        StripeConfiguration.ApiKey = options.SecretKey;
        var unitAmountCents = Convert.ToInt64(decimal.Round(options.UnitPriceUsd * 100m, 0));
        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = options.SuccessUrl,
            CancelUrl = options.CancelUrl,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = request.Credits,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = unitAmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Post Credits"
                        }
                    }
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = request.TenantId.ToString(),
                ["credits"] = request.Credits.ToString()
            }
        }, cancellationToken: cancellationToken);

        return Results.Ok(new { sessionId = session.Id, sessionUrl = session.Url });
    }

    private static async Task<IResult> GetWebhookEventsAsync(
        Guid tenantId,
        ClaimsPrincipal principal,
        ITenantAccessService tenantAccessService,
        BillingDbContext dbContext,
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

        var rows = await dbContext.StripeWebhookEvents
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(100)
            .Select(x => new
            {
                x.Id,
                x.EventType,
                x.Status,
                x.ErrorMessage,
                x.ReceivedAtUtc,
                x.ProcessedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(rows);
    }

    private static async Task<IResult> GrantCreditsAsync(
        DevGrantCreditsRequest request,
        ClaimsPrincipal principal,
        ITenantAccessService tenantAccessService,
        CreditLedgerService ledgerService,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserProvider.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (request.Credits <= 0)
        {
            return Results.BadRequest("Credits must be greater than zero.");
        }

        var isAdmin = await tenantAccessService.IsTenantAdminAsync(userId.Value, request.TenantId, cancellationToken);
        if (!isAdmin)
        {
            return Results.Forbid();
        }

        await ledgerService.AddPurchaseAsync(
            request.TenantId,
            request.Credits,
            $"dev_grant:{Guid.NewGuid()}",
            string.IsNullOrWhiteSpace(request.Reason) ? "Dev manual grant." : request.Reason,
            cancellationToken
        );

        var balance = await ledgerService.GetBalanceAsync(request.TenantId, cancellationToken);
        return Results.Ok(new { request.TenantId, balanceCredits = balance });
    }

    private static async Task<IResult> HandleStripeWebhookAsync(
        HttpRequest request,
        IOptions<StripeOptions> optionsAccessor,
        IWebHostEnvironment environment,
        CreditLedgerService ledgerService,
        StripeWebhookService stripeWebhookService,
        CancellationToken cancellationToken)
    {
        var options = optionsAccessor.Value;
        var payload = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
        Event stripeEvent;

        try
        {
            if (!string.IsNullOrWhiteSpace(options.WebhookSecret))
            {
                var signatureHeader = request.Headers["Stripe-Signature"];
                stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, options.WebhookSecret);
            }
            else
            {
                if (!environment.IsDevelopment())
                {
                    return Results.BadRequest("Stripe webhook secret must be configured outside development.");
                }
                stripeEvent = EventUtility.ParseEvent(payload);
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest($"Invalid Stripe webhook payload: {ex.Message}");
        }

        if (stripeEvent.Type != EventTypes.CheckoutSessionCompleted)
        {
            var beginNonCheckout = await stripeWebhookService.TryBeginEventAsync(
                stripeEvent.Id,
                stripeEvent.Type,
                payload,
                null,
                cancellationToken
            );
            if (!beginNonCheckout)
            {
                return Results.Ok(new { received = true, duplicate = true });
            }
            await stripeWebhookService.MarkProcessedAsync(stripeEvent.Id, cancellationToken);
            return Results.Ok(new { received = true, ignored = true });
        }

        var session = stripeEvent.Data.Object as Session;
        if (session?.Metadata is null
            || !session.Metadata.TryGetValue("tenantId", out var tenantIdValue)
            || !session.Metadata.TryGetValue("credits", out var creditsValue)
            || !Guid.TryParse(tenantIdValue, out var tenantId)
            || !int.TryParse(creditsValue, out var credits)
            || credits <= 0)
        {
            return Results.BadRequest("Missing or invalid metadata for tenant credits.");
        }

        var begin = await stripeWebhookService.TryBeginEventAsync(
            stripeEvent.Id,
            stripeEvent.Type,
            payload,
            tenantId,
            cancellationToken
        );
        if (!begin)
        {
            return Results.Ok(new { received = true, duplicate = true });
        }

        try
        {
            await ledgerService.AddPurchaseAsync(
                tenantId,
                credits,
                $"stripe_checkout:{session.Id}",
                "Credits purchased through Stripe Checkout.",
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            await stripeWebhookService.MarkFailedAsync(stripeEvent.Id, ex.Message, cancellationToken);
            throw;
        }

        await stripeWebhookService.MarkProcessedAsync(stripeEvent.Id, cancellationToken);
        return Results.Ok(new { received = true });
    }
}

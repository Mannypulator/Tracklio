using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Tracklio.Shared.Configurations;
using Tracklio.Shared.Domain.Dto.Payments;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Persistence;

namespace Tracklio.Shared.Services.Stripe;

public class StripeService : IStripeService
{
    private readonly StripeSettings _stripeSettings;
    private readonly RepositoryContext _context;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IOptions<StripeSettings> stripeSettings,
        RepositoryContext context,
        ILogger<StripeService> logger)
    {
        _stripeSettings = stripeSettings.Value;
        _context = context;
        _logger = logger;

        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(Guid userId, CreatePaymentIntentRequest request)
    {
        try
        {
            // Get the subscription plan
            var plan = await _context.SubscriptionPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive);

            if (plan == null)
                throw new ArgumentException("Invalid plan selected");

            // Calculate amount based on billing period
            var amount = request.BillingPeriod.ToLower() == "yearly"
                ? plan.PriceYearly
                : plan.PriceMonthly;

            // Get or create Stripe customer
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            var customerId = await GetOrCreateCustomerAsync(user);

            // Create payment intent
            var paymentIntentService = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = plan.Currency.ToLower(),
                Customer = customerId,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                Metadata = new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(),
                    ["plan_id"] = request.PlanId.ToString(),
                    ["billing_period"] = request.BillingPeriod
                }
            };

            var paymentIntent = await paymentIntentService.CreateAsync(options);

            return new PaymentIntentResponse
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Amount = amount,
                Currency = plan.Currency
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SubscriptionResponse> CreateSubscriptionAsync(Guid userId, CreateSubscriptionRequest request)
    {
        try
        {
            // Get the subscription plan
            var plan = await _context.SubscriptionPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive);

            if (plan == null)
                throw new ArgumentException("Invalid plan selected");

            // Get user
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            // Get or create Stripe customer
            var customerId = await GetOrCreateCustomerAsync(user);

            // Create Stripe price for the plan (you might want to create these beforehand)
            var priceService = new PriceService();
            var amount = request.BillingPeriod.ToLower() == "yearly"
                ? plan.PriceYearly
                : plan.PriceMonthly;

            // Create subscription
            var subscriptionService = new SubscriptionService();
            var subscriptionOptions = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Price = await GetOrCreatePriceAsync(plan, request.BillingPeriod)
                    }
                },
                DefaultPaymentMethod = request.PaymentMethodId,
                Metadata = new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(),
                    ["plan_id"] = request.PlanId.ToString(),
                    ["billing_period"] = request.BillingPeriod
                }
            };

            var subscription = await subscriptionService.CreateAsync(subscriptionOptions);

            // Save to database
            var userSubscription = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = request.PlanId,
                Status = MapStripeStatus(subscription.Status),
                BillingPeriod = request.BillingPeriod,
                AmountPaid = amount,
                ExternalSubscriptionId = subscription.Id,
                StartDate = DateTime.UtcNow,
                EndDate = subscription.Status == "active" ? null : DateTime.UtcNow.AddDays(30)
            };

            _context.UserSubscriptions.Add(userSubscription);

            // Update user subscription status
            user.HasSubscription = subscription.Status == "active";

            await _context.SaveChangesAsync();

            return new SubscriptionResponse
            {
                Id = userSubscription.Id,
                Status = userSubscription.Status,
                StartDate = userSubscription.StartDate,
                EndDate = userSubscription.EndDate,
                BillingPeriod = userSubscription.BillingPeriod,
                AmountPaid = userSubscription.AmountPaid,
                PlanName = plan.Name,
                ExternalSubscriptionId = subscription.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for user {UserId}", userId);
            throw;
        }
    }

    public async Task<SubscriptionResponse?> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(subscriptionId);

            var userSub = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

            if (userSub == null) return null;

            return new SubscriptionResponse
            {
                Id = userSub.Id,
                Status = MapStripeStatus(subscription.Status),
                StartDate = userSub.StartDate,
                EndDate = userSub.EndDate,
                BillingPeriod = userSub.BillingPeriod,
                AmountPaid = userSub.AmountPaid,
                PlanName = userSub.Plan.Name,
                ExternalSubscriptionId = subscription.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription {SubscriptionId}", subscriptionId);
            return null;
        }
    }

    public async Task<SubscriptionResponse?> CancelSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.CancelAsync(subscriptionId);

            // Update in database
            var userSub = await _context.UserSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

            if (userSub != null)
            {
                userSub.Status = "Cancelled";
                userSub.EndDate = DateTime.UtcNow;
                userSub.UpdatedAt = DateTime.UtcNow;

                // Update user subscription status
                userSub.User.HasSubscription = false;

                await _context.SaveChangesAsync();

                return new SubscriptionResponse
                {
                    Id = userSub.Id,
                    Status = userSub.Status,
                    StartDate = userSub.StartDate,
                    EndDate = userSub.EndDate,
                    BillingPeriod = userSub.BillingPeriod,
                    AmountPaid = userSub.AmountPaid,
                    PlanName = userSub.Plan.Name,
                    ExternalSubscriptionId = subscription.Id
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<bool> HandleWebhookAsync(string payload, string signature)
    {
        try
        {
            var webhookEvent = EventUtility.ConstructEvent(
                payload, signature, _stripeSettings.WebhookSecret);

            _logger.LogInformation("Stripe webhook received: {EventType}", webhookEvent.Type);

            switch (webhookEvent.Type)
            {
                case EventTypes.PaymentIntentSucceeded:
                    await HandlePaymentIntentSucceeded(webhookEvent);
                    break;
                case EventTypes.InvoicePaymentSucceeded:
                    await HandleInvoicePaymentSucceeded(webhookEvent);
                    break;
                case EventTypes.CustomerSubscriptionUpdated:
                    await HandleSubscriptionUpdated(webhookEvent);
                    break;
                case EventTypes.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeleted(webhookEvent);
                    break;
                default:
                    _logger.LogInformation("Unhandled webhook event: {EventType}", webhookEvent.Type);
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe webhook");
            return false;
        }
    }

    public async Task<string> CreateCustomerAsync(User user)
    {
        var customerService = new CustomerService();
        var options = new CustomerCreateOptions
        {
            Email = user.Email,
            Name = $"{user.FirstName} {user.LastName}",
            Phone = user.PhoneNumber,
            Metadata = new Dictionary<string, string>
            {
                ["user_id"] = user.Id.ToString()
            }
        };

        var customer = await customerService.CreateAsync(options);
        return customer.Id;
    }

    private async Task<string> GetOrCreateCustomerAsync(User user)
    {
        // First, try to find existing customer by email
        var customerService = new CustomerService();
        var customers = await customerService.ListAsync(new CustomerListOptions
        {
            Email = user.Email,
            Limit = 1
        });

        if (customers.Data.Any())
        {
            return customers.Data.First().Id;
        }

        // Create new customer
        return await CreateCustomerAsync(user);
    }

    private async Task<string> GetOrCreatePriceAsync(SubscriptionPlan plan, string billingPeriod)
    {
        var priceService = new PriceService();
        var amount = billingPeriod.ToLower() == "yearly" ? plan.PriceYearly : plan.PriceMonthly;
        var interval = billingPeriod.ToLower() == "yearly" ? "year" : "month";

        // In production, you should store price IDs in your database
        // For now, we'll create them dynamically
        var priceOptions = new PriceCreateOptions
        {
            UnitAmount = (long)(amount * 100),
            Currency = plan.Currency.ToLower(),
            Recurring = new PriceRecurringOptions
            {
                Interval = interval
            },
            ProductData = new PriceProductDataOptions
            {
                Name = plan.DisplayName
            },
            Metadata = new Dictionary<string, string>
            {
                ["plan_id"] = plan.Id.ToString(),
                ["billing_period"] = billingPeriod
            }
        };

        var price = await priceService.CreateAsync(priceOptions);
        return price.Id;
    }

    private async Task HandlePaymentIntentSucceeded(Event webhookEvent)
    {
        var paymentIntent = webhookEvent.Data.Object as PaymentIntent;
        if (paymentIntent?.Metadata != null &&
            paymentIntent.Metadata.TryGetValue("user_id", out var userIdStr) &&
            Guid.TryParse(userIdStr, out var userId))
        {
            // Create payment transaction record
            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = paymentIntent.Amount / 100m,
                Currency = paymentIntent.Currency.ToUpper(),
                Status = "Successful",
                PaymentMethod = "Stripe",
                TransactionId = paymentIntent.Id,
                PaymentDate = DateTime.UtcNow,
                PlanName = paymentIntent.Metadata.GetValueOrDefault("plan_name", "Unknown"),
                BillingPeriod = paymentIntent.Metadata.GetValueOrDefault("billing_period", "monthly")
            };

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();
        }
    }

    private async Task HandleInvoicePaymentSucceeded(Event webhookEvent)
    {
        try
        {
            var invoice = webhookEvent.Data.Object as Invoice;
            if (invoice == null) return;

            _logger.LogInformation("Processing invoice payment succeeded: {InvoiceId}", invoice.Id);

            // Simple approach: Get subscription ID from metadata that we set during subscription creation
            // OR find by customer ID and match the amount
            string subscriptionId = null;

            // Method 1: Check if we stored subscription_id in invoice metadata
            if (invoice.Metadata?.ContainsKey("subscription_id") == true)
            {
                subscriptionId = invoice.Metadata["subscription_id"];
            }

            // Method 2: If no metadata, find by customer and amount
            if (string.IsNullOrEmpty(subscriptionId) && !string.IsNullOrEmpty(invoice.CustomerId))
            {
                // Find user subscription by customer email (if you store Stripe customer ID)
                // or by matching the invoice amount
                var invoiceAmount = (invoice.AmountPaid ) / 100m;

                // First try to find by Stripe customer ID if you store it
                var userSub = await _context.UserSubscriptions
                    .Include(s => s.Plan)
                    .Include(s => s.User)
                    .Where(s => s.Status == "Active")
                    .FirstOrDefaultAsync(s =>
                        (s.BillingPeriod.ToLower() == "monthly" && s.Plan.PriceMonthly == invoiceAmount) ||
                        (s.BillingPeriod.ToLower() == "yearly" && s.Plan.PriceYearly == invoiceAmount));

                if (userSub != null)
                {
                    await CreatePaymentTransaction(userSub, invoice);
                    return;
                }
            }

            // Method 3: If we have subscription ID, find by external subscription ID
            if (!string.IsNullOrEmpty(subscriptionId))
            {
                var userSub = await _context.UserSubscriptions
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

                if (userSub != null)
                {
                    await CreatePaymentTransaction(userSub, invoice);
                }
            }

            if (string.IsNullOrEmpty(subscriptionId))
            {
                _logger.LogWarning("Could not determine subscription for invoice {InvoiceId}", invoice.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling invoice payment succeeded webhook");
        }
    }

    private async Task CreatePaymentTransaction(UserSubscription userSub, Invoice invoice)
    {
        try
        {
            // Calculate renewal date based on billing period
            var renewalDate = userSub.BillingPeriod.ToLower() == "yearly"
                ? DateTime.UtcNow.AddYears(1)
                : DateTime.UtcNow.AddMonths(1);

            // Create payment transaction
            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userSub.UserId,
                Amount = (invoice.AmountPaid) / 100m,
                Currency = invoice.Currency?.ToUpper() ?? "GBP",
                Status = "Successful",
                PaymentMethod = "Stripe",
                TransactionId = invoice.Id,
                PaymentDate = DateTime.UtcNow,
                RenewalDate = renewalDate,
                PlanName = userSub.Plan?.Name ?? "Unknown",
                BillingPeriod = userSub.BillingPeriod
            };

            _context.PaymentTransactions.Add(transaction);

            // Update subscription for next billing cycle
            userSub.EndDate = renewalDate;
            userSub.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment transaction created for user {UserId}, amount {Amount}",
                userSub.UserId, transaction.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment transaction for subscription {SubscriptionId}",
                userSub.ExternalSubscriptionId);
        }
    }

    private async Task HandleSubscriptionUpdated(Event webhookEvent)
    {
        var subscription = webhookEvent.Data.Object as Subscription;
        if (subscription != null)
        {
            var userSub = await _context.UserSubscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscription.Id);

            if (userSub != null)
            {
                userSub.Status = MapStripeStatus(subscription.Status);
                userSub.UpdatedAt = DateTime.UtcNow;
                userSub.User.HasSubscription = subscription.Status == "active";

                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task HandleSubscriptionDeleted(Event webhookEvent)
    {
        var subscription = webhookEvent.Data.Object as Subscription;
        if (subscription != null)
        {
            var userSub = await _context.UserSubscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscription.Id);

            if (userSub != null)
            {
                userSub.Status = "Cancelled";
                userSub.EndDate = DateTime.UtcNow;
                userSub.UpdatedAt = DateTime.UtcNow;
                userSub.User.HasSubscription = false;

                await _context.SaveChangesAsync();
            }
        }
    }

    private static string MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "active" => "Active",
            "canceled" => "Cancelled",
            "incomplete" => "Incomplete",
            "incomplete_expired" => "Expired",
            "past_due" => "PastDue",
            "trialing" => "Trial",
            "unpaid" => "Unpaid",
            _ => "Unknown"
        };
    }
}
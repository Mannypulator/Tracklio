using System;

namespace Tracklio.Shared.Configurations;

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string WebhookEndpoint { get; set; } = "/api/webhooks/stripe";
}

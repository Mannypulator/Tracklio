using System;

namespace Tracklio.Shared.Domain.Dto.Subscription;

public class PaymentHistoryDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public DateTime RenewalDate { get; set; }
}

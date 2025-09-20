using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Domain.Enums;

namespace Tracklio.Shared.Persistence;

public static class DbSeeder
{
    public static void Seed(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RepositoryContext>();

        // Create sample users if they don't exist
        var userId1 = Guid.Parse("64098457-3a2e-4686-a0f7-7a903d47d3c8");
        var userId2 = Guid.Parse("03e508a8-37e3-4087-8d2c-8f7d67fe2903");

        // Create sample vehicles if they don't exist
        var vehicleId1 = Guid.Parse("f1f8de53-e58b-4ad5-97f4-c3e312bde042");
        var vehicleId2 = Guid.Parse("b64e4455-67b1-4969-bc0a-81c397d3135a");

        // Create sample subscription plans if they don't exist
        var planId1 = Guid.Parse("b78cf67d-6fe0-4c67-b283-e58b82e12211"); // Premium
        var planId2 = Guid.Parse("3b39e583-69e3-43c7-9938-a70c9c3abb23"); // Basic

        // Seed ParkingTickets
        if (!context.ParkingTickets.Any())
        {
            var tickets = new[]
            {
                new ParkingTicket
                {
                    Id = Guid.NewGuid(),
                    PCNReference = "PCN2025001",
                    VRM = "AB12CDE",
                    VehicleId = vehicleId1,
                    IssuedDate = DateTime.UtcNow.AddDays(-15),
                    Location = "Oxford Street, London",
                    Reason = "Parking in a restricted zone",
                    Amount = 130.00m,
                    DiscountedAmount = 65.00m,
                    PaymentDeadline = DateTime.UtcNow.AddDays(14),
                    AppealDeadline = DateTime.UtcNow.AddDays(7)
                },
                new ParkingTicket
                {
                    Id = Guid.NewGuid(),
                    PCNReference = "PCN2025002",
                    VRM = "XY12ZWA",
                    VehicleId = vehicleId2,
                    IssuedDate = DateTime.UtcNow.AddDays(-5),
                    Location = "High Street, Manchester",
                    Reason = "Exceeding parking time limit",
                    Amount = 70.00m,
                    DiscountedAmount = 35.00m,
                    PaymentDeadline = DateTime.UtcNow.AddDays(21),
                    AppealDeadline = DateTime.UtcNow.AddDays(14)
                },
                new ParkingTicket
                {
                    Id = Guid.NewGuid(),
                    PCNReference = "PCN2025003",
                    VRM = "LM34NOP",
                    VehicleId = vehicleId1,
                    IssuedDate = DateTime.UtcNow.AddDays(-2),
                    Location = "Queen Street, Birmingham",
                    Reason = "Parking without a valid ticket",
                    Amount = 90.00m,
                    DiscountedAmount = 45.00m,
                    PaymentDeadline = DateTime.UtcNow.AddDays(28),
                    AppealDeadline = DateTime.UtcNow.AddDays(21)
                },
                new ParkingTicket
                {
                    Id = Guid.NewGuid(),
                    PCNReference = "PCN2025004",
                    VRM = "QR56STU",
                    VehicleId = vehicleId2,
                    IssuedDate = DateTime.UtcNow.AddDays(-1),
                    Location = "King's Road, Leeds",
                    Reason = "Parking on double yellow lines",
                    Amount = 150.00m,
                    DiscountedAmount = 75.00m,
                    PaymentDeadline = DateTime.UtcNow.AddDays(14),
                    AppealDeadline = DateTime.UtcNow.AddDays(7)
                },
                new ParkingTicket
                {
                    Id = Guid.NewGuid(),
                    PCNReference = "PCN2025005",
                    VRM = "GH78IJK",
                    VehicleId = vehicleId1,
                    IssuedDate = DateTime.UtcNow,
                    Location = "Market Street, Liverpool",
                    Reason = "Parking in a no-parking zone",
                    Amount = 120.00m,
                    DiscountedAmount = 60.00m,
                    PaymentDeadline = DateTime.UtcNow.AddDays(21),
                    AppealDeadline = DateTime.UtcNow.AddDays(14)
                }
            };
            context.ParkingTickets.AddRange(tickets);
            context.SaveChanges();

            // Seed TicketImages
            var ticketImages = tickets.SelectMany(t => new[]
            {
                new TicketImage
                {
                    Id = Guid.NewGuid(),
                    TicketId = t.Id,
                    Url = $"https://storage.tracklio.com/tickets/{t.PCNReference}/front.jpg",
                    CreatedAt = t.IssuedDate
                },
                new TicketImage
                {
                    Id = Guid.NewGuid(),
                    TicketId = t.Id,
                    Url = $"https://storage.tracklio.com/tickets/{t.PCNReference}/side.jpg",
                    CreatedAt = t.IssuedDate
                }
            });
            context.TicketImages.AddRange(ticketImages);

            // Seed TicketActions
            var ticketActions = tickets.SelectMany(t => new[]
            {
                new TicketAction
                {
                    Id = Guid.NewGuid(),
                    TicketId = t.Id,
                    ActionType = TicketActionType.PaymentCompleted,
                    ActionDate = t.IssuedDate,
                    Notes = "Ticket received and logged in system",
                    IsSuccessful = true
                },
                new TicketAction
                {
                    Id = Guid.NewGuid(),
                    TicketId = t.Id,
                    ActionType = TicketActionType.AppealSubmitted,
                    ActionDate = t.IssuedDate.AddDays(2),
                    Notes = "Appeal submitted with supporting evidence",
                    ExternalReference = $"APP-{t.PCNReference}",
                    IsSuccessful = true
                },
                new TicketAction
                {
                    Id = Guid.NewGuid(),
                    TicketId = t.Id,
                    ActionType = TicketActionType.PaymentInitiated,
                    ActionDate = t.IssuedDate.AddDays(5),
                    Notes = "Payment process initiated",
                    IsSuccessful = true
                },
                new TicketAction
                {
                    Id = Guid.NewGuid(),
                    TicketId = t.Id,
                    ActionType = TicketActionType.PaymentCompleted,
                    ActionDate = t.IssuedDate.AddDays(10),
                    Notes = "Payment completed after appeal rejection",
                    IsSuccessful = true
                }
            });
            context.TicketActions.AddRange(ticketActions);
        }

        // Seed UserSubscriptions
        if (!context.UserSubscriptions.Any())
        {
            var subscriptions = new[]
            {
                new UserSubscription
                {
                    Id = Guid.NewGuid(),
                    UserId = userId1,
                    PlanId = planId1,
                    StartDate = DateTime.UtcNow.AddMonths(-2),
                    EndDate = null, // Active subscription
                    Status = "Active",
                    BillingPeriod = "monthly",
                    AmountPaid = 14.99m,
                    PaymentMethod = "Stripe",
                    ExternalSubscriptionId = "sub_premium_2025001"
                },
                new UserSubscription
                {
                    Id = Guid.NewGuid(),
                    UserId = userId2,
                    PlanId = planId2,
                    StartDate = DateTime.UtcNow.AddMonths(-1),
                    EndDate = DateTime.UtcNow.AddDays(-5), // Expired
                    Status = "Expired",
                    BillingPeriod = "yearly",
                    AmountPaid = 99.99m,
                    PaymentMethod = "Stripe",
                    ExternalSubscriptionId = "sub_basic_2025002"
                }
            };
            context.UserSubscriptions.AddRange(subscriptions);
        }

        // Seed PaymentTransactions
        if (!context.PaymentTransactions.Any())
        {
            var transactions = new[]
            {
                new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId1,
                    PlanName = "Family",
                    Amount = 14.99m,
                    Currency = "GBP",
                    Status = "Successful",
                    PaymentMethod = "Stripe",
                    TransactionId = "ch_family_2025001",
                    ReceiptUrl = "https://pay.stripe.com/receipts/family_2025001",
                    PaymentDate = DateTime.UtcNow.AddMonths(-2),
                    RenewalDate = DateTime.UtcNow.AddDays(-2)
                },
                new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId2,
                    PlanName = "Freemium",
                    Amount = 0.0m,
                    Currency = "GBP",
                    Status = "Successful",
                    PaymentMethod = "Stripe",
                    TransactionId = "ch_basic_2025002",
                    ReceiptUrl = "https://pay.stripe.com/receipts/basic_2025002",
                    PaymentDate = DateTime.UtcNow.AddMonths(-1),
                    RenewalDate = DateTime.UtcNow.AddMonths(11)
                },
                new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId1,
                    PlanName = "Family",
                    Amount = 14.99m,
                    Currency = "GBP",
                    Status = "Successful",
                    PaymentMethod = "Stripe",
                    TransactionId = "ch_family_2025003",
                    ReceiptUrl = "https://pay.stripe.com/receipts/premium_2025003",
                    PaymentDate = DateTime.UtcNow.AddDays(-2),
                    RenewalDate = DateTime.UtcNow.AddMonths(1)
                },
                new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId1,
                    PlanName = "Premium Monthly",
                    Amount = 14.99m,
                    Currency = "GBP",
                    Status = "Failed",
                    PaymentMethod = "Stripe",
                    TransactionId = "ch_premium_2025004",
                    ReceiptUrl = "https://pay.stripe.com/receipts/premium_2025004",
                    PaymentDate = DateTime.UtcNow.AddDays(-1),
                    RenewalDate = DateTime.UtcNow.AddMonths(1)
                },
                new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId2,
                    PlanName = "Basic Yearly",
                    Amount = 99.99m,
                    Currency = "GBP",
                    Status = "Successful",
                    PaymentMethod = "Stripe",
                    TransactionId = "ch_basic_2025005",
                    ReceiptUrl = "https://pay.stripe.com/receipts/basic_2025005",
                    PaymentDate = DateTime.UtcNow,
                    RenewalDate = DateTime.UtcNow.AddMonths(11)
                },
                new PaymentTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId2,
                    PlanName = "Basic Yearly",
                    Amount = 99.99m,
                    Currency = "GBP",
                    Status = "Pending",
                    PaymentMethod = "Stripe",
                    TransactionId = "ch_basic_2025006",
                    ReceiptUrl = "https://pay.stripe.com/receipts/basic_2025006",
                    PaymentDate = DateTime.UtcNow.AddDays(1),
                    RenewalDate = DateTime.UtcNow.AddMonths(11)
                }
            };
            context.PaymentTransactions.AddRange(transactions);
        }

        context.SaveChanges();
    }
}
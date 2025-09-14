using FirebaseAdmin.Auth.Hash;
using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Entities;
using Tracklio.Shared.Domain.Enums;

namespace Tracklio.Shared.Persistence;

public class RepositoryContext : DbContext
{
    public RepositoryContext(DbContextOptions<RepositoryContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<ParkingTicket> ParkingTickets { get; set; }
    public DbSet<TicketAction> TicketActions { get; set; }
    public DbSet<NotificationPreferences> NotificationPreferences { get; set; }
    public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }
    public DbSet<UserOtp> UserOtps => Set<UserOtp>();

    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }

    public DbSet<EnterprisePlan> EnterprisePlans => Set<EnterprisePlan>();
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<TicketImage> TicketImages => Set<TicketImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RepositoryContext).Assembly);

        modelBuilder.Entity<UserSubscription>()
            .HasOne(us => us.User)
            .WithMany(u => u.Subscriptions) // Add this collection
            .HasForeignKey(us => us.UserId);

        modelBuilder.Entity<UserSubscription>()
            .HasOne(us => us.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(us => us.PlanId);

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.User)
            .WithMany(u => u.PaymentTransactions)
            .HasForeignKey(pt => pt.UserId);

        modelBuilder.Entity<TicketImage>()
            .HasOne(ti => ti.Ticket)
            .WithMany(t => t.Images)
            .HasForeignKey(ti => ti.TicketId);

        modelBuilder.Entity<SubscriptionPlan>().HasData(
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "freemium",
                DisplayName = "Freemium",
                Description = "Covers one vehicle",
                Icon = "🆓",
                PriceMonthly = 0,
                PriceYearly = 0,
                Currency = "GBP",
                MaxVehicles = 1,
                IsPopular = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "solo",
                DisplayName = "Solo plan",
                Description = "Ads free, up to 5 vehicles",
                Icon = "⚡",
                PriceMonthly = 4.99m,
                PriceYearly = 59.88m,
                Currency = "GBP",
                MaxVehicles = 5,
                IsPopular = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "family",
                DisplayName = "Family plan",
                Description = "For families with up to 10 vehicles",
                Icon = "➕",
                PriceMonthly = 14.99m,
                PriceYearly = 179.88m,
                Currency = "GBP",
                MaxVehicles = 10,
                IsPopular = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "fleet",
                DisplayName = "Fleet plan",
                Description = "For small businesses with up to 15 vehicles",
                Icon = "🎯",
                PriceMonthly = 0m,
                PriceYearly = 0m,
                Currency = "GBP",
                MaxVehicles = 15,
                IsPopular = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).HasConversion<string>();

            entity.HasMany(e => e.Vehicles)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.NotificationPreferences)
                .WithOne(e => e.User)
                .HasForeignKey<NotificationPreferences>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.RefreshTokens)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Devices)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        //seed admin user
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FirstName = "Elizabeth",
            LastName = "Adegunwa",
            Role = UserRole.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            HasSubscription = false,
            ProfileImage = null,
            PhoneNumber = "+2348062841527",
            PhoneNumberConfirmed = true,
        });

        // Vehicle Configuration
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.VRM, e.UserId });
            entity.Property(e => e.VRM).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Make).HasMaxLength(50);
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(30);

            entity.HasMany(e => e.ParkingTickets)
                .WithOne(e => e.Vehicle)
                .HasForeignKey(e => e.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ParkingTicket Configuration
        modelBuilder.Entity<ParkingTicket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PCNReference).IsUnique();
            entity.HasIndex(e => new { e.VRM, e.VehicleId });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IssuedDate);

            entity.Property(e => e.PCNReference).IsRequired().HasMaxLength(50);
            entity.Property(e => e.VRM).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Amount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DiscountedAmount).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.IssuingAuthority).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PaymentUrl).HasMaxLength(500);
            entity.Property(e => e.AppealUrl).HasMaxLength(500);

            entity.HasMany(e => e.Actions)
                .WithOne(e => e.Ticket)
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TicketAction Configuration
        modelBuilder.Entity<TicketAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TicketId, e.ActionDate });
            entity.Property(e => e.ActionType).HasConversion<string>();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ExternalReference).HasMaxLength(500);
        });

        // NotificationPreferences Configuration
        modelBuilder.Entity<NotificationPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // UserRefreshToken Configuration
        modelBuilder.Entity<UserRefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsRevoked });
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.CreatedByIp).HasMaxLength(100);
            entity.Property(e => e.RevokedByIp).HasMaxLength(100);
        });

        // UserDevice Configuration

        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceToken).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.Property(e => e.DeviceToken).IsRequired();
            entity.Property(e => e.Platform).HasMaxLength(100);
        });

        // SyncLog Configuration
        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DataProvider, e.StartedAt });
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.VRM).HasMaxLength(10);
            entity.Property(e => e.DataProvider).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            entity.HasOne(e => e.Vehicle)
                .WithMany()
                .HasForeignKey(e => e.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure global query filters for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive);
        modelBuilder.Entity<Vehicle>().HasQueryFilter(v => v.IsActive);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is NotificationPreferences notificationPrefs &&
                entry.State == EntityState.Modified)
            {
                notificationPrefs.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
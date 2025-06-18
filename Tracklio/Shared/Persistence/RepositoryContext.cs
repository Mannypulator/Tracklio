using Microsoft.EntityFrameworkCore;
using Tracklio.Shared.Domain.Entities;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RepositoryContext).Assembly);

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
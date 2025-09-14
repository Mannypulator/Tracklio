using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tracklio.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class seed_admin_user : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("0ef9ca53-7ea4-42df-ab42-e055c9258b02"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("4d019ac7-718b-4c3d-8712-a4262b42f305"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("9dec9ba2-840a-4c75-ad04-9ff7c3497ebf"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("d7fff5f2-ffa0-4046-9963-400645ee0f46"));

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "CreatedAt", "Currency", "Description", "DisplayName", "Icon", "IsActive", "IsPopular", "MaxVehicles", "Name", "PriceMonthly", "PriceYearly", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("5311b1ce-9985-4930-bcd9-a57d4afc0bdb"), new DateTime(2025, 9, 13, 19, 33, 22, 929, DateTimeKind.Utc).AddTicks(3000), "GBP", "Covers one vehicle", "Freemium", "🆓", true, false, 1, "freemium", 0m, 0m, new DateTime(2025, 9, 13, 19, 33, 22, 929, DateTimeKind.Utc).AddTicks(2850) },
                    { new Guid("5bd6b6d6-e231-4488-8d8e-ef8371f8b2a4"), new DateTime(2025, 9, 13, 19, 33, 22, 929, DateTimeKind.Utc).AddTicks(3030), "GBP", "For small businesses with up to 15 vehicles", "Fleet plan", "🎯", true, false, 15, "fleet", 0m, 0m, new DateTime(2025, 9, 13, 19, 33, 22, 929, DateTimeKind.Utc).AddTicks(3030) },
                    { new Guid("9e15229e-779a-4d54-963b-5ff0fae73536"), new DateTime(2025, 9, 13, 19, 33, 22, 929, DateTimeKind.Utc).AddTicks(3020), "GBP", "For families with up to 10 vehicles", "Family plan", "➕", true, false, 10, "family", 14.99m, 179.88m, new DateTime(2025, 9, 13, 19, 33, 22, 929, DateTimeKind.Utc).AddTicks(3020) },
                    { new Guid("df27316d-64a7-4dfa-9bcb-5607be01b7dd"), new DateTime(2025, 9, 13, 19, 33, 22, 929, DateTimeKind.Utc).AddTicks(3010), "GBP", "Ads free, up to 5 vehicles", "Solo plan", "⚡", true, true, 5, "solo", 4.99m, 59.88m, new DateTime(2025, 9, 13, 19, 33, 22, 929, DateTimeKind.Utc).AddTicks(3000) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "EmailConfirmed", "FirstName", "HasSubscription", "IsActive", "LastLoginAt", "LastName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "ProfileImage", "Role", "UpdatedAt" },
                values: new object[] { new Guid("60f60b3e-58b6-47e0-a8a2-e3406d371bcf"), new DateTime(2025, 9, 13, 19, 33, 23, 361, DateTimeKind.Utc).AddTicks(3030), "admin@example.com", true, "Elizabeth", false, true, null, "Adegunwa", "$2a$11$w2Luq6QL/fZKQ5buGZBT4OudVQqgkuhZcFuqjj6sFP9GVHGhdRal.", "+2348062841527", true, null, "Admin", new DateTime(2025, 9, 13, 19, 33, 23, 361, DateTimeKind.Utc).AddTicks(3030) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("5311b1ce-9985-4930-bcd9-a57d4afc0bdb"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("5bd6b6d6-e231-4488-8d8e-ef8371f8b2a4"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("9e15229e-779a-4d54-963b-5ff0fae73536"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("df27316d-64a7-4dfa-9bcb-5607be01b7dd"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("60f60b3e-58b6-47e0-a8a2-e3406d371bcf"));

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "CreatedAt", "Currency", "Description", "DisplayName", "Icon", "IsActive", "IsPopular", "MaxVehicles", "Name", "PriceMonthly", "PriceYearly", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0ef9ca53-7ea4-42df-ab42-e055c9258b02"), new DateTime(2025, 9, 13, 18, 44, 12, 781, DateTimeKind.Utc).AddTicks(6390), "GBP", "For families with up to 10 vehicles", "Family plan", "➕", true, false, 10, "family", 14.99m, 179.88m, new DateTime(2025, 9, 13, 18, 44, 12, 781, DateTimeKind.Utc).AddTicks(6390) },
                    { new Guid("4d019ac7-718b-4c3d-8712-a4262b42f305"), new DateTime(2025, 9, 13, 18, 44, 12, 781, DateTimeKind.Utc).AddTicks(6400), "GBP", "For small businesses with up to 15 vehicles", "Fleet plan", "🎯", true, false, 15, "fleet", 0m, 0m, new DateTime(2025, 9, 13, 18, 44, 12, 781, DateTimeKind.Utc).AddTicks(6400) },
                    { new Guid("9dec9ba2-840a-4c75-ad04-9ff7c3497ebf"), new DateTime(2025, 9, 13, 18, 44, 12, 781, DateTimeKind.Utc).AddTicks(6390), "GBP", "Ads free, up to 5 vehicles", "Solo plan", "⚡", true, true, 5, "solo", 4.99m, 59.88m, new DateTime(2025, 9, 13, 18, 44, 12, 781, DateTimeKind.Utc).AddTicks(6370) },
                    { new Guid("d7fff5f2-ffa0-4046-9963-400645ee0f46"), new DateTime(2025, 9, 13, 18, 44, 12, 781, DateTimeKind.Utc).AddTicks(6370), "GBP", "Covers one vehicle", "Freemium", "🆓", true, false, 1, "freemium", 0m, 0m, new DateTime(2025, 9, 13, 18, 44, 12, 781, DateTimeKind.Utc).AddTicks(6290) }
                });
        }
    }
}

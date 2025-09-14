using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tracklio.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updated_ticket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameColumn(
                name: "RegisteredAt",
                table: "Vehicles",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Vehicles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "CreatedAt", "Currency", "Description", "DisplayName", "Icon", "IsActive", "IsPopular", "MaxVehicles", "Name", "PriceMonthly", "PriceYearly", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("18b36c97-498f-4874-8bf8-c0a643e385e5"), new DateTime(2025, 9, 13, 19, 49, 39, 278, DateTimeKind.Utc).AddTicks(8060), "GBP", "For families with up to 10 vehicles", "Family plan", "➕", true, false, 10, "family", 14.99m, 179.88m, new DateTime(2025, 9, 13, 19, 49, 39, 278, DateTimeKind.Utc).AddTicks(8060) },
                    { new Guid("35861097-7563-49a3-9650-a0ffbce9b09b"), new DateTime(2025, 9, 13, 19, 49, 39, 278, DateTimeKind.Utc).AddTicks(8070), "GBP", "For small businesses with up to 15 vehicles", "Fleet plan", "🎯", true, false, 15, "fleet", 0m, 0m, new DateTime(2025, 9, 13, 19, 49, 39, 278, DateTimeKind.Utc).AddTicks(8060) },
                    { new Guid("3b39e583-69e3-43c7-9938-a70c9c3abb23"), new DateTime(2025, 9, 13, 19, 49, 39, 278, DateTimeKind.Utc).AddTicks(8030), "GBP", "Covers one vehicle", "Freemium", "🆓", true, false, 1, "freemium", 0m, 0m, new DateTime(2025, 9, 13, 19, 49, 39, 278, DateTimeKind.Utc).AddTicks(7940) },
                    { new Guid("b78cf67d-6fe0-4c67-b283-e58b82e12211"), new DateTime(2025, 9, 13, 19, 49, 39, 278, DateTimeKind.Utc).AddTicks(8050), "GBP", "Ads free, up to 5 vehicles", "Solo plan", "⚡", true, true, 5, "solo", 4.99m, 59.88m, new DateTime(2025, 9, 13, 19, 49, 39, 278, DateTimeKind.Utc).AddTicks(8040) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "EmailConfirmed", "FirstName", "HasSubscription", "IsActive", "LastLoginAt", "LastName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "ProfileImage", "Role", "UpdatedAt" },
                values: new object[] { new Guid("bd81881f-d669-45fa-83ae-2a98539fac82"), new DateTime(2025, 9, 13, 19, 49, 39, 518, DateTimeKind.Utc).AddTicks(4860), "admin@example.com", true, "Elizabeth", false, true, null, "Adegunwa", "$2a$11$ZnIfJJdK3Q3TnustZ1fLL.8a6TASw3QV.JbaShdhCu.hJf.pAgMa2", "+2348062841527", true, null, "Admin", new DateTime(2025, 9, 13, 19, 49, 39, 518, DateTimeKind.Utc).AddTicks(4870) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("18b36c97-498f-4874-8bf8-c0a643e385e5"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("35861097-7563-49a3-9650-a0ffbce9b09b"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("3b39e583-69e3-43c7-9938-a70c9c3abb23"));

            migrationBuilder.DeleteData(
                table: "SubscriptionPlans",
                keyColumn: "Id",
                keyValue: new Guid("b78cf67d-6fe0-4c67-b283-e58b82e12211"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bd81881f-d669-45fa-83ae-2a98539fac82"));

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Vehicles",
                newName: "RegisteredAt");

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
    }
}

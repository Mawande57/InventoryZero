using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryZeroAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "IsEmailVerified", "IsPhoneVerified", "LastLoginAt", "PasswordHash", "PhoneNumber", "ProfilePictureUrl", "Rating", "Role", "StripeAccountId", "StripeCustomerId", "TotalReviews" },
                values: new object[] { 1, new DateTime(2026, 8, 17, 18, 58, 0, 10, DateTimeKind.Local).AddTicks(580), "admin@inventoryzero.com", "System Administrator", true, true, false, null, "$2a$11$BYM2muT275Fy4479ZuXBs.sDMbiFFsDG5X5uNJUTksfGkIQShvrQO", null, null, 0m, "Admin", null, null, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}

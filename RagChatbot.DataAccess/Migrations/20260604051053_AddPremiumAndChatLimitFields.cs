using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumAndChatLimitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(1802));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(2126));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(2144));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(2154));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(2166));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(1558));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(2933));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(3252));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(3273));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(3336));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(3348));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(2753));
        }
    }
}

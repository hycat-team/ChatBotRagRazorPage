using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionToAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastActiveDate",
                table: "AppUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Subscription",
                table: "AppUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TodayChatCount",
                table: "AppUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate", "Subscription", "TodayChatCount" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(2933), 0, 0 });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "LastActiveDate", "LastQueryDate", "Subscription", "TodayChatCount" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(3252), 0, 0 });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate", "Subscription", "TodayChatCount" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(3273), 0, 0 });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate", "Subscription", "TodayChatCount" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(3336), 0, 0 });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate", "Subscription", "TodayChatCount" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(3348), 0, 0 });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 4, 4, 44, 19, 707, DateTimeKind.Utc).AddTicks(2753));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastActiveDate",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Subscription",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "TodayChatCount",
                table: "AppUsers");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(6876));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(7147));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(7159));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(7167));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(7175));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(6756));
        }
    }
}

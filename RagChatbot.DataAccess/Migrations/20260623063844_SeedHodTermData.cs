using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedHodTermData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(3930) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(4198) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(4213) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 23, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(4224) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 23, 6, 38, 38, 179, DateTimeKind.Utc).AddTicks(3817));

            migrationBuilder.InsertData(
                table: "HodTerms",
                columns: new[] { "Id", "AppUserId", "DepartmentId", "EndAt", "StartAt" },
                values: new object[] { 1, 100, 1, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "HodTerms",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 10, 6, 25, 22, 565, DateTimeKind.Utc).AddTicks(3203) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 10, 6, 25, 22, 565, DateTimeKind.Utc).AddTicks(3495) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 10, 6, 25, 22, 565, DateTimeKind.Utc).AddTicks(3546) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 10, 6, 25, 22, 565, DateTimeKind.Utc).AddTicks(3585) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 6, 25, 22, 565, DateTimeKind.Utc).AddTicks(2977));
        }
    }
}

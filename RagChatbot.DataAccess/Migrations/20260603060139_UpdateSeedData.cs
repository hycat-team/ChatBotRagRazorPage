using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "admin@gmail.com", new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(6876) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "lecturer@gmail.com", new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(7147) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "student1@gmail.com", new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(7159) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "student2@gmail.com", new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(7167) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "hod@gmail.com", new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(7175) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 3, 6, 1, 30, 67, DateTimeKind.Utc).AddTicks(6756));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "admin1", new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(7726) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "lecturer1", new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(7981) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "cus1", new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(7996) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "cus2", new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(8009) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "Email", "LastQueryDate" },
                values: new object[] { "hod1", new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(8020) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(7615));
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RelatedId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessages", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 4, 48, 36, 158, DateTimeKind.Utc).AddTicks(9241) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 4, 48, 36, 158, DateTimeKind.Utc).AddTicks(9590) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 4, 48, 36, 158, DateTimeKind.Utc).AddTicks(9608) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 4, 48, 36, 158, DateTimeKind.Utc).AddTicks(9622) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 4, 48, 36, 158, DateTimeKind.Utc).AddTicks(9632) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 4, 48, 36, 158, DateTimeKind.Utc).AddTicks(8939));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactMessages");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(1802) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(2126) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(2144) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(2154) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(2166) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 4, 5, 10, 52, 634, DateTimeKind.Utc).AddTicks(1558));
        }
    }
}

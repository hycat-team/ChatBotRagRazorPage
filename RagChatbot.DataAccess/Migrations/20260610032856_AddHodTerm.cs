using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHodTerm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HodTerms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppUserId = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HodTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HodTerms_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HodTerms_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(687) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(1041) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(1055) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(1065) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(1075) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(444));

            migrationBuilder.CreateIndex(
                name: "IX_HodTerms_AppUserId",
                table: "HodTerms",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HodTerms_DepartmentId",
                table: "HodTerms",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HodTerms");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 5, 22, 6, 538, DateTimeKind.Utc).AddTicks(1288) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 5, 22, 6, 538, DateTimeKind.Utc).AddTicks(2059) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 5, 22, 6, 538, DateTimeKind.Utc).AddTicks(2108) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 5, 22, 6, 538, DateTimeKind.Utc).AddTicks(2139) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "LastActiveDate", "LastQueryDate" },
                values: new object[] { new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 5, 5, 22, 6, 538, DateTimeKind.Utc).AddTicks(2154) });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 5, 22, 6, 538, DateTimeKind.Utc).AddTicks(983));
        }
    }
}

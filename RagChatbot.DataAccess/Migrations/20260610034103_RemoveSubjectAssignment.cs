using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubjectAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubjectAssignments");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 41, 3, 570, DateTimeKind.Utc).AddTicks(9601));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 41, 3, 570, DateTimeKind.Utc).AddTicks(9927));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 41, 3, 570, DateTimeKind.Utc).AddTicks(9945));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 41, 3, 570, DateTimeKind.Utc).AddTicks(9958));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 41, 3, 570, DateTimeKind.Utc).AddTicks(9968));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 3, 41, 3, 570, DateTimeKind.Utc).AddTicks(9450));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubjectAssignments",
                columns: table => new
                {
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    LecturerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectAssignments", x => new { x.SubjectId, x.LecturerId });
                    table.ForeignKey(
                        name: "FK_SubjectAssignments_AppUsers_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectAssignments_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(687));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(1041));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(1055));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(1065));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(1075));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 3, 28, 50, 915, DateTimeKind.Utc).AddTicks(444));

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAssignments_LecturerId",
                table: "SubjectAssignments",
                column: "LecturerId");
        }
    }
}

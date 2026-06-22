using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridSearchAndHod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyQueryCount",
                table: "AppUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "AppUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastQueryDate",
                table: "AppUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActorId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    TargetObjectId = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AppUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

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
                columns: new[] { "DailyQueryCount", "DepartmentId", "LastQueryDate" },
                values: new object[] { 0, null, new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4342) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DailyQueryCount", "DepartmentId", "LastQueryDate" },
                values: new object[] { 0, 1, new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4624) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DailyQueryCount", "DepartmentId", "LastQueryDate" },
                values: new object[] { 0, null, new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4639) });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DailyQueryCount", "DepartmentId", "LastQueryDate" },
                values: new object[] { 0, null, new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4655) });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "CreatedAt", "Description", "Name" },
                values: new object[] { 1, new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4216), "Khoa CNTT", "Công nghệ Thông tin" });

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Id", "DailyQueryCount", "DepartmentId", "FirstName", "IsActive", "LastName", "LastQueryDate", "PasswordHash", "Role", "Username" },
                values: new object[] { 100, 0, 1, "Trưởng", true, "Khoa CNTT", new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4670), "Cl7afaR0DnYdIJfugd6f3iJedk+4iQxVU2eK8vcBa6w=", "HeadOfDepartment", "hod1" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_DepartmentId",
                table: "Subjects",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_DepartmentId",
                table: "AppUsers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorId",
                table: "AuditLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectAssignments_LecturerId",
                table: "SubjectAssignments",
                column: "LecturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Departments_DepartmentId",
                table: "AppUsers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Departments_DepartmentId",
                table: "Subjects",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Departments_DepartmentId",
                table: "AppUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Departments_DepartmentId",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "SubjectAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_DepartmentId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_DepartmentId",
                table: "AppUsers");

            migrationBuilder.DeleteData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "DailyQueryCount",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "LastQueryDate",
                table: "AppUsers");
        }
    }
}

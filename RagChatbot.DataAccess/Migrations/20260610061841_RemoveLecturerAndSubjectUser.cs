using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLecturerAndSubjectUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_AppUsers_UserId",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Departments_DepartmentId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_Code_UserId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_UserId",
                table: "Subjects");

            migrationBuilder.DeleteData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Subjects");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 6, 18, 36, 296, DateTimeKind.Utc).AddTicks(1898));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 6, 18, 36, 296, DateTimeKind.Utc).AddTicks(2292));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 6, 18, 36, 296, DateTimeKind.Utc).AddTicks(2314));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 6, 18, 36, 296, DateTimeKind.Utc).AddTicks(2383));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 6, 18, 36, 296, DateTimeKind.Utc).AddTicks(1702));

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Departments_DepartmentId",
                table: "Subjects",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Departments_DepartmentId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_Code",
                table: "Subjects");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Subjects",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 5, 50, 55, 736, DateTimeKind.Utc).AddTicks(2945));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 5, 50, 55, 736, DateTimeKind.Utc).AddTicks(3246));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 5, 50, 55, 736, DateTimeKind.Utc).AddTicks(3263));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 10, 5, 50, 55, 736, DateTimeKind.Utc).AddTicks(3277));

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Id", "DailyQueryCount", "DepartmentId", "Email", "FirstName", "IsActive", "LastActiveDate", "LastName", "LastQueryDate", "PasswordHash", "Role", "Subscription", "TodayChatCount" },
                values: new object[] { 2, 0, 1, "lecturer@gmail.com", "Nguyễn", true, new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Giảng Viên 1", new DateTime(2026, 6, 10, 5, 50, 55, 736, DateTimeKind.Utc).AddTicks(3232), "Yz9PJlOwHiN+8KJrW6mbQYyJTl9BLR121umofM8/fNg=", "Lecturer", 0, 0 });

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 10, 5, 50, 55, 736, DateTimeKind.Utc).AddTicks(2847));

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code_UserId",
                table: "Subjects",
                columns: new[] { "Code", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_UserId",
                table: "Subjects",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_AppUsers_UserId",
                table: "Subjects",
                column: "UserId",
                principalTable: "AppUsers",
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
    }
}

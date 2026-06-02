using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndChatSessionUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ChatSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "AppUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "Role",
                value: "Admin");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "Role", "Username" },
                values: new object[] { "Yz9PJlOwHiN+8KJrW6mbQYyJTl9BLR121umofM8/fNg=", "Lecturer", "lecturer1" });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "Role", "Username" },
                values: new object[] { "q5AEtNl18HLfc3SmE3xdUM9B4HfRQy9LxxhIBjdDrhk=", "Student", "cus1" });

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Id", "PasswordHash", "Role", "Username" },
                values: new object[] { 4, "++RMfEkC1qU39CHjzrIMeIRvyI14mE55Nv/47HrPF1I=", "Student", "cus2" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions",
                column: "UserId");

            migrationBuilder.Sql("UPDATE \"ChatSessions\" SET \"UserId\" = 1;");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatSessions_AppUsers_UserId",
                table: "ChatSessions",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatSessions_AppUsers_UserId",
                table: "ChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_ChatSessions_UserId",
                table: "ChatSessions");

            migrationBuilder.DeleteData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChatSessions");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "AppUsers");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PasswordHash", "Username" },
                values: new object[] { "q5AEtNl18HLfc3SmE3xdUM9B4HfRQy9LxxhIBjdDrhk=", "cus1" });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PasswordHash", "Username" },
                values: new object[] { "++RMfEkC1qU39CHjzrIMeIRvyI14mE55Nv/47HrPF1I=", "cus2" });
        }
    }
}

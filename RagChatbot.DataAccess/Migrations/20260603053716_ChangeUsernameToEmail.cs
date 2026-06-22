using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUsernameToEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Username",
                table: "AppUsers",
                newName: "Email");

            migrationBuilder.RenameIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                newName: "IX_AppUsers_Email");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(7726));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(7981));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(7996));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(8009));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 3, 5, 37, 0, 987, DateTimeKind.Utc).AddTicks(7615));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Email",
                table: "AppUsers",
                newName: "Username");

            migrationBuilder.RenameIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                newName: "IX_AppUsers_Username");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4342));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4624));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4639));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4655));

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 100,
                column: "LastQueryDate",
                value: new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4670));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 3, 3, 41, 8, 475, DateTimeKind.Utc).AddTicks(4216));
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNamesAndDocumentUploaderId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UploaderId",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Patch: Gán tất cả tài liệu cũ cho Admin (Id = 1) để không bị lỗi khóa ngoại (do defaultValue=0 không có user)
            migrationBuilder.Sql("UPDATE \"Documents\" SET \"UploaderId\" = 1;");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AppUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AppUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "FirstName", "LastName" },
                values: new object[] { "Quản trị", "Hệ thống" });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "FirstName", "LastName" },
                values: new object[] { "Nguyễn", "Giảng Viên 1" });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "FirstName", "LastName" },
                values: new object[] { "Học", "Sinh 1" });

            migrationBuilder.UpdateData(
                table: "AppUsers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "FirstName", "LastName" },
                values: new object[] { "Học", "Sinh 2" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UploaderId",
                table: "Documents",
                column: "UploaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_AppUsers_UploaderId",
                table: "Documents",
                column: "UploaderId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AppUsers_UploaderId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_UploaderId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "UploaderId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AppUsers");
        }
    }
}

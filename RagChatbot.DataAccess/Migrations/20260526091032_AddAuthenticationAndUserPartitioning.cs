using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RagChatbot.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationAndUserPartitioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Id", "PasswordHash", "Username" },
                values: new object[,]
                {
                    { 1, "Y07d+n5+EQi6ce7n2ti3NIbfnCs1+TT9LE/LNjozxlc=", "admin1" },
                    { 2, "q5AEtNl18HLfc3SmE3xdUM9B4HfRQy9LxxhIBjdDrhk=", "cus1" },
                    { 3, "++RMfEkC1qU39CHjzrIMeIRvyI14mE55Nv/47HrPF1I=", "cus2" }
                });

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Subjects",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_UserId",
                table: "Subjects",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_AppUsers_UserId",
                table: "Subjects",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_AppUsers_UserId",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_UserId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Subjects");
        }
    }
}

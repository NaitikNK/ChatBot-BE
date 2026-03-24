using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatBot_BE.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoleToAuthorTypeAndAddRoleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Role",
                table: "ChatMessages",
                newName: "AuthorType");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "ChatMessages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "ChatMessages");

            migrationBuilder.RenameColumn(
                name: "AuthorType",
                table: "ChatMessages",
                newName: "Role");
        }
    }
}

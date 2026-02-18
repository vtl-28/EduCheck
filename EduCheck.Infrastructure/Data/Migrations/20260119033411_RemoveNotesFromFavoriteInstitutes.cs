using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCheck.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNotesFromFavoriteInstitutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "favorite_institutes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "favorite_institutes",
                type: "text",
                nullable: true);
        }
    }
}

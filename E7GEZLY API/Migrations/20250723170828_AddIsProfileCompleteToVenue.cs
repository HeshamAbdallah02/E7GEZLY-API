using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E7GEZLY_API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsProfileCompleteToVenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProfileComplete",
                table: "Venues",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProfileComplete",
                table: "Venues");
        }
    }
}

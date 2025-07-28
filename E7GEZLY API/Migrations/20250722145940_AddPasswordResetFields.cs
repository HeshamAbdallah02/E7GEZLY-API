using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E7GEZLY_API.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailPasswordResetCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailPasswordResetCodeExpiry",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailPasswordResetCodeUsed",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordResetRequest",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhonePasswordResetCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhonePasswordResetCodeExpiry",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhonePasswordResetCodeUsed",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailPasswordResetCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailPasswordResetCodeExpiry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailPasswordResetCodeUsed",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastPasswordResetRequest",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhonePasswordResetCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhonePasswordResetCodeExpiry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhonePasswordResetCodeUsed",
                table: "AspNetUsers");
        }
    }
}

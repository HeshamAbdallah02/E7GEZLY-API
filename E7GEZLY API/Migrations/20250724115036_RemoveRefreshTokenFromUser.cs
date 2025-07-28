using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E7GEZLY_API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRefreshTokenFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create UserSessions table
            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_RefreshToken",
                table: "UserSessions",
                column: "RefreshToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_IsActive",
                table: "UserSessions",
                columns: new[] { "UserId", "IsActive" });

            // Migrate existing refresh tokens to UserSessions table before dropping columns
            migrationBuilder.Sql(@"
                INSERT INTO UserSessions (Id, UserId, RefreshToken, RefreshTokenExpiry, DeviceName, DeviceType, LastActivityAt, IsActive, CreatedAt, UpdatedAt, IsDeleted)
                SELECT 
                    NEWID(),
                    Id,
                    RefreshToken,
                    RefreshTokenExpiry,
                    'Migrated Session',
                    'Unknown',
                    GETUTCDATE(),
                    1,
                    GETUTCDATE(),
                    GETUTCDATE(),
                    0
                FROM AspNetUsers
                WHERE RefreshToken IS NOT NULL AND RefreshTokenExpiry IS NOT NULL AND RefreshTokenExpiry > GETUTCDATE()
            ");

            // Now remove the old columns from AspNetUsers
            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiry",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add back the columns to AspNetUsers
            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiry",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            // Migrate the most recent active session back to user (if needed)
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.RefreshToken = s.RefreshToken,
                    u.RefreshTokenExpiry = s.RefreshTokenExpiry
                FROM AspNetUsers u
                INNER JOIN (
                    SELECT UserId, RefreshToken, RefreshTokenExpiry,
                           ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY LastActivityAt DESC) as rn
                    FROM UserSessions
                    WHERE IsActive = 1
                ) s ON u.Id = s.UserId AND s.rn = 1
            ");

            // Drop the UserSessions table
            migrationBuilder.DropTable(
                name: "UserSessions");
        }
    }
}
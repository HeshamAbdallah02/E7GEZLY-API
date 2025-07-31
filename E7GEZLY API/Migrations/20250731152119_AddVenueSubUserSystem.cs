using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E7GEZLY_API.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueSubUserSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresSubUserSetup",
                table: "Venues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "VenueSubUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VenueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Permissions = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFounderAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBySubUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueSubUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueSubUsers_VenueSubUsers_CreatedBySubUserId",
                        column: x => x.CreatedBySubUserId,
                        principalTable: "VenueSubUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VenueSubUsers_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VenueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueAuditLogs_VenueSubUsers_SubUserId",
                        column: x => x.SubUserId,
                        principalTable: "VenueSubUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VenueAuditLogs_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VenueAuditLogs_SubUserId",
                table: "VenueAuditLogs",
                column: "SubUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueAuditLogs_VenueId_Timestamp",
                table: "VenueAuditLogs",
                columns: new[] { "VenueId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_VenueSubUsers_CreatedBySubUserId",
                table: "VenueSubUsers",
                column: "CreatedBySubUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSubUsers_VenueId",
                table: "VenueSubUsers",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSubUsers_VenueId_Username",
                table: "VenueSubUsers",
                columns: new[] { "VenueId", "Username" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VenueAuditLogs");

            migrationBuilder.DropTable(
                name: "VenueSubUsers");

            migrationBuilder.DropColumn(
                name: "RequiresSubUserSetup",
                table: "Venues");
        }
    }
}

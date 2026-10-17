using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PendingActions_Status_CreatedAtUtc",
                table: "PendingActions");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UpdatedAtUtc",
                table: "Conversations");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PendingActions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingActions_UserId_Status_CreatedAtUtc",
                table: "PendingActions",
                columns: new[] { "UserId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LogEvents_TimestampUtc",
                table: "LogEvents",
                column: "TimestampUtc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_StartedAtUtc",
                table: "Incidents",
                column: "StartedAtUtc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId_UpdatedAtUtc",
                table: "Conversations",
                columns: new[] { "UserId", "UpdatedAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Users_UserId",
                table: "Conversations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingActions_Users_UserId",
                table: "PendingActions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Users_UserId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingActions_Users_UserId",
                table: "PendingActions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_PendingActions_UserId_Status_CreatedAtUtc",
                table: "PendingActions");

            migrationBuilder.DropIndex(
                name: "IX_LogEvents_TimestampUtc",
                table: "LogEvents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_StartedAtUtc",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserId_UpdatedAtUtc",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PendingActions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Conversations");

            migrationBuilder.CreateIndex(
                name: "IX_PendingActions_Status_CreatedAtUtc",
                table: "PendingActions",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UpdatedAtUtc",
                table: "Conversations",
                column: "UpdatedAtUtc",
                descending: new bool[0]);
        }
    }
}

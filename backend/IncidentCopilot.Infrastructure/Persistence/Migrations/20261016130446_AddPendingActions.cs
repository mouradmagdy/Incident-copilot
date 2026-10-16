using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PendingActionId",
                table: "Incidents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PendingActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ArgumentsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingActions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_PendingActionId",
                table: "Incidents",
                column: "PendingActionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingActions_Status_CreatedAtUtc",
                table: "PendingActions",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_PendingActions_PendingActionId",
                table: "Incidents",
                column: "PendingActionId",
                principalTable: "PendingActions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_PendingActions_PendingActionId",
                table: "Incidents");

            migrationBuilder.DropTable(
                name: "PendingActions");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_PendingActionId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "PendingActionId",
                table: "Incidents");
        }
    }
}

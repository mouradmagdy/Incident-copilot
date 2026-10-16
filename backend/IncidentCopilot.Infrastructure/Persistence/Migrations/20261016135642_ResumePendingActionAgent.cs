using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResumePendingActionAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssistantResponse",
                table: "PendingActions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "PendingActions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToolCallId",
                table: "PendingActions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToolExchangesJson",
                table: "PendingActions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "UserMessage",
                table: "PendingActions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssistantResponse",
                table: "PendingActions");

            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "PendingActions");

            migrationBuilder.DropColumn(
                name: "ToolCallId",
                table: "PendingActions");

            migrationBuilder.DropColumn(
                name: "ToolExchangesJson",
                table: "PendingActions");

            migrationBuilder.DropColumn(
                name: "UserMessage",
                table: "PendingActions");
        }
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentCopilot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(IncidentCopilotDbContext))]
[Migration("20260926120000_EnableVectorExtension")]
public partial class EnableVectorExtension : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP EXTENSION IF EXISTS vector;");
    }
}

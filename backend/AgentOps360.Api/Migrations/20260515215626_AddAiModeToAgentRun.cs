using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentOps360.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiModeToAgentRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiMode",
                table: "AgentRuns",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiMode",
                table: "AgentRuns");
        }
    }
}

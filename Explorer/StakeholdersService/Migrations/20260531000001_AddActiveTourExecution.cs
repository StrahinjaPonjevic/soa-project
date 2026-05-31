using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StakeholdersService.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveTourExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveTourExecutionId",
                table: "Profiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveTourExecutionId",
                table: "Profiles");
        }
    }
}

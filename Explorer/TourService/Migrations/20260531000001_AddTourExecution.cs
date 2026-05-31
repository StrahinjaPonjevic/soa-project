using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TourService.Migrations
{
    /// <inheritdoc />
    public partial class AddTourExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TourExecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TourId = table.Column<int>(type: "integer", nullable: false),
                    TouristId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AbandonedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActivityAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InitialLatitude = table.Column<double>(type: "double precision", nullable: false),
                    InitialLongitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourExecutions_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TourExecutions_TouristId",
                table: "TourExecutions",
                column: "TouristId");

            migrationBuilder.CreateIndex(
                name: "IX_TourExecutions_TourId",
                table: "TourExecutions",
                column: "TourId");

            migrationBuilder.CreateTable(
                name: "CompletedKeyPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TourExecutionId = table.Column<int>(type: "integer", nullable: false),
                    KeyPointId = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedKeyPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletedKeyPoints_TourExecutions_TourExecutionId",
                        column: x => x.TourExecutionId,
                        principalTable: "TourExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompletedKeyPoints_TourExecutionId",
                table: "CompletedKeyPoints",
                column: "TourExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletedKeyPoints_TourExecutionId_KeyPointId",
                table: "CompletedKeyPoints",
                columns: new[] { "TourExecutionId", "KeyPointId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CompletedKeyPoints");
            migrationBuilder.DropTable(name: "TourExecutions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabFlow.API.Migrations
{
    /// <inheritdoc />
    public partial class AddObservationResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Observations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FhirJson = table.Column<string>(type: "TEXT", nullable: false),
                    PatientId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CodeDisplay = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    EffectiveDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValueQuantity = table.Column<decimal>(type: "TEXT", nullable: true),
                    ValueUnit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ValueCodeableConcept = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Observations_Category",
                table: "Observations",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_Code",
                table: "Observations",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_EffectiveDateTime",
                table: "Observations",
                column: "EffectiveDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_IsDeleted",
                table: "Observations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_LastUpdated",
                table: "Observations",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_PatientId",
                table: "Observations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_PatientId_EffectiveDateTime",
                table: "Observations",
                columns: new[] { "PatientId", "EffectiveDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Observations_Status",
                table: "Observations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Observations");
        }
    }
}

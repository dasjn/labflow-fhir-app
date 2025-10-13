using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabFlow.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosticReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiagnosticReports",
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
                    Issued = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResultIds = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Conclusion = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticReports_Category",
                table: "DiagnosticReports",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticReports_Code",
                table: "DiagnosticReports",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticReports_EffectiveDateTime",
                table: "DiagnosticReports",
                column: "EffectiveDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticReports_IsDeleted",
                table: "DiagnosticReports",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticReports_Issued",
                table: "DiagnosticReports",
                column: "Issued");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticReports_LastUpdated",
                table: "DiagnosticReports",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticReports_PatientId",
                table: "DiagnosticReports",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticReports_PatientId_Issued",
                table: "DiagnosticReports",
                columns: new[] { "PatientId", "Issued" });

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticReports_Status",
                table: "DiagnosticReports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiagnosticReports");
        }
    }
}

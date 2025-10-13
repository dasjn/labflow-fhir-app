using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabFlow.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FhirJson = table.Column<string>(type: "TEXT", nullable: false),
                    FamilyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    GivenName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Identifier = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_BirthDate",
                table: "Patients",
                column: "BirthDate");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_FamilyName",
                table: "Patients",
                column: "FamilyName");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_GivenName",
                table: "Patients",
                column: "GivenName");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Identifier",
                table: "Patients",
                column: "Identifier");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_IsDeleted",
                table: "Patients",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_LastUpdated",
                table: "Patients",
                column: "LastUpdated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Patients");
        }
    }
}

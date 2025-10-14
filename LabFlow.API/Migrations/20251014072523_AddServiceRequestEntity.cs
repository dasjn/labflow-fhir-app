using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabFlow.API.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FhirJson = table.Column<string>(type: "TEXT", nullable: false),
                    PatientId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CodeDisplay = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Intent = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AuthoredOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequesterId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PerformerId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    OccurrenceDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_AuthoredOn",
                table: "ServiceRequests",
                column: "AuthoredOn");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Category",
                table: "ServiceRequests",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Code",
                table: "ServiceRequests",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Intent",
                table: "ServiceRequests",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_IsDeleted",
                table: "ServiceRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_LastUpdated",
                table: "ServiceRequests",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_OccurrenceDateTime",
                table: "ServiceRequests",
                column: "OccurrenceDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_PatientId",
                table: "ServiceRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_PatientId_AuthoredOn",
                table: "ServiceRequests",
                columns: new[] { "PatientId", "AuthoredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_PerformerId",
                table: "ServiceRequests",
                column: "PerformerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_RequesterId",
                table: "ServiceRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_Status",
                table: "ServiceRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceRequests");
        }
    }
}

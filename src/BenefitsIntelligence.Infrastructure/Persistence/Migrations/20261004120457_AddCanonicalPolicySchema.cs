using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenefitsIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalPolicySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnnualPremium",
                table: "BenefitPolicies",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DependantsAllowed",
                table: "BenefitPolicies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DependantsIncluded",
                table: "BenefitPolicies",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveDate",
                table: "BenefitPolicies",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsReview",
                table: "BenefitPolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RenewalDate",
                table: "BenefitPolicies",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoverageItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Covered = table.Column<bool>(type: "bit", nullable: true),
                    Limit = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SessionLimit = table.Column<int>(type: "int", nullable: true),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    ReviewReasons = table.Column<int>(type: "int", nullable: false),
                    EvidencePageEnd = table.Column<int>(type: "int", nullable: true),
                    EvidencePageStart = table.Column<int>(type: "int", nullable: true),
                    EvidenceQuote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoverageItems_BenefitPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "BenefitPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    ReviewReasons = table.Column<int>(type: "int", nullable: false),
                    EvidencePageEnd = table.Column<int>(type: "int", nullable: true),
                    EvidencePageStart = table.Column<int>(type: "int", nullable: true),
                    EvidenceQuote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityRules_BenefitPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "BenefitPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolicyFieldAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Field = table.Column<int>(type: "int", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    ReviewReasons = table.Column<int>(type: "int", nullable: false),
                    EvidencePageEnd = table.Column<int>(type: "int", nullable: true),
                    EvidencePageStart = table.Column<int>(type: "int", nullable: true),
                    EvidenceQuote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyFieldAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyFieldAssessments_BenefitPolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "BenefitPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoverageItems_PolicyId_Type",
                table: "CoverageItems",
                columns: new[] { "PolicyId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityRules_PolicyId_Type",
                table: "EligibilityRules",
                columns: new[] { "PolicyId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyFieldAssessments_PolicyId_Field",
                table: "PolicyFieldAssessments",
                columns: new[] { "PolicyId", "Field" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoverageItems");

            migrationBuilder.DropTable(
                name: "EligibilityRules");

            migrationBuilder.DropTable(
                name: "PolicyFieldAssessments");

            migrationBuilder.DropColumn(
                name: "AnnualPremium",
                table: "BenefitPolicies");

            migrationBuilder.DropColumn(
                name: "DependantsAllowed",
                table: "BenefitPolicies");

            migrationBuilder.DropColumn(
                name: "DependantsIncluded",
                table: "BenefitPolicies");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "BenefitPolicies");

            migrationBuilder.DropColumn(
                name: "NeedsReview",
                table: "BenefitPolicies");

            migrationBuilder.DropColumn(
                name: "RenewalDate",
                table: "BenefitPolicies");
        }
    }
}

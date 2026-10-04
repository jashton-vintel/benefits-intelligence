using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenefitsIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractedPolicyHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnnualExcess",
                table: "BenefitPolicies",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "BenefitPolicies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchemeName",
                table: "BenefitPolicies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualExcess",
                table: "BenefitPolicies");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "BenefitPolicies");

            migrationBuilder.DropColumn(
                name: "SchemeName",
                table: "BenefitPolicies");
        }
    }
}

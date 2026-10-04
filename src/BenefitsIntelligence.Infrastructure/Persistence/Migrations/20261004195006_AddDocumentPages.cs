using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenefitsIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicyDocumentPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDocumentPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyDocumentPages_PolicyDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "PolicyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocumentPages_DocumentId_PageNumber",
                table: "PolicyDocumentPages",
                columns: new[] { "DocumentId", "PageNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicyDocumentPages");
        }
    }
}

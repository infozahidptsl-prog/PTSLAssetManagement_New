using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CandidateSerial",
                table: "PurchaseItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hdd",
                table: "PurchaseItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLowConfidence",
                table: "PurchaseItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PurchaseItemEvidences",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PurchaseItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OcrText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractedFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    MatchMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    CandidateSerial = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseItemEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseItemEvidences_PurchaseItems_PurchaseItemId",
                        column: x => x.PurchaseItemId,
                        principalTable: "PurchaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItemEvidences_PurchaseItemId",
                table: "PurchaseItemEvidences",
                column: "PurchaseItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseItemEvidences");

            migrationBuilder.DropColumn(
                name: "CandidateSerial",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "Hdd",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "IsLowConfidence",
                table: "PurchaseItems");
        }
    }
}

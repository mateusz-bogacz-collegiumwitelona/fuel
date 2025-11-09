using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class RepairProposalStatistisc3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceProposals_AspNetUsers_ReviewerId",
                table: "PriceProposals");

            migrationBuilder.DropIndex(
                name: "IX_PriceProposals_ReviewerId",
                table: "PriceProposals");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                table: "PriceProposals");

            migrationBuilder.CreateIndex(
                name: "IX_PriceProposals_ReviewedBy",
                table: "PriceProposals",
                column: "ReviewedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceProposals_AspNetUsers_ReviewedBy",
                table: "PriceProposals",
                column: "ReviewedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceProposals_AspNetUsers_ReviewedBy",
                table: "PriceProposals");

            migrationBuilder.DropIndex(
                name: "IX_PriceProposals_ReviewedBy",
                table: "PriceProposals");

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerId",
                table: "PriceProposals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PriceProposals_ReviewerId",
                table: "PriceProposals",
                column: "ReviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceProposals_AspNetUsers_ReviewerId",
                table: "PriceProposals",
                column: "ReviewerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

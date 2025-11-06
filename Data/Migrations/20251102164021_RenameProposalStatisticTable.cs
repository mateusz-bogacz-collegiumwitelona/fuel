using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameProposalStatisticTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProposalStatisicts_AspNetUsers_UserId",
                table: "ProposalStatisicts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProposalStatisicts",
                table: "ProposalStatisicts");

            migrationBuilder.RenameTable(
                name: "ProposalStatisicts",
                newName: "ProposalStatistics");

            migrationBuilder.RenameIndex(
                name: "IX_ProposalStatisicts_UserId",
                table: "ProposalStatistics",
                newName: "IX_ProposalStatistics_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProposalStatistics",
                table: "ProposalStatistics",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProposalStatistics_AspNetUsers_UserId",
                table: "ProposalStatistics",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProposalStatistics_AspNetUsers_UserId",
                table: "ProposalStatistics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProposalStatistics",
                table: "ProposalStatistics");

            migrationBuilder.RenameTable(
                name: "ProposalStatistics",
                newName: "ProposalStatisicts");

            migrationBuilder.RenameIndex(
                name: "IX_ProposalStatistics_UserId",
                table: "ProposalStatisicts",
                newName: "IX_ProposalStatisicts_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProposalStatisicts",
                table: "ProposalStatisicts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProposalStatisicts_AspNetUsers_UserId",
                table: "ProposalStatisicts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

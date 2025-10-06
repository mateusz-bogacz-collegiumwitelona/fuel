using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class small_changes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProposalStatisict_AspNetUsers_UserId",
                table: "ProposalStatisict");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProposalStatisict",
                table: "ProposalStatisict");

            migrationBuilder.RenameTable(
                name: "ProposalStatisict",
                newName: "ProposalStatisicts");

            migrationBuilder.RenameIndex(
                name: "IX_ProposalStatisict_UserId",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProposalStatisicts_AspNetUsers_UserId",
                table: "ProposalStatisicts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProposalStatisicts",
                table: "ProposalStatisicts");

            migrationBuilder.RenameTable(
                name: "ProposalStatisicts",
                newName: "ProposalStatisict");

            migrationBuilder.RenameIndex(
                name: "IX_ProposalStatisicts_UserId",
                table: "ProposalStatisict",
                newName: "IX_ProposalStatisict_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProposalStatisict",
                table: "ProposalStatisict",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProposalStatisict_AspNetUsers_UserId",
                table: "ProposalStatisict",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

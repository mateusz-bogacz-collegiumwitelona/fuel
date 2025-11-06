using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class RepairStationAddressTable3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stations_StationAddress_AddressId",
                table: "Stations");

            migrationBuilder.DropIndex(
                name: "IX_Stations_AddressId",
                table: "Stations");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_AddressId",
                table: "Stations",
                column: "AddressId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Stations_StationAddress_AddressId",
                table: "Stations",
                column: "AddressId",
                principalTable: "StationAddress",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stations_StationAddress_AddressId",
                table: "Stations");

            migrationBuilder.DropIndex(
                name: "IX_Stations_AddressId",
                table: "Stations");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_AddressId",
                table: "Stations",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stations_StationAddress_AddressId",
                table: "Stations",
                column: "AddressId",
                principalTable: "StationAddress",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

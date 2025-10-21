using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeStationAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Stations");

            migrationBuilder.AddColumn<Guid>(
                name: "AddressId",
                table: "Stations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<int>(
                name: "TotalProposals",
                table: "ProposalStatisicts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "RejectedProposals",
                table: "ProposalStatisicts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedProposals",
                table: "ProposalStatisicts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "AcceptedRate",
                table: "ProposalStatisicts",
                type: "integer",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.CreateTable(
                name: "StationAddress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Street = table.Column<string>(type: "text", nullable: false),
                    HouseNumber = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationAddress", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stations_StationAddress_AddressId",
                table: "Stations");

            migrationBuilder.DropTable(
                name: "StationAddress");

            migrationBuilder.DropIndex(
                name: "IX_Stations_AddressId",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "Stations");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Stations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Stations",
                type: "geometry(Point, 4326)",
                nullable: false);

            migrationBuilder.AlterColumn<int>(
                name: "TotalProposals",
                table: "ProposalStatisicts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RejectedProposals",
                table: "ProposalStatisicts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedProposals",
                table: "ProposalStatisicts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AcceptedRate",
                table: "ProposalStatisicts",
                type: "integer",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);
        }
    }
}

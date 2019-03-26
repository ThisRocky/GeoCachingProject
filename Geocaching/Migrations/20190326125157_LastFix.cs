using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class LastFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PrimaryKey_ID",
                table: "Person");

            migrationBuilder.DropPrimaryKey(
                name: "PrimaryKey_ID_",
                table: "Geocache");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Person",
                table: "Person",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Geocache",
                table: "Geocache",
                column: "ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Person",
                table: "Person");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Geocache",
                table: "Geocache");

            migrationBuilder.AddPrimaryKey(
                name: "PrimaryKey_ID",
                table: "Person",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PrimaryKey_ID_",
                table: "Geocache",
                column: "ID");
        }
    }
}

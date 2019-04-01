using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class relationManytoMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FoundGeocache_GeoCacheID",
                table: "FoundGeocache",
                column: "GeoCacheID");

            migrationBuilder.AddForeignKey(
                name: "FK_FoundGeocache_Geocache_GeoCacheID",
                table: "FoundGeocache",
                column: "GeoCacheID",
                principalTable: "Geocache",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FoundGeocache_Person_PersonID",
                table: "FoundGeocache",
                column: "PersonID",
                principalTable: "Person",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FoundGeocache_Geocache_GeoCacheID",
                table: "FoundGeocache");

            migrationBuilder.DropForeignKey(
                name: "FK_FoundGeocache_Person_PersonID",
                table: "FoundGeocache");

            migrationBuilder.DropIndex(
                name: "IX_FoundGeocache_GeoCacheID",
                table: "FoundGeocache");
        }
    }
}

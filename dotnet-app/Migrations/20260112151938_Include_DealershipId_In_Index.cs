using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_app.Migrations
{
    /// <inheritdoc />
    public partial class Include_DealershipId_In_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vehicles_vin",
                table: "vehicles");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_dealership_id_vin",
                table: "vehicles",
                columns: new[] { "dealership_id", "vin" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vehicles_dealership_id_vin",
                table: "vehicles");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_vin",
                table: "vehicles",
                column: "vin",
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_app.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dealerships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    brand = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dealerships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vin = table.Column<string>(type: "text", nullable: false),
                    stock_number = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    year = table.Column<string>(type: "text", nullable: false),
                    used = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => new { x.dealership_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "service_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    serviced_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_records", x => new { x.dealership_id, x.id });
                    table.ForeignKey(
                        name: "fk_service_records_dealerships_dealership_id",
                        column: x => x.dealership_id,
                        principalTable: "dealerships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_service_records_vehicles_dealership_id_vehicle_id",
                        columns: x => new { x.dealership_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "dealership_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_records_dealership_id_vehicle_id",
                table: "service_records",
                columns: new[] { "dealership_id", "vehicle_id" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_vin",
                table: "vehicles",
                column: "vin",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_records");

            migrationBuilder.DropTable(
                name: "dealerships");

            migrationBuilder.DropTable(
                name: "vehicles");
        }
    }
}

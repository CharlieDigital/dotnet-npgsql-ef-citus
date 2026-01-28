using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_app.Migrations
{
    /// <inheritdoc />
    public partial class Add_PartsOrder_Distributed_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "parts_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    part_number = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parts_orders", x => new { x.dealership_id, x.id });
                    table.ForeignKey(
                        name: "fk_parts_orders_vehicles_dealership_id_vehicle_id",
                        columns: x => new { x.dealership_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "dealership_id", "id" });
                });

            migrationBuilder.CreateIndex(
                name: "ix_parts_orders_dealership_id_vehicle_id",
                table: "parts_orders",
                columns: new[] { "dealership_id", "vehicle_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parts_orders");
        }
    }
}

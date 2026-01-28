using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_app.Migrations
{
    /// <inheritdoc />
    public partial class Add_Customer_With_Set_Null : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dealership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => new { x.dealership_id, x.id });
                    table.ForeignKey(
                        name: "fk_customers_vehicles_dealership_id_vehicle_id",
                        columns: x => new { x.dealership_id, x.vehicle_id },
                        principalTable: "vehicles",
                        principalColumns: new[] { "dealership_id", "id" },
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customers_dealership_id_vehicle_id",
                table: "customers",
                columns: new[] { "dealership_id", "vehicle_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}

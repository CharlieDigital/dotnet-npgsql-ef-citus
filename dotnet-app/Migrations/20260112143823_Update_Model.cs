using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_app.Migrations
{
    /// <inheritdoc />
    public partial class Update_Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "dealership_id",
                table: "vehicles",
                type: "uuid",
                nullable: false,
                defaultValueSql: "(get_tenant()::uuid)",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "dealership_id",
                table: "service_records",
                type: "uuid",
                nullable: false,
                defaultValueSql: "(get_tenant()::uuid)",
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "dealership_id",
                table: "vehicles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "(get_tenant()::uuid)");

            migrationBuilder.AlterColumn<Guid>(
                name: "dealership_id",
                table: "service_records",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "(get_tenant()::uuid)");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_app.Migrations
{
    /// <inheritdoc />
    public partial class Declare_Citus_Artifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                -- Declare the Dealerships table as a Citus distributed table
                SELECT create_distributed_table('dealerships', 'id');

                -- Declare the Vehicles table as a Citus distributed table
                SELECT create_distributed_table('vehicles', 'dealership_id');

                -- Declare the ServiceRecords table as a Citus distributed table
                SELECT create_distributed_table('service_records', 'dealership_id');
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}

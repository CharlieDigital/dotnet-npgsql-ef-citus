using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_app.Migrations
{
    /// <inheritdoc />
    public partial class Add_Citus_Utility_Functions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION get_tenant()
                RETURNS uuid AS $$
                SELECT NULLIF(current_setting('tx.current_tenant_id', true), '')::uuid;
                $$ LANGUAGE SQL STABLE;

                -- Function to set the tenant ID for the current transaction
                -- Uses SET LOCAL which Citus can propagate to worker nodes via citus.propagate_set_commands
                -- This is essential for transaction pooling mode and Citus distributed queries
                CREATE OR REPLACE FUNCTION set_tenant(tenant_id text)
                RETURNS void AS $$
                BEGIN
                EXECUTE format('SET LOCAL tx.current_tenant_id = %L', tenant_id);
                END;
                $$ LANGUAGE plpgsql;

                -- Function to unset/clear the tenant ID for the current transaction
                CREATE OR REPLACE FUNCTION unset_tenant()
                RETURNS void AS $$
                BEGIN
                EXECUTE 'SET LOCAL tx.current_tenant_id = ''''';
                END;
                $$ LANGUAGE plpgsql;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}

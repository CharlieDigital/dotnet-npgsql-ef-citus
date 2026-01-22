using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;

public class CitusSqlFixture : IAsyncLifetime
{
    private IContainer? _citusContainer;

    public NpgsqlConnection CreateConnection() =>
        new(
            $"Host=localhost;Port={_citusContainer!.GetMappedPublicPort(5432)};Username=postgres;Password=password;Database=postgres;Include Error Detail=true"
        );

    public async ValueTask DisposeAsync()
    {
        if (_citusContainer is not null)
        {
            await _citusContainer.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    public async ValueTask InitializeAsync()
    {
        // Create and start the container.
        _citusContainer = new ContainerBuilder()
            .WithImage("citusdata/citus:latest")
            .WithName("citus_test_container_sql")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_PASSWORD", "password")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilMessageIsLogged("database system is ready to accept connections")
                    .UntilInternalTcpPortIsAvailable(5432)
            )
            .Build();

        await _citusContainer.StartAsync();

        // Create default functions
        using var connection = CreateConnection();
        await connection.OpenAsync();

        var createFunctionsCommand = connection.CreateCommand();
        createFunctionsCommand.CommandText = """
            CREATE OR REPLACE FUNCTION get_tenant()
            RETURNS uuid AS $$
            SELECT NULLIF(current_setting('tx.current_tenant_id', true), '2004b4f6-85c5-43bd-abae-c60e08b18448')::uuid;
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
            """;

        createFunctionsCommand.ExecuteNonQuery();
    }
}

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
    }
}

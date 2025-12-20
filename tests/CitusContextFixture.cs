using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public class CitusContextFixture : IAsyncLifetime
{
    private IContainer? _citusContainer;

    private PooledDbContextFactory<SchoolTrackContext>? _factory;

    public SchoolTrackContext CreateContext() => _factory!.CreateDbContext();

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
            .WithName("citus_test_container")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_PASSWORD", "password")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilMessageIsLogged("database system is ready to accept connections")
            )
            .Build();

        await _citusContainer.StartAsync();

        // Migrate the database.
        _factory = new PooledDbContextFactory<SchoolTrackContext>(
            new DbContextOptionsBuilder<SchoolTrackContext>()
                .UseNpgsql(
                    $"Host=localhost;Port={_citusContainer.GetMappedPublicPort(5432)};Username=postgres;Password=password;Database=postgres"
                )
                .UseSnakeCaseNamingConvention()
                .Options
        );

        using var context = CreateContext();

        await context.Database.EnsureCreatedAsync();

        // // Now we need to mark them as distributed tables.
        // await context.Database.ExecuteSqlRawAsync(
        //     "SELECT create_distributed_table('district', 'id');"
        // );

        // await context.Database.ExecuteSqlRawAsync(
        //     "SELECT create_distributed_table('schools', 'district_id');"
        // );

        // await context.Database.ExecuteSqlRawAsync(
        //     "SELECT create_distributed_table('student', 'district_id');"
        // );

        // await context.Database.ExecuteSqlRawAsync(
        //     "SELECT create_distributed_table('teacher', 'district_id');"
        // );
    }
}

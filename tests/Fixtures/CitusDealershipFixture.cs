using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

/// <summary>
/// This fixture uses the dealership context from the standalone project which will
/// have migrations associated with i.
/// </summary>
public class CitusDealershipFixture : IAsyncLifetime
{
    private IContainer? _citusContainer;

    private DbContextOptions<DealershipContext>? _options;

    public DealershipContext CreateContext() => new(_options!);

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
            .WithName("citus_test_container_ef_dealership")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_PASSWORD", "password")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilMessageIsLogged("database system is ready to accept connections")
            )
            .Build();

        await _citusContainer.StartAsync();

        await Task.Delay(500); // Some extra buffer

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Migrate the database.
        _options = new DbContextOptionsBuilder<DealershipContext>()
            .UseNpgsql(
                $"Host=localhost;Port={_citusContainer.GetMappedPublicPort(5432)};Username=postgres;Password=password;Database=postgres;Include Error Detail=true"
            )
            .UseSnakeCaseNamingConvention()
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .UseLoggerFactory(loggerFactory)
            .Options;

        using var context = CreateContext();

        // Script the schema to console/file for manual inspection.
        // TODO: Add some tests directly around the generated SQL.
        var sqlScript = context.Database.GenerateCreateScript();
        await File.WriteAllTextAsync("../../../../schemas/schema.sql", sqlScript);

        await context.Database.MigrateAsync();

        // For this context, we want to create the distribution from the migrations.
    }
}

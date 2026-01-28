using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

/// <summary>
/// This fixture uses the dealership context from the standalone project which will
/// have migrations associated with i.
/// </summary>
public class CitusDealershipFixture : IAsyncLifetime
{
    private IContainer? _citusContainer;

    private DbContextOptionsBuilder<DealershipContext>? _optionsBuilder;

    public DealershipContext CreateContext(IEnumerable<IInterceptor>? interceptors = null) =>
        new(_optionsBuilder!.AddInterceptors(interceptors ?? Array.Empty<IInterceptor>()).Options);

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
        _optionsBuilder = new DbContextOptionsBuilder<DealershipContext>()
            .UseNpgsql(
                $"Host=localhost;Port={_citusContainer.GetMappedPublicPort(5432)};Username=postgres;Password=password;Database=postgres;Include Error Detail=true"
            )
            .UseSnakeCaseNamingConvention()
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .UseLoggerFactory(loggerFactory);

        using var context = CreateContext();

        // Script the schema to console/file for manual inspection.
        // TODO: Add some tests directly around the generated SQL.
        var sqlScript = context.Database.GenerateCreateScript();
        await File.WriteAllTextAsync("../../../../schemas/schema.sql", sqlScript);

        await context.Database.MigrateAsync();

        // For this context, we want to create the distribution from the migrations.
    }
}

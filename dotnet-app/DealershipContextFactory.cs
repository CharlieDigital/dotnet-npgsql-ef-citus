using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Needed to support creating the migration via the CLI tools.
/// </summary>
public class DealershipContextFactory : IDesignTimeDbContextFactory<DealershipContext>
{
    public DealershipContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DealershipContext>();

        optionsBuilder
            .UseNpgsql(
                "Host=localhost;Port=5432;Username=postgres;Password=password;Database=postgres;Include Error Detail=true"
            )
            .UseSnakeCaseNamingConvention();

        return new DealershipContext(optionsBuilder.Options);
    }
}

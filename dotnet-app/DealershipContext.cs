using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DealershipContext(DbContextOptions<DealershipContext> options) : DbContext(options)
{
    private static readonly TenancyCommandInterceptor TenancyCommandInterceptor = new();

    public DbSet<Dealership> Dealerships => Set<Dealership>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.AddInterceptors(TenancyCommandInterceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DealershipId to use get_tenant() for default value on inserts
        modelBuilder
            .Entity<Vehicle>()
            .Property(v => v.DealershipId)
            .HasDefaultValueSql("(get_tenant()::uuid)");

        modelBuilder
            .Entity<ServiceRecord>()
            .Property(s => s.DealershipId)
            .HasDefaultValueSql("(get_tenant()::uuid)");
    }
}

public class Dealership
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Brand { get; set; }
}

[PrimaryKey(nameof(DealershipId), nameof(Id))]
[Index(nameof(Vin), IsUnique = true)]
public class Vehicle
{
    public Guid Id { get; set; }

    // 👇 Distribution column
    public Guid DealershipId { get; set; }
    public required string Vin { get; set; }
    public required string StockNumber { get; set; }
    public required string Model { get; set; }
    public required string Year { get; set; }
    public required bool Used { get; set; }
}

[PrimaryKey(nameof(DealershipId), nameof(Id))]
[EntityTypeConfiguration(typeof(ServiceRecordConfiguration))]
public class ServiceRecord
{
    public Guid Id { get; set; }

    // 👇 Distribution column
    public Guid DealershipId { get; set; }
    public Dealership Dealership { get; set; } = null!;
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public required DateTimeOffset ServicedOnUtc { get; set; }
}

public class ServiceRecordConfiguration : IEntityTypeConfiguration<ServiceRecord>
{
    public void Configure(EntityTypeBuilder<ServiceRecord> builder) =>
        builder
            .HasOne(t => t.Vehicle)
            .WithMany()
            .HasForeignKey(service => new { service.DealershipId, service.VehicleId })
            .HasPrincipalKey(vehicle => new { vehicle.DealershipId, vehicle.Id });
}

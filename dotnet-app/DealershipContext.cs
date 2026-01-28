using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DealershipContext(DbContextOptions<DealershipContext> options) : DbContext(options)
{
    private static readonly TenancyCommandInterceptor TenancyCommandInterceptor = new();

    public DbSet<Dealership> Dealerships => Set<Dealership>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();
    public DbSet<PartsOrder> PartsOrders => Set<PartsOrder>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.AddInterceptors(TenancyCommandInterceptor);

    /// <summary>
    /// Gets the current dealership ID from the database tenant context.
    /// This method is mapped to the get_tenant() PostgreSQL function and can be used in LINQ queries.
    /// </summary>
    /// <returns>The current dealership ID as a Guid.</returns>
    public Guid UdfGetCurrentDealershipId() =>
        throw new NotSupportedException(
            "This method is for use in LINQ queries only and is translated to the get_tenant() database function."
        );

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map the get_tenant() UDF for use in LINQ queries
        modelBuilder
            .HasDbFunction(typeof(DealershipContext).GetMethod(nameof(UdfGetCurrentDealershipId))!)
            .HasName("get_tenant");

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

/// <summary>
/// This is the tenancy root entity representing a car dealership.
/// </summary>
public class Dealership
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Brand { get; set; }
}

/// <summary>
/// A vehicle is associated with a specific dealership.
/// The primary key includes DealershipId as the distribution key.
/// </summary>
[PrimaryKey(nameof(DealershipId), nameof(Id))]
[Index(nameof(DealershipId), nameof(Vin), IsUnique = true)] // 👈 Note this index requires the DealershipId
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

/// <summary>
/// A customer associated with a specific dealership.  A Vehicle can be sold to a
/// Customer.  We maintain this via a junction table.  If the Vehicle is deleted,
/// we want to keep the Customer record.  If the Customer is deleted, we want to
/// keep the Vehicle.
/// </summary>
[PrimaryKey(nameof(DealershipId), nameof(Id))]
[EntityTypeConfiguration(typeof(CustomerConfiguration))]
public class Customer
{
    public Guid Id { get; set; }
    public Guid DealershipId { get; set; }
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Use this to test ON DELETE CASCADE behavior
        builder
            .HasOne(c => c.Vehicle)
            .WithMany()
            .HasForeignKey(c => new { c.DealershipId, c.VehicleId })
            .HasPrincipalKey(vehicle => new { vehicle.DealershipId, vehicle.Id })
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Another table used for testing the "virtual" set null behavior via
/// `SetNullInterceptor`.  Here, we do NOT use the `.OnDelete(DeleteBehavior.SetNull)`
/// and instead on delete of the vehicle, we set the `VehicleId` column to `null`
/// via the `SetNullInterceptor`.
/// </summary>
[PrimaryKey(nameof(DealershipId), nameof(Id))]
[EntityTypeConfiguration(typeof(PartsOrderConfiguration))]
public class PartsOrder
{
    public Guid Id { get; set; }
    public Guid DealershipId { get; set; }

    /// <summary>
    /// The vehicle associated with this parts order. When the Vehicle is deleted,
    /// the <see cref="SetNullInterceptor"/> will set this to null while preserving
    /// the <see cref="DealershipId"/>.
    /// </summary>
    [CitusSetNullOnDelete(nameof(Vehicle))]
    public Guid? VehicleId { get; set; }

    public Vehicle? Vehicle { get; set; }
    public required string PartNumber { get; set; }
    public required string Description { get; set; }
    public required int Quantity { get; set; }
}

public class PartsOrderConfiguration : IEntityTypeConfiguration<PartsOrder>
{
    public void Configure(EntityTypeBuilder<PartsOrder> builder)
    {
        // Use ClientNoAction to prevent EF Core from attempting any cascade
        // behavior AND to prevent in-memory relationship fix-up.
        // The SetNullInterceptor will handle setting VehicleId to null when
        // a Vehicle is deleted.
        builder
            .HasOne(c => c.Vehicle)
            .WithMany()
            .HasForeignKey(c => new { c.DealershipId, c.VehicleId })
            .HasPrincipalKey(vehicle => new { vehicle.DealershipId, vehicle.Id })
            .OnDelete(DeleteBehavior.ClientNoAction);
    }
}

/// <summary>
/// An example of a distributed entity associated with a dealership via a relation
/// to another distributed entity (Vehicle).
/// </summary>
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

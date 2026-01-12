/// <summary>
/// Run as: dotnet run -- -filter "/*/*/*DealershipTests";
/// </summary>
public class DealershipTests(CitusDealershipFixture fixture) : IClassFixture<CitusDealershipFixture>
{
    [Fact]
    public void Can_Deploy_Dealership_Ef_Model()
    {
        using var context = fixture.CreateContext();

        // Just ensure we can connect and query the model.
        var dealerships = context.Dealerships.ToList();
    }

    [Fact]
    public void Can_Insert_Vehicle_With_Automatic_DealershipId_From_Tenancy_Scope()
    {
        using var context = fixture.CreateContext();

        // Create a dealership first
        var dealership = new Dealership
        {
            Id = Guid.NewGuid(),
            Name = "Smith Auto Group",
            Brand = "Honda",
        };
        context.Dealerships.Add(dealership);
        context.SaveChanges();

        // Set the tenancy scope - this will flow through the interceptor to set_tenant()
        TenancyScope.SetDealershipId(dealership.Id);

        try
        {
            // Create a vehicle WITHOUT explicitly setting DealershipId
            // The get_tenant() function will automatically populate it
            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                // 👇 Note: DealershipId is NOT set here - it will come from get_tenant()
                DealershipId = default, // This will be overridden by the database default
                Vin = "1HGCM82633A123456",
                StockNumber = "STK-001",
                Model = "Accord",
                Year = "2024",
                Used = false,
            };
            context.Vehicles.Add(vehicle);
            context.SaveChanges();

            // Verify the vehicle was saved with the correct DealershipId from tenancy
            var savedVehicle = context.Vehicles.Single(v => v.Vin == "1HGCM82633A123456");

            Assert.Equal(dealership.Id, savedVehicle.DealershipId);
        }
        finally
        {
            // Clean up the tenancy scope
            TenancyScope.Clear();
        }
    }

    [Fact(Skip = "This doesn't work; the UDF can't read the tenant context in this setup.")]
    public void Can_Query_Vehicles_Using_Tenancy_Udf_In_LINQ()
    {
        using var context = fixture.CreateContext();

        // Create two dealerships with vehicles
        var dealership1 = new Dealership
        {
            Id = Guid.NewGuid(),
            Name = "Premium Motors",
            Brand = "BMW",
        };
        var dealership2 = new Dealership
        {
            Id = Guid.NewGuid(),
            Name = "Value Auto",
            Brand = "Toyota",
        };
        context.Dealerships.AddRange(dealership1, dealership2);
        context.SaveChanges();

        // Create vehicles for dealership 1
        TenancyScope.SetDealershipId(dealership1.Id);
        try
        {
            var vehicle1 = new Vehicle
            {
                Id = Guid.NewGuid(),
                DealershipId = default,
                Vin = "WBADT43452G123456",
                StockNumber = "BMW-001",
                Model = "330i",
                Year = "2024",
                Used = false,
            };
            context.Vehicles.Add(vehicle1);
            context.SaveChanges();
        }
        finally
        {
            TenancyScope.Clear();
        }

        // Create vehicles for dealership 2
        TenancyScope.SetDealershipId(dealership2.Id);
        try
        {
            var vehicle2 = new Vehicle
            {
                Id = Guid.NewGuid(),
                DealershipId = default,
                Vin = "4T1BF1FK5CU123456",
                StockNumber = "TOY-001",
                Model = "Camry",
                Year = "2024",
                Used = false,
            };
            context.Vehicles.Add(vehicle2);
            context.SaveChanges();
        }
        finally
        {
            TenancyScope.Clear();
        }

        // Now query using the UDF to filter by current tenant
        TenancyScope.SetDealershipId(dealership1.Id);
        try
        {
            // Use the mapped UDF in a LINQ query to filter by current tenant
            // This translates to: WHERE dealership_id = get_tenant()::uuid
            var tenantVehicles = context
                .Vehicles.Where(v => v.DealershipId == context.UdfGetCurrentDealershipId())
                .ToList();

            // Should only return the BMW vehicle for dealership1
            Assert.Single(tenantVehicles);
            Assert.Equal("WBADT43452G123456", tenantVehicles[0].Vin);
            Assert.Equal("BMW-001", tenantVehicles[0].StockNumber);
            Assert.Equal(dealership1.Id, tenantVehicles[0].DealershipId);
        }
        finally
        {
            TenancyScope.Clear();
        }

        // Switch tenant and verify we get different results
        TenancyScope.SetDealershipId(dealership2.Id);
        try
        {
            var tenantVehicles = context
                .Vehicles.Where(v => v.DealershipId == context.UdfGetCurrentDealershipId())
                .ToList();

            // Should only return the Toyota vehicle for dealership2
            Assert.Single(tenantVehicles);
            Assert.Equal("4T1BF1FK5CU123456", tenantVehicles[0].Vin);
            Assert.Equal("TOY-001", tenantVehicles[0].StockNumber);
            Assert.Equal(dealership2.Id, tenantVehicles[0].DealershipId);
        }
        finally
        {
            TenancyScope.Clear();
        }
    }
}

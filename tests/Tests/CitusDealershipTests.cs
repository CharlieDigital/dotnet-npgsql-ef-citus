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
                // Note: DealershipId is NOT set here - it will come from get_tenant()
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
}

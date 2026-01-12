public class DealershipTests(CitusDealershipFixture fixture) : IClassFixture<CitusDealershipFixture>
{
    [Fact]
    public void Can_Deploy_Dealership_Ef_Model()
    {
        using var context = fixture.CreateContext();

        // Just ensure we can connect and query the model.
        var dealerships = context.Dealerships.ToList();
    }
}

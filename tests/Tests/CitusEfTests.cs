public class CitusEfTests(CitusContextFixture citus) : IClassFixture<CitusContextFixture>
{
    [Fact]
    public void Can_Deploy_Ef_Model()
    {
        using var context = citus.CreateContext();
        // Just ensure we can connect and query the model.
        var districts = context.Districts.ToList();
    }
}

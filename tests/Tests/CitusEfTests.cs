public class CitusEfTests(CitusContextFixture citus) : IClassFixture<CitusContextFixture>
{
    [Fact]
    public void Can_Deploy_Ef_Model()
    {
        using var context = citus.CreateContext();
        // Just ensure we can connect and query the model.
        var districts = context.Districts.ToList();
    }

    /*
    [Fact]
    public async Task Can_Create_District_School_Teacher()
    {
        using var context = citus.CreateContext();
        using var tx = await context.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken
        );

        var hamiltonTownship = new District
        {
            Id = Guid.CreateVersion7(),
            Name = "Hamilton Township Public School District",
        };

        var highschoolType = new SchoolType { Id = Guid.CreateVersion7(), Name = "High School" };

        var gwHighSchool = new School
        {
            Id = Guid.CreateVersion7(),
            Name = "George Washington High School",
            District = hamiltonTownship,
            SchoolType = highschoolType,
        };

        var sandraChen = new Teacher
        {
            Id = Guid.CreateVersion7(),
            District = hamiltonTownship,
            Name = "Sandra Chen",
            School = gwHighSchool,
        };

        context.Add(sandraChen);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
    */
}

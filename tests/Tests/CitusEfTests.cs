using Microsoft.EntityFrameworkCore;

public class CitusEfTests(CitusContextFixture citus) : IClassFixture<CitusContextFixture>
{
    private sealed record TeacherGraphSetup(
        Guid DistrictId,
        Guid SchoolId,
        Guid TeacherId,
        IReadOnlyList<Guid> StudentIds,
        string DistrictName,
        string SchoolName,
        string TeacherName
    );

    [Fact]
    public void Can_Deploy_Ef_Model()
    {
        using var context = citus.CreateContext();
        // Just ensure we can connect and query the model.
        var districts = context.Districts.ToList();
    }

    [Fact]
    public async Task Can_Create_District_School_Teacher()
    {
        var setup = await CreateTeacherGraphAsync();

        using var context = citus.CreateContext();
        var teacher = await context.Teachers.SingleAsync(
            t => t.Id == setup.TeacherId,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(setup.DistrictId, teacher.DistrictId);
    }

    [Fact]
    public async Task Can_Query_Teacher_With_Include_School()
    {
        var setup = await CreateTeacherGraphAsync();

        using var context = citus.CreateContext();
        var teacher = await context
            .Teachers.Include(t => t.School)
            .SingleAsync(t => t.Id == setup.TeacherId, TestContext.Current.CancellationToken);

        Assert.Equal(setup.TeacherName, teacher.Name);
        Assert.NotNull(teacher.School);
        Assert.Equal(setup.SchoolId, teacher.SchoolId);
        Assert.Equal(setup.SchoolName, teacher.School.Name);
        Assert.Equal(setup.DistrictId, teacher.School.DistrictId);
    }

    [Fact]
    public async Task Can_Query_Teacher_With_Include_School_And_District()
    {
        var setup = await CreateTeacherGraphAsync();

        using var context = citus.CreateContext();
        var teacher = await context
            .Teachers.Include(t => t.School)
            .Include(t => t.District)
            .SingleAsync(t => t.Id == setup.TeacherId, TestContext.Current.CancellationToken);

        Assert.NotNull(teacher.School);
        Assert.NotNull(teacher.District);
        Assert.Equal(setup.SchoolName, teacher.School.Name);
        Assert.Equal(setup.DistrictId, teacher.District.Id);
        Assert.Equal(setup.DistrictName, teacher.District.Name);
    }

    [Fact]
    public async Task Can_Query_Teacher_With_Include_School_Then_District()
    {
        var setup = await CreateTeacherGraphAsync();

        using var context = citus.CreateContext();
        var teacher = await context
            .Teachers.Include(t => t.School)
            .ThenInclude(s => s.District)
            .SingleAsync(t => t.Id == setup.TeacherId, TestContext.Current.CancellationToken);

        Assert.NotNull(teacher.School);
        Assert.NotNull(teacher.School.District);
        Assert.Equal(setup.SchoolId, teacher.School.Id);
        Assert.Equal(setup.DistrictId, teacher.School.District.Id);
        Assert.Equal(setup.DistrictName, teacher.School.District.Name);
    }

    [Fact]
    public async Task Can_Query_Students_For_School()
    {
        var setup = await CreateTeacherGraphAsync();

        using var context = citus.CreateContext();
        var students = await context
            .Students.Include(s => s.School)
            .Where(s => s.DistrictId == setup.DistrictId && s.SchoolId == setup.SchoolId)
            .OrderBy(s => s.Name)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(4, students.Count);
        Assert.Equal(
            setup.StudentIds.OrderBy(id => id),
            students.Select(s => s.Id).OrderBy(id => id)
        );
        Assert.All(
            students,
            student =>
            {
                Assert.Equal(setup.DistrictId, student.DistrictId);
                Assert.Equal(setup.SchoolId, student.SchoolId);
                Assert.NotNull(student.School);
                Assert.Equal(setup.SchoolId, student.School.Id);
                Assert.Equal(setup.SchoolName, student.School.Name);
            }
        );
    }

    private async Task<TeacherGraphSetup> CreateTeacherGraphAsync()
    {
        using var context = citus.CreateContext();

        var districtId = Guid.CreateVersion7();
        var schoolId = Guid.CreateVersion7();
        var teacherId = Guid.CreateVersion7();
        var studentIds = Enumerable.Range(1, 4).Select(_ => Guid.CreateVersion7()).ToArray();

        var district = new District
        {
            Id = districtId,
            Name = $"Hamilton Township Public School District {teacherId}",
        };

        var school = new School
        {
            Id = schoolId,
            Name = $"George Washington High School {teacherId}",
            District = district,
        };

        var teacher = new Teacher
        {
            Id = teacherId,
            District = district,
            Name = $"Sandra Chen {teacherId}",
            School = school,
        };

        var students = studentIds
            .Select(
                (studentId, index) =>
                    new Student
                    {
                        Id = studentId,
                        District = district,
                        Name = $"Student {index + 1} {teacherId}",
                        SchoolId = schoolId,
                        School = school,
                    }
            )
            .ToArray();

        context.Add(teacher);
        context.AddRange(students);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new TeacherGraphSetup(
            district.Id,
            school.Id,
            teacher.Id,
            studentIds,
            district.Name,
            school.Name,
            teacher.Name
        );
    }
}

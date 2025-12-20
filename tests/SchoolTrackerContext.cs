using Microsoft.EntityFrameworkCore;

public class SchoolTrackContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<District> Districts => Set<District>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
}

public class District
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class School
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public District District { get; set; } = null!;
}

public class Student
{
    public Guid Id { get; set; }
    public Guid DistrictId { get; set; }
    public string Name { get; set; } = string.Empty;
    public School School { get; set; } = null!;
}

public class Teacher
{
    public Guid Id { get; set; }
    public Guid DistrictId { get; set; }
    public string Name { get; set; } = string.Empty;
    public School School { get; set; } = null!;
}

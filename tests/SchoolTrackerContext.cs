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

[PrimaryKey(nameof(Id), nameof(DistrictId))]
public class School
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid DistrictId { get; set; }
    public District District { get; set; } = null!;
}

[EntityTypeConfiguration(typeof(StudentConfiguration))]
[PrimaryKey(nameof(Id), nameof(DistrictId))]
public class Student
{
    public Guid Id { get; set; }
    public Guid DistrictId { get; set; }
    public District District { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public School School { get; set; } = null!;
}

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Student> builder
    )
    {
        builder
            .HasOne(s => s.School)
            .WithMany()
            .HasForeignKey(s => new { s.Id, s.DistrictId })
            .HasPrincipalKey(sc => new { sc.Id, sc.DistrictId });
    }
}

[EntityTypeConfiguration(typeof(TeacherConfiguration))]
[PrimaryKey(nameof(Id), nameof(DistrictId))]
public class Teacher
{
    public Guid Id { get; set; }
    public Guid DistrictId { get; set; }
    public District District { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public School School { get; set; } = null!;
}

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Teacher> builder
    )
    {
        builder
            .HasOne(t => t.School)
            .WithMany()
            .HasForeignKey(t => new { t.Id, t.DistrictId })
            .HasPrincipalKey(sc => new { sc.Id, sc.DistrictId });
    }
}

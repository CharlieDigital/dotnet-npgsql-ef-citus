using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

public class SchoolTrackContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<District> Districts => Set<District>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<SchoolType> SchoolTypes => Set<SchoolType>();
}

public class District
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// 👇 Primary key includes both the ID and the distribution key
[PrimaryKey(nameof(Id), nameof(DistrictId))]
public class School
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid DistrictId { get; set; }
    public District District { get; set; } = null!;
    public Guid SchoolTypeId { get; set; }
    // TODO: this fails with error "Reference tables and local tables can only have foreign keys to reference tables and local tables" when the first distributed table is created.
    // public SchoolType SchoolType { get; set; } = null!;
}

/// <summary>
/// An example of a reference/lookup table which does not have a distribution key
/// (`district_id`)
/// </summary>
public class SchoolType
{
    public Guid Id { get; set; }

    /// <summary>
    /// Primary, Elementary, Middle, High, etc.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

// 👇 Primary key includes both the ID and the distribution key
[PrimaryKey(nameof(DistrictId), nameof(Id))]
[EntityTypeConfiguration(typeof(StudentConfiguration))]
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
        // Have to manually configure the FK to have both fields.  The `PrincipalKey`
        // Translates to the referenced table's PK, which in this case includes both
        // FOREIGN KEY (district_id, school_id) REFERENCES schools(district_id, id)
        builder
            .HasOne(s => s.School)
            .WithMany()
            .HasForeignKey(s => new { s.DistrictId, s.Id })
            .HasPrincipalKey(sc => new { sc.DistrictId, sc.Id });
    }
}

// 👇 Primary key includes both the ID and the distribution key
[PrimaryKey(nameof(DistrictId), nameof(Id))]
[EntityTypeConfiguration(typeof(TeacherConfiguration))]
public class Teacher
{
    public Guid Id { get; set; }
    public Guid DistrictId { get; set; }
    public District District { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public Guid SchoolId { get; set; }
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
            .HasForeignKey(teacher => new { teacher.DistrictId, teacher.SchoolId })
            .HasPrincipalKey(school => new { school.DistrictId, school.Id });
    }
}

using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity.Data;

public class ContosoUniversityContext : DbContext
{
    public ContosoUniversityContext(DbContextOptions<ContosoUniversityContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Instructor> Instructors { get; set; } = null!;
    public DbSet<Cours> Courses { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names and primary keys
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(e => e.StudentID);
            entity.Property(e => e.FirstName).IsRequired(false);
            entity.Property(e => e.LastName).IsRequired(false);
            entity.Property(e => e.Email).IsRequired(false);
        });

        modelBuilder.Entity<Instructor>(entity =>
        {
            entity.ToTable("Instructors");
            entity.HasKey(e => e.InstructorID);
            entity.Property(e => e.FirstName).IsRequired(false);
            entity.Property(e => e.LastName).IsRequired(false);
            entity.Property(e => e.Email).IsRequired(false);
        });

        modelBuilder.Entity<Cours>(entity =>
        {
            entity.ToTable("Courses");
            entity.HasKey(e => e.CourseID);
            entity.Property(e => e.CourseName).IsRequired(false);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(e => e.DepartmentID);
            entity.Property(e => e.DepartmentName).IsRequired(false);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollment");
            entity.HasKey(e => e.EnrollmentID);
        });
    }
}

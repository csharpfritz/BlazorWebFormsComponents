using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity.Data;

public class ContosoUniversityContext : DbContext
{
    public ContosoUniversityContext(DbContextOptions<ContosoUniversityContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<Instructor> Instructors { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Table names from EDMX (PLURAL except Enrollment)
        modelBuilder.Entity<Course>().ToTable("Courses");
        modelBuilder.Entity<Department>().ToTable("Departments");
        modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
        modelBuilder.Entity<Instructor>().ToTable("Instructors");
        modelBuilder.Entity<Student>().ToTable("Students");

        // Configure relationships
        modelBuilder.Entity<Course>()
            .HasOne<Department>()
            .WithMany()
            .HasForeignKey(c => c.DepartmentID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Course>()
            .HasOne<Instructor>()
            .WithMany()
            .HasForeignKey(c => c.InstructorID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasOne<Course>()
            .WithMany()
            .HasForeignKey(e => e.CourseID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrollment>()
            .HasOne<Student>()
            .WithMany()
            .HasForeignKey(e => e.StudentID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

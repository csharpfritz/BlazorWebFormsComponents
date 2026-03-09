using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Models;

public class ContosoUniversityContext : DbContext
{
    public ContosoUniversityContext(DbContextOptions<ContosoUniversityContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Cours> Courses { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Instructor> Instructors { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Map to existing table names in the database
        modelBuilder.Entity<Student>().ToTable("Students").HasKey(s => s.StudentID);
        modelBuilder.Entity<Cours>().ToTable("Courses").HasKey(c => c.CourseID);
        modelBuilder.Entity<Department>().ToTable("Departments").HasKey(d => d.DepartmentID);
        modelBuilder.Entity<Instructor>().ToTable("Instructors").HasKey(i => i.InstructorID);
        modelBuilder.Entity<Enrollment>().ToTable("Enrollment").HasKey(e => e.EnrollmentID);

        modelBuilder.Entity<Cours>()
            .HasOne(c => c.Department)
            .WithMany(d => d.Courses)
            .HasForeignKey(c => c.DepartmentID);

        modelBuilder.Entity<Cours>()
            .HasOne(c => c.Instructor)
            .WithMany(i => i.Courses)
            .HasForeignKey(c => c.InstructorID);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentID);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseID);
    }
}

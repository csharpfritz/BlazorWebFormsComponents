using System;
using System.Collections.Generic;
using AfterContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace AfterContosoUniversity.Data;

public partial class SchoolContext : DbContext
{
    public SchoolContext()
    {
    }

    public SchoolContext(DbContextOptions<SchoolContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Instructor> Instructors { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Connection string is configured via dependency injection in Program.cs
        // This fallback is only used for design-time tools (e.g., migrations)
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ContosoUniversity;Trusted_Connection=True;MultipleActiveResultSets=true");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Cyrillic_General_CI_AS");

        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.CourseName).HasMaxLength(30);
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.InstructorId).HasColumnName("InstructorID");

            entity.HasOne(d => d.Department).WithMany(p => p.Courses)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Courses_Departments");

            entity.HasOne(d => d.Instructor).WithMany(p => p.Courses)
                .HasForeignKey(d => d.InstructorId)
                .HasConstraintName("FK_Courses_Instructors");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.DepartmentName)
                .HasMaxLength(20)
                .IsFixedLength();
            entity.Property(e => e.ManagingInstructorId).HasColumnName("ManagingInstructorID");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.ToTable("Enrollment");

            entity.Property(e => e.EnrollmentId).HasColumnName("EnrollmentID");
            entity.Property(e => e.CourseId).HasColumnName("CourseID");
            entity.Property(e => e.StudentId).HasColumnName("StudentID");

            entity.HasOne(d => d.Course).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK_Enrollment_Courses");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_Enrollment_Students");
        });

        modelBuilder.Entity<Instructor>(entity =>
        {
            entity.Property(e => e.InstructorId).HasColumnName("InstructorID");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.FirstName).HasMaxLength(20);
            entity.Property(e => e.LastName).HasMaxLength(20);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.FirstName).HasMaxLength(20);
            entity.Property(e => e.LastName).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

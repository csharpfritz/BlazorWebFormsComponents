using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Models
{
    public partial class ContosoUniversityEntities : DbContext
    {
        public ContosoUniversityEntities(DbContextOptions<ContosoUniversityEntities> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Map to actual database table names
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
        }

        public virtual DbSet<Cours> Courses { get; set; }
        public virtual DbSet<Department> Departments { get; set; }
        public virtual DbSet<Enrollment> Enrollments { get; set; }
        public virtual DbSet<Instructor> Instructors { get; set; }
        public virtual DbSet<Student> Students { get; set; }
    }
}


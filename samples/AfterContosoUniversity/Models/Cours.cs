// TODO: Review — auto-copied from Web Forms source
using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Models
{
    public partial class Cours
    {
        public Cours()
        {
            this.Enrollments = new HashSet<Enrollment>();
        }

        [Key]
        public int CourseID { get; set; }
        public string? CourseName { get; set; }
        public int StudentsMax { get; set; }
        public int DepartmentID { get; set; }
        public int InstructorID { get; set; }

        public virtual Department? Department { get; set; }
        public virtual Instructor? Instructor { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }
}


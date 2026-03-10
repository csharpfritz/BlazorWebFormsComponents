// TODO: Review — auto-copied from Web Forms source
using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Models
{
    public partial class Instructor
    {
        public Instructor()
        {
            this.Courses = new HashSet<Cours>();
        }

        [Key]
        public int InstructorID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Email { get; set; }

        public virtual ICollection<Cours> Courses { get; set; }
    }
}


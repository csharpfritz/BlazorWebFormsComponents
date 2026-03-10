// TODO: Review — auto-copied from Web Forms source
using System.ComponentModel.DataAnnotations;

namespace ContosoUniversity.Models
{
    public partial class Department
    {
        public Department()
        {
            this.Courses = new HashSet<Cours>();
        }

        [Key]
        public int DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public int BuildingNumber { get; set; }
        public int ManagingInstructorID { get; set; }

        public virtual ICollection<Cours> Courses { get; set; }
    }
}


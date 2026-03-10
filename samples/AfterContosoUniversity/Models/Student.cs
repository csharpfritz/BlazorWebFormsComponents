namespace ContosoUniversity.Models
{
    using System.ComponentModel.DataAnnotations;
    
    public partial class Student
    {
        public Student()
        {
            this.Enrollments = new HashSet<Enrollment>();
        }

        [Key]
        public int StudentID { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public string? Email { get; set; }
    
        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }
}


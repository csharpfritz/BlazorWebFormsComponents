namespace ContosoUniversity.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    
    public partial class Enrollment
    {
        [Key]
        public int EnrollmentID { get; set; }
        
        [Column("Date")]
        public DateTime? EnrollmentDate { get; set; }
        
        public int StudentID { get; set; }
        public int CourseID { get; set; }
    
        public virtual Cours? Cours { get; set; }
        public virtual Student? Student { get; set; }
    }
}


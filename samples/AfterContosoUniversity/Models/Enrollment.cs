namespace ContosoUniversity.Models;

public class Enrollment
{
    public int EnrollmentID { get; set; }
    public DateTime Date { get; set; }
    public int StudentID { get; set; }
    public int CourseID { get; set; }

    public virtual Student? Student { get; set; }
    public virtual Cours? Course { get; set; }
}

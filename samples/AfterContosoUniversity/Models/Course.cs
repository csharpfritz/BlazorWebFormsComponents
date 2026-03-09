namespace ContosoUniversity.Models;

public class Course
{
    public int CourseID { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int StudentsMax { get; set; }
    public int DepartmentID { get; set; }
    public int InstructorID { get; set; }

    public virtual Department? Department { get; set; }
    public virtual Instructor? Instructor { get; set; }
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

namespace ContosoUniversity.Models;

public partial class Cours
{
    public Cours()
    {
        Enrollments = new HashSet<Enrollment>();
    }

    public int CourseID { get; set; }
    public string? CourseName { get; set; }
    public int StudentsMax { get; set; }
    public int DepartmentID { get; set; }
    public int InstructorID { get; set; }

    public virtual Department? Department { get; set; }
    public virtual Instructor? Instructor { get; set; }
    public virtual ICollection<Enrollment> Enrollments { get; set; }
}


namespace ContosoUniversity.Models;

public class Course
{
    public int CourseID { get; set; }
    public string? CourseName { get; set; }
    public int StudentsMax { get; set; }
    public int DepartmentID { get; set; }
    public int InstructorID { get; set; }
}

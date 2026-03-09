namespace ContosoUniversity.Models;

public class Instructor
{
    public int InstructorID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Email { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}

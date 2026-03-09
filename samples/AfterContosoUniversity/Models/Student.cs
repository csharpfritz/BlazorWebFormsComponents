namespace ContosoUniversity.Models;

public class Student
{
    public int StudentID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Email { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";

    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

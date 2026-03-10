namespace ContosoUniversity.Models;

public partial class Student
{
    public Student()
    {
        Enrollments = new HashSet<Enrollment>();
    }

    public int StudentID { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime BirthDate { get; set; }
    public string? Email { get; set; }

    public virtual ICollection<Enrollment> Enrollments { get; set; }
}


namespace ContosoUniversity.Models;

public partial class Instructor
{
    public Instructor()
    {
        Courses = new HashSet<Cours>();
    }

    public int InstructorID { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime BirthDate { get; set; }
    public string? Email { get; set; }

    public virtual ICollection<Cours> Courses { get; set; }
}


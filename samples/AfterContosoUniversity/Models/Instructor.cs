namespace ContosoUniversity.Models;

public class Instructor
{
    public int InstructorID { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public string? Email { get; set; }
}



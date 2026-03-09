namespace ContosoUniversity.Models;

// ViewModel for student grid display with computed properties
public class StudentDisplayModel
{
    public int ID { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Date { get; set; }
    public int Count { get; set; }
}

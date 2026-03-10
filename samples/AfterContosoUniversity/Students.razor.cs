using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;

namespace ContosoUniversity;

public class StudentViewModel
{
    public int ID { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public partial class Students : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<StudentViewModel> _students = new();
    private List<Cours> _courses = new();
    private Student? _selectedStudent;

    private string? _firstName;
    private string? _lastName;
    private string? _birthDate;
    private string? _email;
    private string? _selectedCourseId;
    private string? _searchName;

    protected override async Task OnInitializedAsync()
    {
        await LoadStudents();
        await LoadCourses();
    }

    private async Task LoadStudents()
    {
        using var db = DbFactory.CreateDbContext();
        _students = await db.Students
            .Include(s => s.Enrollments)
            .Select(s => new StudentViewModel
            {
                ID = s.StudentID,
                FullName = s.FirstName + " " + s.LastName,
                Email = s.Email,
                Date = s.Enrollments.Any() ? s.Enrollments.Min(e => e.Date) : DateTime.MinValue,
                Count = s.Enrollments.Count
            })
            .ToListAsync();
    }

    private async Task LoadCourses()
    {
        using var db = DbFactory.CreateDbContext();
        _courses = await db.Courses.ToListAsync();
    }

    private async Task btnInsert_Click()
    {
        if (!string.IsNullOrWhiteSpace(_firstName) && !string.IsNullOrWhiteSpace(_lastName))
        {
            using var db = DbFactory.CreateDbContext();
            
            var student = new Student
            {
                FirstName = _firstName,
                LastName = _lastName,
                Email = _email,
                BirthDate = DateTime.TryParse(_birthDate, out var bd) ? bd : DateTime.Now
            };
            
            db.Students.Add(student);
            await db.SaveChangesAsync();
            
            if (int.TryParse(_selectedCourseId, out var courseId))
            {
                db.Enrollments.Add(new Enrollment
                {
                    StudentID = student.StudentID,
                    CourseID = courseId,
                    Date = DateTime.Now
                });
                await db.SaveChangesAsync();
            }

            await LoadStudents();
            _firstName = _lastName = _birthDate = _email = null;
        }
    }

    private async Task grv_DeleteItem(int studentId)
    {
        using var db = DbFactory.CreateDbContext();
        var student = await db.Students
            .Include(s => s.Enrollments)
            .FirstOrDefaultAsync(s => s.StudentID == studentId);
        
        if (student != null)
        {
            db.Enrollments.RemoveRange(student.Enrollments);
            db.Students.Remove(student);
            await db.SaveChangesAsync();
            await LoadStudents();
        }
    }

    private async Task btnSearch_Click()
    {
        if (!string.IsNullOrWhiteSpace(_searchName))
        {
            using var db = DbFactory.CreateDbContext();
            _selectedStudent = await db.Students
                .FirstOrDefaultAsync(s => 
                    (s.FirstName != null && s.FirstName.Contains(_searchName)) ||
                    (s.LastName != null && s.LastName.Contains(_searchName)));
        }
    }
}


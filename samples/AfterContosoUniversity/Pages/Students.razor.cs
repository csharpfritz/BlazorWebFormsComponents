// Layer2-transformed
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;
using System.Globalization;

namespace ContosoUniversity.Pages;

public class StudentViewModel
{
    public int ID { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Date { get; set; } = "";
    public int Count { get; set; }
}

public partial class Students : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<StudentViewModel> _studentData = new();
    private List<Course> _courses = new();
    private Student? _foundStudent;
    
    private string _firstName = "";
    private string _lastName = "";
    private string _birthDate = "";
    private string _email = "";
    private string _selectedCourseId = "";
    private string _searchTerm = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        
        _courses = await db.Courses.OrderBy(c => c.CourseName).ToListAsync();
        
        var enrollments = await db.Enrollments
            .Include(e => e.Student)
            .ToListAsync();
        
        _studentData = enrollments
            .GroupBy(e => new { e.Student!.StudentID, e.Date, e.Student.FirstName, e.Student.LastName, e.Student.Email })
            .Select(g => new StudentViewModel
            {
                ID = g.Key.StudentID,
                FullName = $"{g.Key.FirstName} {g.Key.LastName}",
                Email = g.Key.Email ?? "",
                Date = g.Key.Date.ToShortDateString(),
                Count = g.Count()
            })
            .ToList();
    }

    private async Task InsertStudent()
    {
        if (string.IsNullOrWhiteSpace(_firstName) || string.IsNullOrWhiteSpace(_lastName))
            return;
        
        if (!DateTime.TryParseExact(_birthDate, new[] { "dd.MM.yyyy", "MM/dd/yyyy", "yyyy-MM-dd" }, 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
            return;
        
        if (!int.TryParse(_selectedCourseId, out var courseId))
            return;

        await using var db = await DbFactory.CreateDbContextAsync();
        
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.FirstName == _firstName && s.LastName == _lastName);
        
        if (student == null)
        {
            student = new Student
            {
                FirstName = _firstName,
                LastName = _lastName,
                BirthDate = birthDate,
                Email = string.IsNullOrWhiteSpace(_email) ? "Has not specified" : _email
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();
        }
        
        var enrollment = new Enrollment
        {
            StudentID = student.StudentID,
            CourseID = courseId,
            Date = DateTime.Now
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();
        
        ClearForm();
        await LoadData();
    }

    private void ClearForm()
    {
        _firstName = "";
        _lastName = "";
        _birthDate = "";
        _email = "";
        _selectedCourseId = "";
        StateHasChanged();
    }

    private async Task SearchStudent()
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
            return;
        
        var parts = _searchTerm.Split(' ', 2);
        if (parts.Length < 2)
            return;
        
        await using var db = await DbFactory.CreateDbContextAsync();
        _foundStudent = await db.Students
            .FirstOrDefaultAsync(s => s.FirstName == parts[0] && s.LastName == parts[1]);
    }
}


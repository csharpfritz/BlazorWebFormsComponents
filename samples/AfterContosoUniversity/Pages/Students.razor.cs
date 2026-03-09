using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;

namespace ContosoUniversity.Pages;

public partial class Students : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    // GridView data
    private List<StudentEnrollmentView> _gridData = new();
    
    // Form fields
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _birthDate = string.Empty;
    private string _email = string.Empty;
    private string _selectedCourse = string.Empty;
    private List<string> _courses = new();
    
    // Search
    private string _searchText = string.Empty;
    private StudentSearchResult? _searchResult;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        
        // Load courses for dropdown
        _courses = await context.Courses.Select(c => c.CourseName).ToListAsync();
        if (_courses.Any() && string.IsNullOrEmpty(_selectedCourse))
            _selectedCourse = _courses.First();

        // Load student enrollment data for grid
        _gridData = await (
            from e in context.Enrollments
            join s in context.Students on e.StudentID equals s.StudentID
            group e by new { s.StudentID, s.FirstName, s.LastName, s.Email, e.Date } into g
            select new StudentEnrollmentView
            {
                ID = g.Key.StudentID,
                FullName = $"{g.Key.FirstName} {g.Key.LastName}",
                Email = g.Key.Email,
                Date = g.Key.Date.ToShortDateString(),
                Count = g.Count()
            }).ToListAsync();
    }

    // GridView SelectMethod signature
    public IQueryable<StudentEnrollmentView> grv_GetData(int maxRows, int startRowIndex, string sortByExpression, out int totalRowCount)
    {
        totalRowCount = _gridData.Count;
        return _gridData.AsQueryable().Skip(startRowIndex).Take(maxRows);
    }

    // GridView DeleteMethod
    public async Task grv_DeleteItem(int id)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        var student = await context.Students.FindAsync(id);
        if (student != null)
        {
            // Remove enrollments first
            var enrollments = context.Enrollments.Where(e => e.StudentID == id);
            context.Enrollments.RemoveRange(enrollments);
            context.Students.Remove(student);
            await context.SaveChangesAsync();
        }
        await LoadDataAsync();
    }

    // GridView UpdateMethod
    public void grv_UpdateItem(int id)
    {
        // Update handled in grv_RowUpdating
    }

    // GridView RowUpdating event
    public async Task grv_RowUpdating(object sender, object e)
    {
        // This would receive the updated values from the grid
        // For now, reload data
        await LoadDataAsync();
    }

    // Insert button handler
    private async Task btnInsert_Click()
    {
        if (!DateTime.TryParse(_birthDate, out var birthDate))
        {
            // TODO: Show error message
            return;
        }

        await using var context = await DbFactory.CreateDbContextAsync();
        
        // Check if student exists
        var existingStudent = await context.Students
            .FirstOrDefaultAsync(s => s.FirstName == _firstName && s.LastName == _lastName && s.BirthDate == birthDate);

        int studentId;
        if (existingStudent == null)
        {
            var newStudent = new Student
            {
                FirstName = _firstName,
                LastName = _lastName,
                BirthDate = birthDate,
                Email = string.IsNullOrEmpty(_email) ? "Has not specified" : _email
            };
            context.Students.Add(newStudent);
            await context.SaveChangesAsync();
            studentId = newStudent.StudentID;
        }
        else
        {
            studentId = existingStudent.StudentID;
        }

        // Find course
        var course = await context.Courses.FirstOrDefaultAsync(c => c.CourseName == _selectedCourse);
        if (course != null)
        {
            var enrollment = new Enrollment
            {
                StudentID = studentId,
                CourseID = course.CourseID,
                Date = DateTime.Now
            };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();
        }

        await ClearForm();
        await LoadDataAsync();
    }

    // Clear button handler
    private Task btnClear_Click()
    {
        return ClearForm();
    }

    private Task ClearForm()
    {
        _firstName = string.Empty;
        _lastName = string.Empty;
        _birthDate = string.Empty;
        _email = string.Empty;
        if (_courses.Any())
            _selectedCourse = _courses.First();
        return Task.CompletedTask;
    }

    // Search button handler
    private async Task btnSearch_Click()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            _searchResult = null;
            return;
        }

        await using var context = await DbFactory.CreateDbContextAsync();
        
        // Search by first name, last name, or partial match
        var searchTerm = _searchText.Trim();
        var student = await context.Students
            .FirstOrDefaultAsync(s => 
                s.FirstName.Contains(searchTerm) || 
                s.LastName.Contains(searchTerm) ||
                (s.FirstName + " " + s.LastName).Contains(searchTerm));

        if (student != null)
        {
            _searchResult = new StudentSearchResult
            {
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                BirthDate = student.BirthDate.ToShortDateString(),
                StudentID = student.StudentID
            };
        }
        else
        {
            _searchResult = null;
        }
    }

    // View models
    public class StudentEnrollmentView
    {
        public int ID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class StudentSearchResult
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string BirthDate { get; set; } = string.Empty;
        public int StudentID { get; set; }
    }
}


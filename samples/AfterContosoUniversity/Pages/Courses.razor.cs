// Layer2-transformed
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity.Pages;

public partial class Courses : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<Department> _departments = new();
    private List<Course> _courses = new();
    private Course? _selectedCourse;
    private string _selectedDepartmentId = "";
    private string _searchCourse = "";

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        _departments = await db.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
    }

    private async Task SearchCourses()
    {
        if (int.TryParse(_selectedDepartmentId, out var deptId))
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            _courses = await db.Courses
                .Where(c => c.DepartmentID == deptId)
                .OrderBy(c => c.CourseName)
                .ToListAsync();
        }
    }

    private async Task SearchCourseByName()
    {
        if (!string.IsNullOrWhiteSpace(_searchCourse))
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            _selectedCourse = await db.Courses
                .FirstOrDefaultAsync(c => c.CourseName != null && c.CourseName.Contains(_searchCourse));
        }
    }
}


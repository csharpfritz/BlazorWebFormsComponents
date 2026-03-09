using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;

namespace ContosoUniversity.Pages;

public partial class Courses : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<Course> _courses = new();
    private List<string> _departments = new();
    private string _selectedDepartment = string.Empty;
    private string _searchText = string.Empty;
    private Course? _selectedCourse;

    protected override async Task OnInitializedAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        _departments = await context.Departments.Select(d => d.DepartmentName).ToListAsync();
        if (_departments.Any())
            _selectedDepartment = _departments.First();
    }

    private async Task btnSearchCourse_Click()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        var dept = await context.Departments.FirstOrDefaultAsync(d => d.DepartmentName == _selectedDepartment);
        if (dept != null)
        {
            _courses = await context.Courses.Where(c => c.DepartmentID == dept.DepartmentID).ToListAsync();
        }
    }

    private async Task search_Click()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            _selectedCourse = null;
            return;
        }

        await using var context = await DbFactory.CreateDbContextAsync();
        _selectedCourse = await context.Courses
            .FirstOrDefaultAsync(c => c.CourseName.Contains(_searchText));
    }
}


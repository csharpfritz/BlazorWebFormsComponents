using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;

namespace ContosoUniversity;

public partial class Courses : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<Department> _departments = new();
    private List<Cours> _courses = new();
    private string? _selectedDepartmentId;
    private string? _searchText;
    private Cours? _selectedCourse;

    protected override async Task OnInitializedAsync()
    {
        using var db = DbFactory.CreateDbContext();
        _departments = await db.Departments.ToListAsync();
    }

    private async Task btnSearchCourse_Click()
    {
        if (int.TryParse(_selectedDepartmentId, out var deptId))
        {
            using var db = DbFactory.CreateDbContext();
            _courses = await db.Courses
                .Where(c => c.DepartmentID == deptId)
                .ToListAsync();
        }
    }

    private async Task search_Click()
    {
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            using var db = DbFactory.CreateDbContext();
            _selectedCourse = await db.Courses
                .FirstOrDefaultAsync(c => c.CourseName != null && c.CourseName.Contains(_searchText));
        }
    }

    private void grvCourses_PageIndexChanging(BlazorWebFormsComponents.PageChangedEventArgs e)
    {
        // Paging handled by GridView component
    }
}


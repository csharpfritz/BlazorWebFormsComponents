using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using AfterContosoUniversity.Data;
using AfterContosoUniversity.Models;

namespace ContosoUniversity;

public partial class Courses : ComponentBase
{
    [Inject]
    public SchoolContext DbContext { get; set; } = default!;

    // Data for components
    protected List<Department> Departments { get; set; } = new();
    protected List<Course> CoursesForGrid { get; set; } = new();
    protected List<Course> CourseDetails { get; set; } = new();
    protected string? SelectedDepartment { get; set; }
    protected string? CourseSearchText { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Load departments for dropdown
        Departments = await DbContext.Departments.ToListAsync();
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Search courses by selected department.
    /// </summary>
    protected async Task btnSearchCourse_Click()
    {
        if (!string.IsNullOrEmpty(SelectedDepartment))
        {
            CoursesForGrid = await DbContext.Courses
                .Include(c => c.Department)
                .Where(c => c.Department.DepartmentName == SelectedDepartment)
                .ToListAsync();
        }
        StateHasChanged();
    }

    /// <summary>
    /// Search course by course name.
    /// </summary>
    protected async Task search_Click()
    {
        if (!string.IsNullOrEmpty(CourseSearchText))
        {
            CourseDetails = await DbContext.Courses
                .Where(c => c.CourseName.Contains(CourseSearchText))
                .ToListAsync();
        }
        CourseSearchText = string.Empty;
        StateHasChanged();
    }

    /// <summary>
    /// Get autocomplete suggestions for course names.
    /// </summary>
    public async Task<List<string>> GetCourseAutocomplete(string prefixText)
    {
        return await DbContext.Courses
            .Where(c => c.CourseName.StartsWith(prefixText))
            .Select(c => c.CourseName)
            .Take(20)
            .ToListAsync();
    }
}

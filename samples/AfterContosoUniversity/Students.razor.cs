using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using AfterContosoUniversity.Data;
using AfterContosoUniversity.Models;

namespace ContosoUniversity;

/// <summary>
/// DTO for displaying student enrollment data in the grid.
/// </summary>
public class StudentEnrollmentViewModel
{
    public int ID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public partial class Students : ComponentBase
{
    [Inject]
    public SchoolContext DbContext { get; set; } = default!;

    // Data for components
    protected List<Course> CoursesList { get; set; } = new();
    protected List<StudentEnrollmentViewModel> StudentEnrollments { get; set; } = new();
    protected List<Student> SearchResults { get; set; } = new();

    // Form fields
    protected string? FirstName { get; set; }
    protected string? LastName { get; set; }
    protected string? BirthDateText { get; set; }
    protected string? Email { get; set; }
    protected string? SelectedCourse { get; set; }
    protected string? SearchText { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Load courses for dropdown
        CoursesList = await DbContext.Courses.ToListAsync();
        // Load student enrollment data
        await LoadStudentEnrollments();
        await base.OnInitializedAsync();
    }

    private async Task LoadStudentEnrollments()
    {
        StudentEnrollments = await DbContext.Students
            .Include(s => s.Enrollments)
            .Select(s => new StudentEnrollmentViewModel
            {
                ID = s.StudentId,
                FullName = s.FirstName + " " + s.LastName,
                Email = s.Email,
                Date = s.Enrollments.Any() ? s.Enrollments.Min(e => e.Date) : DateTime.MinValue,
                Count = s.Enrollments.Count
            })
            .ToListAsync();
    }

    /// <summary>
    /// SelectMethod for GridView - supports paging/sorting.
    /// </summary>
    public IQueryable<StudentEnrollmentViewModel> grv_GetData(
        int maxRows,
        int startRowIndex,
        string sortByExpression,
        out int totalRowCount)
    {
        totalRowCount = StudentEnrollments.Count;
        return StudentEnrollments.Skip(startRowIndex).Take(maxRows).AsQueryable();
    }

    /// <summary>
    /// Delete a student by ID.
    /// </summary>
    public async Task grv_DeleteItem(int id)
    {
        var student = await DbContext.Students.FindAsync(id);
        if (student != null)
        {
            DbContext.Students.Remove(student);
            await DbContext.SaveChangesAsync();
            await LoadStudentEnrollments();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Update student data.
    /// </summary>
    public async Task grv_UpdateItem(int id, string fullName, string email)
    {
        var student = await DbContext.Students.FindAsync(id);
        if (student != null)
        {
            var names = fullName.Split(' ', 2);
            student.FirstName = names.Length > 0 ? names[0] : string.Empty;
            student.LastName = names.Length > 1 ? names[1] : string.Empty;
            student.Email = email;
            await DbContext.SaveChangesAsync();
            await LoadStudentEnrollments();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Insert new student enrollment.
    /// </summary>
    protected async Task btnInsert_Click()
    {
        if (!DateTime.TryParse(BirthDateText, out var birthDate))
        {
            throw new Exception("Wrong Date Format!");
        }

        var student = new Student
        {
            FirstName = FirstName ?? string.Empty,
            LastName = LastName ?? string.Empty,
            BirthDate = birthDate,
            Email = Email
        };

        DbContext.Students.Add(student);
        await DbContext.SaveChangesAsync();

        // Create enrollment if a course was selected
        if (!string.IsNullOrEmpty(SelectedCourse))
        {
            var course = await DbContext.Courses.FirstOrDefaultAsync(c => c.CourseName == SelectedCourse);
            if (course != null)
            {
                DbContext.Enrollments.Add(new Enrollment
                {
                    StudentId = student.StudentId,
                    CourseId = course.CourseId,
                    Date = DateTime.Now
                });
                await DbContext.SaveChangesAsync();
            }
        }

        await LoadStudentEnrollments();
        btnClear_Click();
        StateHasChanged();
    }

    /// <summary>
    /// Clear form fields.
    /// </summary>
    protected void btnClear_Click()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        BirthDateText = string.Empty;
        Email = string.Empty;
        SelectedCourse = null;
    }

    /// <summary>
    /// Search for students by name.
    /// </summary>
    protected async Task btnSearch_Click()
    {
        if (!string.IsNullOrEmpty(SearchText))
        {
            SearchResults = await DbContext.Students
                .Where(s => (s.FirstName + " " + s.LastName).Contains(SearchText))
                .ToListAsync();
        }
        SearchText = string.Empty;
        StateHasChanged();
    }

    /// <summary>
    /// Get autocomplete suggestions for student names.
    /// </summary>
    public async Task<List<string>> GetStudentAutocomplete(string prefixText)
    {
        return await DbContext.Students
            .Where(s => s.FirstName.StartsWith(prefixText))
            .Select(s => s.FirstName + " " + s.LastName)
            .Take(20)
            .ToListAsync();
    }
}

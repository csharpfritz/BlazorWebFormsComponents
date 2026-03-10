using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;

namespace ContosoUniversity;

public partial class Instructors : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<Instructor> _instructors = new();
    private bool _sortAscending = true;
    private string _sortField = "InstructorID";

    protected override async Task OnInitializedAsync()
    {
        await LoadInstructors();
    }

    private async Task LoadInstructors()
    {
        using var db = DbFactory.CreateDbContext();
        
        IQueryable<Instructor> query = db.Instructors;
        
        query = (_sortField, _sortAscending) switch
        {
            ("InstructorID", true) => query.OrderBy(i => i.InstructorID),
            ("InstructorID", false) => query.OrderByDescending(i => i.InstructorID),
            ("FirstName", true) => query.OrderBy(i => i.FirstName),
            ("FirstName", false) => query.OrderByDescending(i => i.FirstName),
            ("LastName", true) => query.OrderBy(i => i.LastName),
            ("LastName", false) => query.OrderByDescending(i => i.LastName),
            _ => query.OrderBy(i => i.InstructorID)
        };
        
        _instructors = await query.ToListAsync();
    }

    private async Task grvInstructors_Sorting(BlazorWebFormsComponents.GridViewSortEventArgs e)
    {
        if (e.SortExpression == _sortField)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortField = e.SortExpression ?? "InstructorID";
            _sortAscending = true;
        }
        
        await LoadInstructors();
    }
}


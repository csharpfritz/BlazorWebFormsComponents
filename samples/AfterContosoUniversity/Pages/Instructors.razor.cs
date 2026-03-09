// Layer2-transformed
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using BlazorWebFormsComponents;
using ContosoUniversity.Models;

namespace ContosoUniversity.Pages;

public partial class Instructors : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<Instructor> _instructors = new();
    private string _sortColumn = "InstructorID";
    private bool _sortAscending = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadInstructors();
    }

    private async Task LoadInstructors()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        
        IQueryable<Instructor> query = db.Instructors;
        
        query = _sortColumn switch
        {
            "FirstName" => _sortAscending ? query.OrderBy(i => i.FirstName) : query.OrderByDescending(i => i.FirstName),
            "LastName" => _sortAscending ? query.OrderBy(i => i.LastName) : query.OrderByDescending(i => i.LastName),
            _ => _sortAscending ? query.OrderBy(i => i.InstructorID) : query.OrderByDescending(i => i.InstructorID)
        };
        
        _instructors = await query.ToListAsync();
    }

    private async Task HandleSorting(GridViewSortEventArgs e)
    {
        if (_sortColumn == e.SortExpression)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = e.SortExpression;
            _sortAscending = true;
        }
        
        await LoadInstructors();
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using AfterContosoUniversity.Data;
using AfterContosoUniversity.Models;

namespace ContosoUniversity;

public partial class Instructors : ComponentBase
{
    [Inject]
    public SchoolContext DbContext { get; set; } = default!;

    protected List<Instructor> InstructorsList { get; set; } = new();
    protected string SortDirection { get; set; } = "desc";
    protected string? CurrentSortExpression { get; set; }

    protected override async Task OnInitializedAsync()
    {
        InstructorsList = await DbContext.Instructors.ToListAsync();
        await base.OnInitializedAsync();
    }

    /// <summary>
    /// Handle sorting of the instructors grid.
    /// </summary>
    protected void grvInstructors_Sorting(string sortExpression)
    {
        CurrentSortExpression = sortExpression;
        
        var query = DbContext.Instructors.AsQueryable();
        
        InstructorsList = sortExpression switch
        {
            "InstructorID" => SortDirection == "asc" 
                ? query.OrderBy(i => i.InstructorId).ToList() 
                : query.OrderByDescending(i => i.InstructorId).ToList(),
            "FirstName" => SortDirection == "asc" 
                ? query.OrderBy(i => i.FirstName).ToList() 
                : query.OrderByDescending(i => i.FirstName).ToList(),
            "LastName" => SortDirection == "asc" 
                ? query.OrderBy(i => i.LastName).ToList() 
                : query.OrderByDescending(i => i.LastName).ToList(),
            _ => query.ToList()
        };

        // Toggle sort direction
        SortDirection = SortDirection == "asc" ? "desc" : "asc";
        StateHasChanged();
    }
}

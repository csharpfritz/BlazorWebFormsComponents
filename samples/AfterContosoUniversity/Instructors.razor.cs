using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;
using BlazorWebFormsComponents;

namespace ContosoUniversity
{
    public partial class Instructors : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityEntities> DbFactory { get; set; } = default!;

        private List<Instructor> instructors = new();
        private string sortExpression = "";
        private bool sortAscending = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadInstructors();
        }

        private async Task LoadInstructors()
        {
            using var db = DbFactory.CreateDbContext();
            var query = db.Instructors.AsQueryable();
            
            if (!string.IsNullOrEmpty(sortExpression))
            {
                query = sortExpression switch
                {
                    "InstructorID" => sortAscending ? query.OrderBy(i => i.InstructorID) : query.OrderByDescending(i => i.InstructorID),
                    "FirstName" => sortAscending ? query.OrderBy(i => i.FirstName) : query.OrderByDescending(i => i.FirstName),
                    "LastName" => sortAscending ? query.OrderBy(i => i.LastName) : query.OrderByDescending(i => i.LastName),
                    _ => query
                };
            }
            
            instructors = await query.ToListAsync();
        }

        private async Task grvInstructors_Sorting(GridViewSortEventArgs e)
        {
            if (sortExpression == e.SortExpression)
                sortAscending = !sortAscending;
            else
            {
                sortExpression = e.SortExpression;
                sortAscending = true;
            }
            await LoadInstructors();
        }
    }
}


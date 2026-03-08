using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity
{
    public partial class Instructors : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityEntities> DbFactory { get; set; } = default!;

        private List<Instructor> _instructors = new();
        private string _sortColumn = "InstructorID";
        private bool _sortAscending = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadInstructors();
        }

        private async Task LoadInstructors()
        {
            using var db = DbFactory.CreateDbContext();
            var query = db.Instructors.AsQueryable();

            query = _sortColumn switch
            {
                "FirstName" => _sortAscending ? query.OrderBy(i => i.FirstName) : query.OrderByDescending(i => i.FirstName),
                "LastName" => _sortAscending ? query.OrderBy(i => i.LastName) : query.OrderByDescending(i => i.LastName),
                _ => _sortAscending ? query.OrderBy(i => i.InstructorID) : query.OrderByDescending(i => i.InstructorID)
            };

            _instructors = await query.ToListAsync();
        }

        private async Task grvInstructors_Sorting(string sortExpression)
        {
            if (_sortColumn == sortExpression)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = sortExpression;
                _sortAscending = true;
            }
            await LoadInstructors();
        }
    }
}


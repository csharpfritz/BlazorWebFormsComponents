using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity
{
    public partial class Courses : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityEntities> DbFactory { get; set; } = default!;

        private List<Department> _departments = new();
        private List<Cours> _courses = new();
        private Cours? _selectedCourse;
        private int _selectedDepartmentId;
        private string _searchText = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await LoadDepartments();
        }

        private async Task LoadDepartments()
        {
            using var db = DbFactory.CreateDbContext();
            _departments = await db.Departments.ToListAsync();
        }

        private async Task btnSearchCourse_Click()
        {
            using var db = DbFactory.CreateDbContext();
            if (_selectedDepartmentId > 0)
            {
                _courses = await db.Courses
                    .Where(c => c.DepartmentID == _selectedDepartmentId)
                    .ToListAsync();
            }
            else
            {
                _courses = await db.Courses.ToListAsync();
            }
        }

        private async Task search_Click()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return;

            using var db = DbFactory.CreateDbContext();
            _selectedCourse = await db.Courses
                .Where(c => c.CourseName.Contains(_searchText))
                .FirstOrDefaultAsync();
        }
    }
}


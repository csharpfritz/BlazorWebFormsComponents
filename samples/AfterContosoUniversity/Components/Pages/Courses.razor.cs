// Layer2-transformed
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity.Components.Pages
{
    public partial class Courses : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = null!;

        private List<Department> _departments = new();
        private List<Cours> _courses = new();
        private Cours? _selectedCourse;

        protected override async Task OnInitializedAsync()
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            _departments = await db.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
            _courses = await db.Courses.Include(c => c.Department).ToListAsync();
        }

        private async Task btnSearchCourse_Click(MouseEventArgs e)
        {
            // Filter courses by department
            await Task.CompletedTask;
        }

        private async Task search_Click(MouseEventArgs e)
        {
            // Search course by name
            await Task.CompletedTask;
        }
    }
}

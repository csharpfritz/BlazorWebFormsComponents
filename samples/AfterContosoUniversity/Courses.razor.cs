using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Components.Web;
using BlazorWebFormsComponents;

namespace ContosoUniversity
{
    public partial class Courses : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityEntities> DbFactory { get; set; } = default!;

        private List<Department> departments = new();
        private List<Cours> courses = new();
        private Cours? selectedCourse;
        private string selectedDepartmentId = "";
        private string searchText = "";

        protected override async Task OnInitializedAsync()
        {
            using var db = DbFactory.CreateDbContext();
            departments = await db.Departments.ToListAsync();
        }

        private async Task btnSearchCourse_Click(MouseEventArgs e)
        {
            if (int.TryParse(selectedDepartmentId, out int deptId))
            {
                using var db = DbFactory.CreateDbContext();
                courses = await db.Courses
                    .Where(c => c.DepartmentID == deptId)
                    .ToListAsync();
            }
        }

        private async Task search_Click(MouseEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                using var db = DbFactory.CreateDbContext();
                selectedCourse = await db.Courses
                    .FirstOrDefaultAsync(c => c.CourseName.Contains(searchText));
            }
        }

        private void grvCourses_PageIndexChanged(PageChangedEventArgs e)
        {
            // Paging handled by GridView
        }
    }
}


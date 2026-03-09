// Layer2-transformed
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;
using System.Globalization;

namespace ContosoUniversity.Components.Pages
{
    public partial class Students : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = null!;

        private List<StudentDisplayModel> _studentDisplayModels = new();
        private List<Cours> _courses = new();
        private List<Student> _selectedStudentList = new();

        // Form fields
        private string _firstName = "";
        private string _lastName = "";
        private string _birthDate = "";
        private string _email = "";
        private string _searchText = "";

        protected override async Task OnInitializedAsync()
        {
            await LoadStudentsAsync();
            await using var db = await DbFactory.CreateDbContextAsync();
            _courses = await db.Courses.OrderBy(c => c.CourseName).ToListAsync();
        }

        private async Task LoadStudentsAsync()
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            _studentDisplayModels = await db.Students
                .Include(s => s.Enrollments)
                .Select(s => new StudentDisplayModel
                {
                    ID = s.StudentID,
                    FullName = s.FirstName + " " + s.LastName,
                    Email = s.Email,
                    Date = s.BirthDate.ToString("d"),
                    Count = s.Enrollments.Count
                })
                .ToListAsync();
        }

        private async Task btnInsert_Click(MouseEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_firstName) || string.IsNullOrWhiteSpace(_lastName))
                return;

            // Parse birth date, default to today if invalid
            if (!DateTime.TryParse(_birthDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
            {
                birthDate = DateTime.Today;
            }

            await using var db = await DbFactory.CreateDbContextAsync();
            
            var student = new Student
            {
                FirstName = _firstName,
                LastName = _lastName,
                BirthDate = birthDate,
                Email = string.IsNullOrWhiteSpace(_email) ? null : _email
            };
            
            db.Students.Add(student);
            await db.SaveChangesAsync();

            // Clear form and reload grid
            _firstName = "";
            _lastName = "";
            _birthDate = "";
            _email = "";
            
            await LoadStudentsAsync();
        }

        private void btnClear_Click(MouseEventArgs e)
        {
            _firstName = "";
            _lastName = "";
            _birthDate = "";
            _email = "";
        }

        private async Task btnSearch_Click(MouseEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                _selectedStudentList = new();
                return;
            }

            await using var db = await DbFactory.CreateDbContextAsync();
            var student = await db.Students
                .FirstOrDefaultAsync(s =>
                    (s.FirstName != null && s.FirstName.Contains(_searchText)) ||
                    (s.LastName != null && s.LastName.Contains(_searchText)));
            
            _selectedStudentList = student != null ? new List<Student> { student } : new();
        }
    }
}

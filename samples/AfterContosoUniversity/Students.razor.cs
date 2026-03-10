using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;
using Microsoft.AspNetCore.Components.Web;
using BlazorWebFormsComponents;

namespace ContosoUniversity
{
    public class StudentViewModel
    {
        public int ID { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime? Date { get; set; }
        public int Count { get; set; }
    }

    public partial class Students : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityEntities> DbFactory { get; set; } = default!;

        private List<StudentViewModel> students = new();
        private List<Cours> availableCourses = new();
        private Student? selectedStudent;
        private string selectedCourseId = "";
        private string searchText = "";
        private string txtFirstName = "";
        private string txtLastName = "";
        private string txtBirthDate = "";
        private string txtEmail = "";

        protected override async Task OnInitializedAsync()
        {
            await LoadStudents();
            await LoadCourses();
        }

        private async Task LoadStudents()
        {
            using var db = DbFactory.CreateDbContext();
            students = await db.Students
                .Select(s => new StudentViewModel
                {
                    ID = s.StudentID,
                    FullName = s.FirstName + " " + s.LastName,
                    Email = s.Email ?? "",
                    Date = db.Enrollments.Where(e => e.StudentID == s.StudentID).Select(e => e.EnrollmentDate).FirstOrDefault(),
                    Count = db.Enrollments.Count(e => e.StudentID == s.StudentID)
                })
                .ToListAsync();
        }

        private async Task LoadCourses()
        {
            using var db = DbFactory.CreateDbContext();
            availableCourses = await db.Courses.ToListAsync();
        }

        private async Task btnInsert_Click(MouseEventArgs e)
        {
            using var db = DbFactory.CreateDbContext();
            var student = new Student
            {
                FirstName = txtFirstName,
                LastName = txtLastName,
                Email = txtEmail,
                BirthDate = DateTime.TryParse(txtBirthDate, out var bd) ? bd : DateTime.Now
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();
            await LoadStudents();
            btnClear_Click(e);
        }

        private void btnClear_Click(MouseEventArgs e)
        {
            txtFirstName = "";
            txtLastName = "";
            txtBirthDate = "";
            txtEmail = "";
            StateHasChanged();
        }

        private async Task btnSearch_Click(MouseEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                using var db = DbFactory.CreateDbContext();
                selectedStudent = await db.Students
                    .FirstOrDefaultAsync(s => (s.FirstName + " " + s.LastName).Contains(searchText));
            }
        }
    }
}


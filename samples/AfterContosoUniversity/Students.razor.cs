using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity
{
    public partial class Students : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityEntities> DbFactory { get; set; } = default!;

        private List<StudentViewModel> _students = new();
        private List<Cours> _courses = new();
        private StudentViewModel? _selectedStudent;

        // Form fields
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _birthDate = string.Empty;
        private string _email = string.Empty;
        private int _selectedCourseId;
        private string _searchText = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await LoadStudents();
            await LoadCourses();
        }

        private async Task LoadStudents()
        {
            using var db = DbFactory.CreateDbContext();
            _students = await db.Students
                .Include(s => s.Enrollments)
                .Select(s => new StudentViewModel
                {
                    ID = s.StudentID,
                    FullName = s.FirstName + " " + s.LastName,
                    Email = s.Email,
                    Date = s.BirthDate.ToShortDateString(),
                    Count = s.Enrollments.Count
                })
                .ToListAsync();
        }

        private async Task LoadCourses()
        {
            using var db = DbFactory.CreateDbContext();
            _courses = await db.Courses.ToListAsync();
        }

        private async Task btnInsert_Click()
        {
            if (string.IsNullOrWhiteSpace(_firstName) || string.IsNullOrWhiteSpace(_lastName))
                return;

            using var db = DbFactory.CreateDbContext();
            var student = new Student
            {
                FirstName = _firstName,
                LastName = _lastName,
                BirthDate = DateTime.TryParse(_birthDate, out var bd) ? bd : DateTime.Now,
                Email = _email
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            if (_selectedCourseId > 0)
            {
                db.Enrollments.Add(new Enrollment
                {
                    StudentID = student.StudentID,
                    CourseID = _selectedCourseId,
                    Date = DateTime.Now
                });
                await db.SaveChangesAsync();
            }

            btnClear_Click();
            await LoadStudents();
        }

        private void btnClear_Click()
        {
            _firstName = string.Empty;
            _lastName = string.Empty;
            _birthDate = string.Empty;
            _email = string.Empty;
            _selectedCourseId = 0;
        }

        private async Task btnSearch_Click()
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return;

            using var db = DbFactory.CreateDbContext();
            var student = await db.Students
                .Where(s => (s.FirstName + " " + s.LastName).Contains(_searchText))
                .FirstOrDefaultAsync();

            if (student != null)
            {
                _selectedStudent = new StudentViewModel
                {
                    ID = student.StudentID,
                    FullName = student.FirstName + " " + student.LastName,
                    Email = student.Email,
                    Date = student.BirthDate.ToShortDateString()
                };
            }
        }
    }

    public class StudentViewModel
    {
        public int ID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}


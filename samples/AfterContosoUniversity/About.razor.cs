using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity
{
    public partial class About : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityEntities> DbFactory { get; set; } = default!;

        private List<EnrollmentStatistic> _enrollmentStats = new();

        protected override async Task OnInitializedAsync()
        {
            using var db = DbFactory.CreateDbContext();
            _enrollmentStats = await db.Enrollments
                .GroupBy(e => e.Date.Date)
                .Select(g => new EnrollmentStatistic { Key = g.Key.ToShortDateString(), Value = g.Count() })
                .ToListAsync();
        }
    }

    public class EnrollmentStatistic
    {
        public string Key { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}


using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity
{
    public class EnrollmentStat
    {
        public string Key { get; set; } = "";
        public int Value { get; set; }
    }

    public partial class About : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityEntities> DbFactory { get; set; } = default!;

        private List<EnrollmentStat> stats = new();

        protected override async Task OnInitializedAsync()
        {
            using var db = DbFactory.CreateDbContext();
            stats = await db.Enrollments
                .GroupBy(e => e.EnrollmentDate.HasValue ? e.EnrollmentDate.Value.Year.ToString() : "Unknown")
                .Select(g => new EnrollmentStat { Key = g.Key, Value = g.Count() })
                .ToListAsync();
        }
    }
}


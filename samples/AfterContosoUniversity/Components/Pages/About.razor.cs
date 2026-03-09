// Layer2-transformed
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity.Components.Pages
{
    public partial class About : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = null!;

        private List<EnrollmentStatistic> _enrollmentStats = new();

        protected override async Task OnInitializedAsync()
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            _enrollmentStats = await db.Students
                .GroupBy(s => s.BirthDate.Year)
                .Select(g => new EnrollmentStatistic
                {
                    Key = g.Key.ToString(),
                    Value = g.Count()
                })
                .OrderBy(e => e.Key)
                .ToListAsync();
        }
    }
}

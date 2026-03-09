using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;

namespace ContosoUniversity.Pages;

public partial class About : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<EnrollmentStat> _stats = new();

    protected override async Task OnInitializedAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        
        _stats = await context.Enrollments
            .GroupBy(e => e.Date.Date)
            .Select(g => new EnrollmentStat
            {
                EnrollmentDate = g.Key.ToShortDateString(),
                StudentCount = g.Count()
            })
            .ToListAsync();
    }

    public class EnrollmentStat
    {
        public string EnrollmentDate { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }
}


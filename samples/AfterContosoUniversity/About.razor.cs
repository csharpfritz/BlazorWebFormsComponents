using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;

namespace ContosoUniversity;

public partial class About : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<EnrollmentStatistic> _enrollmentStats = new();

    protected override async Task OnInitializedAsync()
    {
        using var db = DbFactory.CreateDbContext();
        
        // Group enrollments by date and count them
        var stats = await db.Enrollments
            .GroupBy(e => e.Date.Date)
            .Select(g => new EnrollmentStatistic
            {
                Key = g.Key.ToShortDateString(),
                Value = g.Count()
            })
            .ToListAsync();
        
        _enrollmentStats = stats;
    }
}


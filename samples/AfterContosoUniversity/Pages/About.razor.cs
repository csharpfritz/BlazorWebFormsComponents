// Layer2-transformed
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity.Pages;

public class EnrollmentStat
{
    public string EnrollmentDate { get; set; } = "";
    public int StudentCount { get; set; }
}

public partial class About : ComponentBase
{
    [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = default!;

    private List<EnrollmentStat> _enrollmentStats = new();

    protected override async Task OnInitializedAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync();
        
        var stats = await db.Enrollments
            .GroupBy(e => e.Date.Date)
            .Select(g => new EnrollmentStat
            {
                EnrollmentDate = g.Key.ToShortDateString(),
                StudentCount = g.Count()
            })
            .ToListAsync();
        
        _enrollmentStats = stats;
    }
}


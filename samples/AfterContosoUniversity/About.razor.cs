using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using AfterContosoUniversity.Data;
using AfterContosoUniversity.Models;

namespace ContosoUniversity;

/// <summary>
/// DTO for enrollment statistics displayed in the GridView.
/// </summary>
public class EnrollmentStat
{
    public string EnrollmentDate { get; set; } = string.Empty;
    public int StudentCount { get; set; }
}

public partial class About : ComponentBase
{
    [Inject]
    public SchoolContext DbContext { get; set; } = default!;

    protected List<EnrollmentStat> EnrollmentStats { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        EnrollmentStats = await DbContext.Enrollments
            .GroupBy(e => e.Date.Date)
            .Select(g => new EnrollmentStat
            {
                EnrollmentDate = g.Key.ToShortDateString(),
                StudentCount = g.Count()
            })
            .ToListAsync();
        
        await base.OnInitializedAsync();
    }
}

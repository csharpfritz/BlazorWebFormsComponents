// Layer2-transformed
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

namespace ContosoUniversity.Components.Pages
{
    public partial class Instructors : ComponentBase
    {
        [Inject] private IDbContextFactory<ContosoUniversityContext> DbFactory { get; set; } = null!;

        private List<Instructor> _instructors = new();

        protected override async Task OnInitializedAsync()
        {
            await using var db = await DbFactory.CreateDbContextAsync();
            _instructors = await db.Instructors.OrderBy(i => i.LastName).ToListAsync();
        }
    }
}

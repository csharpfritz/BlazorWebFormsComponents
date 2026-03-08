// NOTE: This legacy class is not used in the Blazor migration.
// Data access is now handled in the component code-behinds using IDbContextFactory.
// Kept for reference only.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ContosoUniversity.Models
{
    // This class was used in Web Forms for data access.
    // In Blazor, we use IDbContextFactory<ContosoUniversityEntities> directly in components.
    [Obsolete("Use IDbContextFactory<ContosoUniversityEntities> in Blazor components instead")]
    public class Enrollmet_Logic
    {
        // Legacy method - not functional in Blazor
        // Data loading is done in component OnInitializedAsync methods
    }
}

// ============================================================================
// TODO: Generate EF Core models from your database using:
// dotnet ef dbcontext scaffold "YOUR_CONNECTION_STRING" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context ContosoUniversityEntitiesDbContext --force
// See scaffold-command.txt for full details and options.
// ============================================================================

// Layer2-transformed
using BlazorWebFormsComponents;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();  // Required for BWFC GridView/DetailsView
builder.Services.AddBlazorWebFormsComponents();

// Database
builder.Services.AddDbContextFactory<ContosoUniversityEntities>(options =>
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ContosoUniversity;Trusted_Connection=True"));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseSession();
app.UseAntiforgery();

app.MapRazorComponents<ContosoUniversity.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();




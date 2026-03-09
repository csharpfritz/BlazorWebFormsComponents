// Layer2-transformed
using BlazorWebFormsComponents;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();  // Required for BWFC GridView/DetailsView
builder.Services.AddBlazorWebFormsComponents();

// Database - SQL Server LocalDB (same as original WebForms)
var dbPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "ContosoUniversity", "ContosoUniversity.mdf"));
builder.Services.AddDbContextFactory<ContosoUniversityContext>(options =>
    options.UseSqlServer($@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={dbPath};Integrated Security=True"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();

// Legacy URL redirects
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null && path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
    {
        var newPath = path.Replace(".aspx", "", StringComparison.OrdinalIgnoreCase);
        context.Response.Redirect(newPath, permanent: true);
        return;
    }
    await next();
});

app.MapRazorComponents<ContosoUniversity.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();


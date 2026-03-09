using BlazorWebFormsComponents;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddBlazorWebFormsComponents();

// Database - SQLite for portability
var connectionString = builder.Configuration.GetConnectionString("ContosoUniversity") 
    ?? "Data Source=ContosoUniversity.db";
builder.Services.AddDbContextFactory<ContosoUniversityContext>(options =>
    options.UseSqlite(connectionString));

// Session (for cart-like functionality if needed)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Ensure database is created with seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ContosoUniversityContext>();
    context.Database.EnsureCreated();
    await ContosoUniversity.Data.DbInitializer.InitializeAsync(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// URL rewriting for legacy .aspx URLs
var rewriteOptions = new RewriteOptions()
    .AddRedirect("^Home.aspx$", "/ContosoUniversity/Home", 301)
    .AddRedirect("^About.aspx$", "/ContosoUniversity/About", 301)
    .AddRedirect("^Students.aspx$", "/ContosoUniversity/Students", 301)
    .AddRedirect("^Courses.aspx$", "/ContosoUniversity/Courses", 301)
    .AddRedirect("^Instructors.aspx$", "/ContosoUniversity/Instructors", 301);
app.UseRewriter(rewriteOptions);

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseSession();
app.UseAntiforgery();

app.MapRazorComponents<ContosoUniversity.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();


// TODO: Review and adjust this generated Program.cs for your application needs.
using AfterContosoUniversity.Data;
using BlazorWebFormsComponents;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddBlazorWebFormsComponents();

// Add Entity Framework DbContext with SQLite
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=ContosoUniversity.db"));

var app = builder.Build();

// Ensure database is created with schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchoolContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ASPX URL backward compatibility — 301 redirects for SEO
var rewriteOptions = new RewriteOptions()
    .AddRedirect(@"^Default\.aspx$", "/", statusCode: 301)
    .AddRedirect(@"^(.+)\.aspx$", "$1", statusCode: 301);
app.UseRewriter(rewriteOptions);

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<ContosoUniversity.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

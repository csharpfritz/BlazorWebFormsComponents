// ContosoUniversity Blazor Server App (Migrated from Web Forms)
using BlazorWebFormsComponents;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;
using ContosoUniversity.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddBlazorWebFormsComponents();

// Database - LocalDB (matching original Web Forms config)
builder.Services.AddDbContextFactory<ContosoUniversityContext>(options =>
    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ContosoUniversity;Trusted_Connection=True;MultipleActiveResultSets=true"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ASPX URL backward compatibility — redirect .aspx URLs to Blazor routes
var rewriteOptions = new RewriteOptions()
    .AddRedirect(@"^Default\.aspx$", "/", statusCode: 301)
    .AddRedirect(@"^(.+)\.aspx$", "$1", statusCode: 301);
app.UseRewriter(rewriteOptions);

app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<ContosoUniversity.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();



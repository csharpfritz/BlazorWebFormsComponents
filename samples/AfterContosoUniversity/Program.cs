// Layer2-transformed
using BlazorWebFormsComponents;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Rewrite;
using ContosoUniversity.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddBlazorWebFormsComponents();

// Database — SQL Server LocalDB (preserving original source database technology)
var connectionString = builder.Configuration.GetConnectionString("ContosoUniversity")
    ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ContosoUniversity;Integrated Security=True";
builder.Services.AddDbContextFactory<ContosoUniversityContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ASPX URL Rewriting — Standard pattern for all Web Forms migrations
var rewriteOptions = new RewriteOptions()
    .AddRedirect(@"^Default\.aspx$", "/", statusCode: 301)
    .AddRedirect(@"^(.+)\.aspx$", "$1", statusCode: 301);
app.UseRewriter(rewriteOptions);

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<ContosoUniversity.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();


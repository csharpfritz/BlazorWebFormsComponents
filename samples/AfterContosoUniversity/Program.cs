// Layer2-transformed
using BlazorWebFormsComponents;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.AddBlazorWebFormsComponents();

// Database
builder.Services.AddDbContextFactory<ContosoUniversityEntities>(options =>
    options.UseSqlite("Data Source=contosouniversity.db"));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Ensure database is created and seed sample data
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ContosoUniversityEntities>>();
    using var db = dbFactory.CreateDbContext();
    db.Database.EnsureCreated();
    
    // Seed sample data if empty
    if (!db.Departments.Any())
    {
        var dept1 = new Department { DepartmentName = "Computer Science", BuildingNumber = 1, ManagingInstructorID = 1 };
        var dept2 = new Department { DepartmentName = "Mathematics", BuildingNumber = 2, ManagingInstructorID = 2 };
        db.Departments.AddRange(dept1, dept2);
        db.SaveChanges();

        var instructor1 = new Instructor { FirstName = "John", LastName = "Smith", BirthDate = new DateTime(1970, 1, 1), Email = "john.smith@contoso.edu" };
        var instructor2 = new Instructor { FirstName = "Jane", LastName = "Doe", BirthDate = new DateTime(1975, 5, 15), Email = "jane.doe@contoso.edu" };
        db.Instructors.AddRange(instructor1, instructor2);
        db.SaveChanges();

        var course1 = new Cours { CourseName = "Introduction to Programming", StudentsMax = 30, DepartmentID = dept1.DepartmentID, InstructorID = instructor1.InstructorID };
        var course2 = new Cours { CourseName = "Data Structures", StudentsMax = 25, DepartmentID = dept1.DepartmentID, InstructorID = instructor1.InstructorID };
        var course3 = new Cours { CourseName = "Calculus I", StudentsMax = 35, DepartmentID = dept2.DepartmentID, InstructorID = instructor2.InstructorID };
        db.Courses.AddRange(course1, course2, course3);
        db.SaveChanges();

        var student1 = new Student { FirstName = "Alice", LastName = "Johnson", BirthDate = new DateTime(2000, 3, 10), Email = "alice@student.contoso.edu" };
        var student2 = new Student { FirstName = "Bob", LastName = "Williams", BirthDate = new DateTime(1999, 7, 22), Email = "bob@student.contoso.edu" };
        db.Students.AddRange(student1, student2);
        db.SaveChanges();

        db.Enrollments.Add(new Enrollment { StudentID = student1.StudentID, CourseID = course1.CourseID, Date = DateTime.Now });
        db.Enrollments.Add(new Enrollment { StudentID = student2.StudentID, CourseID = course2.CourseID, Date = DateTime.Now });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ASPX URL Rewriting for backward compatibility - MUST be early in pipeline
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (!string.IsNullOrEmpty(path) && path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
    {
        // Strip .aspx extension: /Page.aspx -> /Page
        var newPath = path[..^5];
        context.Request.Path = newPath;
    }
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAntiforgery();

app.MapRazorComponents<ContosoUniversity.Components.App>();

app.Run();


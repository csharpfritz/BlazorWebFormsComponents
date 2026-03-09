using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ContosoUniversityContext context)
    {
        // Check if already seeded
        if (await context.Students.AnyAsync())
            return;

        // Seed Departments
        var departments = new Department[]
        {
            new() { DepartmentName = "Computer Science", BuildingNumber = 1, ManagingInstructorID = 1 },
            new() { DepartmentName = "Mathematics", BuildingNumber = 2, ManagingInstructorID = 2 },
            new() { DepartmentName = "Engineering", BuildingNumber = 3, ManagingInstructorID = 3 }
        };
        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();

        // Seed Instructors
        var instructors = new Instructor[]
        {
            new() { FirstName = "John", LastName = "Smith", BirthDate = new DateTime(1975, 5, 15), Email = "jsmith@contoso.edu" },
            new() { FirstName = "Jane", LastName = "Doe", BirthDate = new DateTime(1980, 3, 20), Email = "jdoe@contoso.edu" },
            new() { FirstName = "Bob", LastName = "Wilson", BirthDate = new DateTime(1978, 9, 10), Email = "bwilson@contoso.edu" }
        };
        await context.Instructors.AddRangeAsync(instructors);
        await context.SaveChangesAsync();

        // Seed Courses
        var courses = new Course[]
        {
            new() { CourseName = "Introduction to Programming", StudentsMax = 30, DepartmentID = 1, InstructorID = 1 },
            new() { CourseName = "Data Structures", StudentsMax = 25, DepartmentID = 1, InstructorID = 1 },
            new() { CourseName = "Calculus I", StudentsMax = 35, DepartmentID = 2, InstructorID = 2 },
            new() { CourseName = "Linear Algebra", StudentsMax = 30, DepartmentID = 2, InstructorID = 2 },
            new() { CourseName = "Mechanics", StudentsMax = 20, DepartmentID = 3, InstructorID = 3 }
        };
        await context.Courses.AddRangeAsync(courses);
        await context.SaveChangesAsync();

        // Seed Students
        var students = new Student[]
        {
            new() { FirstName = "Alice", LastName = "Johnson", BirthDate = new DateTime(2000, 1, 15), Email = "alice@student.contoso.edu" },
            new() { FirstName = "Bob", LastName = "Williams", BirthDate = new DateTime(2001, 4, 22), Email = "bob@student.contoso.edu" },
            new() { FirstName = "Carol", LastName = "Brown", BirthDate = new DateTime(2000, 7, 8), Email = "carol@student.contoso.edu" },
            new() { FirstName = "David", LastName = "Jones", BirthDate = new DateTime(2001, 11, 30), Email = "david@student.contoso.edu" },
            new() { FirstName = "Eve", LastName = "Davis", BirthDate = new DateTime(2002, 2, 14), Email = "eve@student.contoso.edu" }
        };
        await context.Students.AddRangeAsync(students);
        await context.SaveChangesAsync();

        // Seed Enrollments
        var enrollments = new Enrollment[]
        {
            new() { StudentID = 1, CourseID = 1, Date = DateTime.Now.AddDays(-30) },
            new() { StudentID = 1, CourseID = 3, Date = DateTime.Now.AddDays(-30) },
            new() { StudentID = 2, CourseID = 1, Date = DateTime.Now.AddDays(-25) },
            new() { StudentID = 2, CourseID = 2, Date = DateTime.Now.AddDays(-25) },
            new() { StudentID = 3, CourseID = 3, Date = DateTime.Now.AddDays(-20) },
            new() { StudentID = 3, CourseID = 4, Date = DateTime.Now.AddDays(-20) },
            new() { StudentID = 4, CourseID = 5, Date = DateTime.Now.AddDays(-15) },
            new() { StudentID = 5, CourseID = 1, Date = DateTime.Now.AddDays(-10) },
            new() { StudentID = 5, CourseID = 2, Date = DateTime.Now.AddDays(-10) }
        };
        await context.Enrollments.AddRangeAsync(enrollments);
        await context.SaveChangesAsync();
    }
}

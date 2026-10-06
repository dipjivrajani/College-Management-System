using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Helpers;
using CollegeManagementSystem.Hubs;
using CollegeManagementSystem.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services for Razor Pages and EF Core SQLite
builder.Services.AddRazorPages(options =>
{
    // Protect entire folders with role-specific authorization policies
    options.Conventions.AuthorizeFolder("/Admin", "AdminPolicy");
    options.Conventions.AuthorizeFolder("/Teacher", "TeacherPolicy");
    options.Conventions.AuthorizeFolder("/Student", "StudentPolicy");
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=college.db"));

// 2. Add ASP.NET Core SignalR for real-time updates
builder.Services.AddSignalR();

// 3. Configure Separate ASP.NET Core Cookie Authentication Schemes
builder.Services.AddAuthentication()
    .AddCookie(AuthHelper.AdminScheme, options =>
    {
        options.Cookie.Name = AuthHelper.AdminCookie;
        options.LoginPath = "/login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    })
    .AddCookie(AuthHelper.TeacherScheme, options =>
    {
        options.Cookie.Name = AuthHelper.TeacherCookie;
        options.LoginPath = "/login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    })
    .AddCookie(AuthHelper.StudentScheme, options =>
    {
        options.Cookie.Name = AuthHelper.StudentCookie;
        options.LoginPath = "/login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// 4. Configure Authorization Policies (each evaluates strictly its own cookie scheme)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(AuthHelper.AdminScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });

    options.AddPolicy("TeacherPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(AuthHelper.TeacherScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Teacher");
    });

    options.AddPolicy("StudentPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(AuthHelper.StudentScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Student");
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.WebHost.UseStaticWebAssets();

var app = builder.Build();

// Ensure SQLite database & seed data are initialized
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    // Safely ensure new columns & tables exist in SQLite without dropping user data
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS AcademicYears (
            AcademicYearId INTEGER PRIMARY KEY AUTOINCREMENT,
            YearCode TEXT NOT NULL,
            YearName TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            DisplayOrder INTEGER NOT NULL DEFAULT 1
        );
        CREATE TABLE IF NOT EXISTS ResultAttempts (
            ResultAttemptId INTEGER PRIMARY KEY AUTOINCREMENT,
            StudentId TEXT NOT NULL,
            AcademicYear TEXT NOT NULL DEFAULT '2025-26',
            Semester TEXT NOT NULL,
            Attempt INTEGER NOT NULL DEFAULT 1,
            TotalObtainedMarks REAL NOT NULL DEFAULT 0,
            TotalMaxMarks REAL NOT NULL DEFAULT 0,
            Percentage REAL NOT NULL DEFAULT 0,
            SGPA REAL NULL,
            ResultStatus TEXT NOT NULL DEFAULT 'INCOMPLETE',
            ExamDate TEXT NULL,
            Remarks TEXT NULL
        );
    ");

    try { db.Database.ExecuteSqlRaw("ALTER TABLE Subjects ADD COLUMN AcademicYear TEXT DEFAULT '2025-26';"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Subjects ADD COLUMN DisplayOrder INTEGER DEFAULT 1;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE SubjectComponents ADD COLUMN PassingMarks REAL DEFAULT 12;"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Marks ADD COLUMN AcademicYear TEXT DEFAULT '2025-26';"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Marks ADD COLUMN Attempt INTEGER DEFAULT 1;"); } catch { }

    if (!db.AcademicYears.Any())
    {
        db.AcademicYears.AddRange(
            new AcademicYear { YearCode = "2024-25", YearName = "Academic Year 2024-2025", IsActive = true, DisplayOrder = 1 },
            new AcademicYear { YearCode = "2025-26", YearName = "Academic Year 2025-2026", IsActive = true, DisplayOrder = 2 },
            new AcademicYear { YearCode = "2026-27", YearName = "Academic Year 2026-2027", IsActive = true, DisplayOrder = 3 },
            new AcademicYear { YearCode = "2027-28", YearName = "Academic Year 2027-2028", IsActive = true, DisplayOrder = 4 }
        );
        db.SaveChanges();
    }

    // Seed or update Admin#123 account with hashed Deep#9425
    var admin = db.Admins.FirstOrDefault(a => a.Username == "Admin#123");
    if (admin == null)
    {
        var legacyAdmin = db.Admins.FirstOrDefault();
        if (legacyAdmin != null)
        {
            legacyAdmin.Username = "Admin#123";
            legacyAdmin.Password = CollegeManagementSystem.Models.PasswordHelper.HashPassword("Deep#9425");
            legacyAdmin.FullName = "System Administrator";
            legacyAdmin.Email = "admin@ssv.edu.in";
            legacyAdmin.Role = "Admin";
        }
        else
        {
            db.Admins.Add(new CollegeManagementSystem.Models.Admin
            {
                Username = "Admin#123",
                Password = CollegeManagementSystem.Models.PasswordHelper.HashPassword("Deep#9425"),
                FullName = "System Administrator",
                Email = "admin@ssv.edu.in",
                Role = "Admin"
            });
        }
        db.SaveChanges();
    }

    // Normalize student departments strictly from course
    foreach (var s in db.Students.ToList())
    {
        var correctDept = CollegeManagementSystem.Pages.Admin.Students.IndexModel.GetDepartment(s.Course);
        if (s.Department != correctDept)
        {
            s.Department = correctDept;
        }
    }
    db.SaveChanges();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapStaticAssets();

app.UseRouting();

// Authentication & Authorization Middlewares
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Map SignalR Hub
app.MapHub<CollegeManagementHub>("/hubs/collegeHub");

app.MapRazorPages();

app.Run();

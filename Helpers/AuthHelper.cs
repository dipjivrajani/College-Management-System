using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace CollegeManagementSystem.Helpers;

public static class AuthHelper
{
    public const string AdminScheme = "AdminAuth";
    public const string TeacherScheme = "TeacherAuth";
    public const string StudentScheme = "StudentAuth";

    public const string AdminCookie = "CollegeManagement.AdminAuth";
    public const string TeacherCookie = "CollegeManagement.TeacherAuth";
    public const string StudentCookie = "CollegeManagement.StudentAuth";

    public static async Task<(bool Succeeded, string UserId, string Name, string Role)> GetAdminUserAsync(HttpContext context)
    {
        var auth = await context.AuthenticateAsync(AdminScheme);
        if (auth.Succeeded && auth.Principal != null && auth.Principal.IsInRole("Admin"))
        {
            var id = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? auth.Principal.FindFirst("UserId")?.Value
                  ?? context.Session.GetString("AdminId")
                  ?? "Admin#123";
            var name = auth.Principal.Identity?.Name
                    ?? context.Session.GetString("AdminName")
                    ?? "System Administrator";
            return (true, id, name, "Admin");
        }
        return (false, "", "", "");
    }

    public static async Task<(bool Succeeded, string TeacherId, string Name, string Department)> GetTeacherUserAsync(HttpContext context)
    {
        var auth = await context.AuthenticateAsync(TeacherScheme);
        if (auth.Succeeded && auth.Principal != null && auth.Principal.IsInRole("Teacher"))
        {
            var id = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? auth.Principal.FindFirst("TeacherId")?.Value
                  ?? context.Session.GetString("TeacherId")
                  ?? "";
            var name = auth.Principal.Identity?.Name
                    ?? context.Session.GetString("TeacherName")
                    ?? "Faculty Member";
            var dept = auth.Principal.FindFirst("Department")?.Value ?? "";
            return (true, id, name, dept);
        }
        return (false, "", "", "");
    }

    public static async Task<(bool Succeeded, string StudentId, string Name, string Course, string Department)> GetStudentUserAsync(HttpContext context)
    {
        var auth = await context.AuthenticateAsync(StudentScheme);
        if (auth.Succeeded && auth.Principal != null && auth.Principal.IsInRole("Student"))
        {
            var id = auth.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? auth.Principal.FindFirst("StudentId")?.Value
                  ?? context.Session.GetString("StudentId")
                  ?? "";
            var name = auth.Principal.Identity?.Name
                    ?? context.Session.GetString("StudentName")
                    ?? "Student";
            var course = auth.Principal.FindFirst("Course")?.Value ?? "";
            var dept = auth.Principal.FindFirst("Department")?.Value ?? "";
            return (true, id, name, course, dept);
        }
        return (false, "", "", "", "");
    }
}

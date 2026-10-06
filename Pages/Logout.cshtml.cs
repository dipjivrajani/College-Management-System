using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CollegeManagementSystem.Helpers;

namespace CollegeManagementSystem.Pages;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync(string? role)
    {
        return await SignOutRoleAsync(role);
    }

    public async Task<IActionResult> OnPostAsync(string? role)
    {
        return await SignOutRoleAsync(role);
    }

    private async Task<IActionResult> SignOutRoleAsync(string? role)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync(AuthHelper.AdminScheme);
            HttpContext.Session.Remove("AdminId");
            HttpContext.Session.Remove("AdminName");
            return RedirectToPage("/Login", new { role = "Admin" });
        }
        else if (string.Equals(role, "Teacher", StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync(AuthHelper.TeacherScheme);
            HttpContext.Session.Remove("TeacherId");
            HttpContext.Session.Remove("TeacherName");
            return RedirectToPage("/Login", new { role = "Teacher" });
        }
        else if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync(AuthHelper.StudentScheme);
            HttpContext.Session.Remove("StudentId");
            HttpContext.Session.Remove("StudentName");
            return RedirectToPage("/Login", new { role = "Student" });
        }

        // Infer from referer header if not explicitly given
        var referer = Request.Headers.Referer.ToString();
        if (referer.Contains("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync(AuthHelper.AdminScheme);
            HttpContext.Session.Remove("AdminId");
            HttpContext.Session.Remove("AdminName");
            return RedirectToPage("/Login", new { role = "Admin" });
        }
        if (referer.Contains("/Teacher", StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync(AuthHelper.TeacherScheme);
            HttpContext.Session.Remove("TeacherId");
            HttpContext.Session.Remove("TeacherName");
            return RedirectToPage("/Login", new { role = "Teacher" });
        }
        if (referer.Contains("/Student", StringComparison.OrdinalIgnoreCase))
        {
            await HttpContext.SignOutAsync(AuthHelper.StudentScheme);
            HttpContext.Session.Remove("StudentId");
            HttpContext.Session.Remove("StudentName");
            return RedirectToPage("/Login", new { role = "Student" });
        }

        await HttpContext.SignOutAsync(AuthHelper.AdminScheme);
        await HttpContext.SignOutAsync(AuthHelper.TeacherScheme);
        await HttpContext.SignOutAsync(AuthHelper.StudentScheme);
        HttpContext.Session.Clear();
        return RedirectToPage("/Login");
    }
}

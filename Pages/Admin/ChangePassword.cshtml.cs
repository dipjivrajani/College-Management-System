using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Pages.Admin;

public class ChangePasswordModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ChangePasswordModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public AdminChangePasswordInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var adminUser = await CollegeManagementSystem.Helpers.AuthHelper.GetAdminUserAsync(HttpContext);
        var adminId = adminUser.Succeeded ? adminUser.UserId : "Admin#123";
        Input.AdminUsername = adminId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var adminUser = await CollegeManagementSystem.Helpers.AuthHelper.GetAdminUserAsync(HttpContext);
        var currentAdminUsername = adminUser.Succeeded ? adminUser.UserId : "Admin#123";

        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username.ToLower() == currentAdminUsername.ToLower());
        if (admin == null)
        {
            ErrorMessage = "Admin account not found.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Input.CurrentPassword))
        {
            ModelState.AddModelError("Input.CurrentPassword", "Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.NewPassword) || Input.NewPassword.Length < 6)
        {
            ModelState.AddModelError("Input.NewPassword", "New password must be at least 6 characters long.");
        }

        if (Input.NewPassword != Input.ConfirmPassword)
        {
            ModelState.AddModelError("Input.ConfirmPassword", "New password and confirmation password do not match.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Verify current password
        if (!PasswordHelper.VerifyPassword(Input.CurrentPassword, admin.Password))
        {
            ErrorMessage = "The current password entered is incorrect.";
            return Page();
        }

        // Securely hash the new password before storing in database
        admin.Password = PasswordHelper.HashPassword(Input.NewPassword);
        await _context.SaveChangesAsync();

        StatusMessage = "Admin password has been changed and securely hashed. Please use your new password for future logins.";
        return RedirectToPage("/Admin/ChangePassword");
    }
}

public class AdminChangePasswordInput
{
    public string AdminUsername { get; set; } = "Admin#123";
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

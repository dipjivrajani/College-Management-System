using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Pages.Admin.Registrations;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<CollegeManagementSystem.Models.Registration> Registrations { get; set; } = new();

    [BindProperty]
    public string RegistrationId { get; set; } = string.Empty;

    [BindProperty]
    public string NewStatus { get; set; } = string.Empty;

    public string? FeedbackMessage { get; set; }

    public async Task OnGetAsync()
    {
        // Mandatory ordering: Newest first
        Registrations = await _context.Registrations
            .OrderByDescending(r => r.RegistrationDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync()
    {
        if (!string.IsNullOrWhiteSpace(RegistrationId) && !string.IsNullOrWhiteSpace(NewStatus))
        {
            var reg = await _context.Registrations.FirstOrDefaultAsync(r => r.RegistrationId == RegistrationId);
            if (reg != null)
            {
                reg.Status = NewStatus.ToUpper();
                await _context.SaveChangesAsync();
                FeedbackMessage = $"Status for {reg.FullName} ({reg.RegistrationId}) updated to {reg.Status}.";
            }
        }

        Registrations = await _context.Registrations
            .OrderByDescending(r => r.RegistrationDate)
            .ToListAsync();

        return Page();
    }
}

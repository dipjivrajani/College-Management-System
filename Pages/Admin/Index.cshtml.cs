using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalRegistrations { get; set; }
    public int NewRegistrationsCount { get; set; }
    public int TotalInquiries { get; set; }
    public int NewInquiriesCount { get; set; }
    public int TotalCourses { get; set; }

    public List<CollegeManagementSystem.Models.Registration> RecentRegistrations { get; set; } = new();
    public List<CollegeManagementSystem.Models.Inquiry> RecentInquiries { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // Simple session check (accessible for demo/testing or when logged in)
        TotalRegistrations = await _context.Registrations.CountAsync();
        NewRegistrationsCount = await _context.Registrations.CountAsync(r => r.Status == "NEW");

        TotalInquiries = await _context.Inquiries.CountAsync();
        NewInquiriesCount = await _context.Inquiries.CountAsync(i => i.Status == "NEW");

        TotalCourses = await _context.Courses.CountAsync();

        // Sensible ordering: Newest first
        RecentRegistrations = await _context.Registrations
            .OrderByDescending(r => r.RegistrationDate)
            .Take(5)
            .ToListAsync();

        RecentInquiries = await _context.Inquiries
            .OrderByDescending(i => i.CreatedDate)
            .Take(5)
            .ToListAsync();

        return Page();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Pages.Admin.Inquiries;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Inquiry> Inquiries { get; set; } = new();

    [BindProperty]
    public int InquiryId { get; set; }

    [BindProperty]
    public string NewStatus { get; set; } = string.Empty;

    public string? FeedbackMessage { get; set; }

    public async Task OnGetAsync()
    {
        // Mandatory ordering: Newest first
        Inquiries = await _context.Inquiries
            .OrderByDescending(i => i.CreatedDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync()
    {
        if (InquiryId > 0 && !string.IsNullOrWhiteSpace(NewStatus))
        {
            var inq = await _context.Inquiries.FirstOrDefaultAsync(i => i.InquiryId == InquiryId);
            if (inq != null)
            {
                inq.Status = NewStatus.ToUpper();
                await _context.SaveChangesAsync();
                FeedbackMessage = $"Status for Inquiry #{inq.InquiryId} updated to {inq.Status}.";
            }
        }

        Inquiries = await _context.Inquiries
            .OrderByDescending(i => i.CreatedDate)
            .ToListAsync();

        return Page();
    }
}

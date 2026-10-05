using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Pages.Contact;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Inquiry InquiryForm { get; set; } = new();

    public bool IsSubmittedSuccess { get; set; }
    public int GeneratedInquiryId { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        InquiryForm.CreatedDate = DateTime.Now;
        InquiryForm.Status = "NEW";

        _context.Inquiries.Add(InquiryForm);
        await _context.SaveChangesAsync();

        GeneratedInquiryId = InquiryForm.InquiryId;
        IsSubmittedSuccess = true;

        return Page();
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Pages.Programs;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Course> Courses { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Mandatory alphabetical order A-Z
        Courses = await _context.Courses
            .Where(c => c.IsActive)
            .OrderBy(c => c.CourseName)
            .ToListAsync();
    }
}

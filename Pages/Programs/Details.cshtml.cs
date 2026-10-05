using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Pages.Programs;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Course? Course { get; set; }

    public List<Course> OtherCourses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? course)
    {
        if (string.IsNullOrWhiteSpace(course))
        {
            // Default to first course if none specified
            Course = await _context.Courses.OrderBy(c => c.CourseName).FirstOrDefaultAsync();
        }
        else
        {
            string search = course.Trim().ToLower();
            Course = await _context.Courses.FirstOrDefaultAsync(c =>
                c.CourseCode.ToLower() == search ||
                c.CourseName.ToLower().Contains(search));
        }

        if (Course == null)
        {
            Course = await _context.Courses.OrderBy(c => c.CourseName).FirstOrDefaultAsync();
        }

        if (Course != null)
        {
            OtherCourses = await _context.Courses
                .Where(c => c.CourseId != Course.CourseId && c.IsActive)
                .OrderBy(c => c.CourseName)
                .Take(4)
                .ToListAsync();
        }

        return Page();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

using CollegeManagementSystem.Helpers;

namespace CollegeManagementSystem.Pages.StudentPortal.Attendance;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public CollegeManagementSystem.Models.Student? CurrentStudent { get; set; }
    public List<CollegeManagementSystem.Models.Attendance> AttendanceRecords { get; set; } = new();

    public int TotalClasses { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AttendancePercentage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var (succeeded, studentId, studentName, course, dept) = await AuthHelper.GetStudentUserAsync(HttpContext);
        if (!succeeded)
        {
            return RedirectToPage("/Login", new { role = "Student" });
        }

        string targetStudentId = studentId;

        CurrentStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == targetStudentId);
        if (CurrentStudent == null)
        {
            return RedirectToPage("/Login", new { role = "Student" });
        }

        // Fetch attendance exclusively for this authenticated student
        AttendanceRecords = await _context.Attendances
            .Where(a => a.StudentId == targetStudentId)
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        TotalClasses = AttendanceRecords.Count;
        PresentCount = AttendanceRecords.Count(a => a.Status.Equals("Present", StringComparison.OrdinalIgnoreCase));
        AbsentCount = AttendanceRecords.Count(a => a.Status.Equals("Absent", StringComparison.OrdinalIgnoreCase));
        AttendancePercentage = TotalClasses > 0
            ? Math.Round((decimal)PresentCount / TotalClasses * 100, 1)
            : 0;

        return Page();
    }
}

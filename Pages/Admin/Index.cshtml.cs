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

    public int TotalStudents { get; set; }
    public int ActiveStudentsCount { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalCourses { get; set; }
    public int PendingRegistrationsCount { get; set; }
    public int PendingTeacherLeaveCount { get; set; } = 0; // Coming soon module

    public List<Student> RecentStudents { get; set; } = new();
    public List<CollegeManagementSystem.Models.Registration> RecentRegistrations { get; set; } = new();
    public List<Inquiry> RecentInquiries { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        TotalStudents = await _context.Students.CountAsync();
        ActiveStudentsCount = await _context.Students.CountAsync(s => s.Status.ToLower() == "active");

        TotalTeachers = await _context.Teachers.CountAsync();
        TotalCourses = await _context.Courses.CountAsync(c => c.IsActive);

        PendingRegistrationsCount = await _context.Registrations.CountAsync(r => r.Status == "NEW");

        // Load 5 recent students
        RecentStudents = await _context.Students
            .OrderByDescending(s => s.AdmissionDate)
            .ThenBy(s => s.FullName)
            .Take(5)
            .ToListAsync();

        // Load 5 recent registrations
        RecentRegistrations = await _context.Registrations
            .OrderByDescending(r => r.RegistrationDate)
            .Take(5)
            .ToListAsync();

        // Load 5 recent inquiries
        RecentInquiries = await _context.Inquiries
            .OrderByDescending(i => i.CreatedDate)
            .Take(5)
            .ToListAsync();

        return Page();
    }
}

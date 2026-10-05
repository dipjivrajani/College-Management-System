using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Pages.Registration;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public CollegeManagementSystem.Models.Registration RegForm { get; set; } = new();

    [BindProperty]
    public bool AgreeTerms { get; set; }

    public List<string> AvailableCourses { get; set; } = new();

    public bool IsSubmittedSuccess { get; set; }
    public string GeneratedRegistrationId { get; set; } = string.Empty;

    public async Task OnGetAsync(string? course)
    {
        await LoadCoursesAsync();

        if (!string.IsNullOrWhiteSpace(course))
        {
            string search = course.Trim().ToLower();
            var matchedCourse = AvailableCourses.FirstOrDefault(c => c.ToLower().Contains(search));
            if (matchedCourse != null)
            {
                RegForm.SelectedCourse = matchedCourse;
            }
            else
            {
                RegForm.SelectedCourse = course.Trim();
            }
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadCoursesAsync();

        if (!AgreeTerms)
        {
            ModelState.AddModelError("AgreeTerms", "You must agree to the academic terms and guidelines to submit registration.");
        }

        if (string.IsNullOrWhiteSpace(RegForm.SelectedCourse))
        {
            ModelState.AddModelError("RegForm.SelectedCourse", "Please select an undergraduate science program.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Generate clean Registration ID: e.g. REG2026-CHM-1042
        string yearPrefix = DateTime.Now.Year.ToString();
        string courseCode = "SSV";
        var matchedCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseName == RegForm.SelectedCourse);
        if (matchedCourse != null)
        {
            courseCode = matchedCourse.CourseCode;
        }

        int count = await _context.Registrations.CountAsync() + 1;
        RegForm.RegistrationId = $"REG{yearPrefix}-{courseCode}-{count:D4}";
        RegForm.RegistrationDate = DateTime.Now;
        RegForm.Status = "NEW";

        _context.Registrations.Add(RegForm);
        await _context.SaveChangesAsync();

        GeneratedRegistrationId = RegForm.RegistrationId;
        IsSubmittedSuccess = true;

        return Page();
    }

    private async Task LoadCoursesAsync()
    {
        // Alphabetical order A-Z
        AvailableCourses = await _context.Courses
            .Where(c => c.IsActive)
            .OrderBy(c => c.CourseName)
            .Select(c => c.CourseName)
            .ToListAsync();

        if (!AvailableCourses.Any())
        {
            AvailableCourses = new List<string>
            {
                "B.Sc. Botany",
                "B.Sc. Chemistry",
                "B.Sc. Computer Science",
                "B.Sc. Forensic Science",
                "B.Sc. Microbiology",
                "B.Sc. Physics",
                "B.Sc. Zoology"
            };
        }
    }
}

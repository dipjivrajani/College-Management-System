using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;
using CollegeManagementSystem.Helpers;
using CollegeManagementSystem.Hubs;

namespace CollegeManagementSystem.Pages.AdminPortal.SubjectsAdmin;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<CollegeManagementHub> _hubContext;

    public IndexModel(ApplicationDbContext context, IHubContext<CollegeManagementHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [BindProperty(SupportsGet = true)]
    public string SelectedCourse { get; set; } = "B.Sc. Botany";

    [BindProperty(SupportsGet = true)]
    public string? SelectedSemester { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public List<Subject> SubjectsList { get; set; } = new();
    public int TotalSubjectsCount { get; set; }
    public int ActiveSubjectsCount { get; set; }

    public static readonly List<string> CourseList = new()
    {
        "B.Sc. Botany",
        "B.Sc. Chemistry",
        "B.Sc. Computer Science",
        "B.Sc. Forensic Science",
        "B.Sc. Microbiology",
        "B.Sc. Physics",
        "B.Sc. Zoology"
    };

    public static readonly List<string> SemesterList = new()
    {
        "Semester 1",
        "Semester 2",
        "Semester 3",
        "Semester 4",
        "Semester 5",
        "Semester 6"
    };

    public static readonly Dictionary<string, string> CourseToDeptMap = new()
    {
        { "B.Sc. Botany", "Botany" },
        { "B.Sc. Chemistry", "Chemistry" },
        { "B.Sc. Computer Science", "Computer Science" },
        { "B.Sc. Forensic Science", "Forensic Science" },
        { "B.Sc. Microbiology", "Microbiology" },
        { "B.Sc. Physics", "Physics" },
        { "B.Sc. Zoology", "Zoology" }
    };

    public static readonly Dictionary<string, string> CourseToCodeMap = new()
    {
        { "B.Sc. Botany", "BOT" },
        { "B.Sc. Chemistry", "CHM" },
        { "B.Sc. Computer Science", "CSC" },
        { "B.Sc. Forensic Science", "FOR" },
        { "B.Sc. Microbiology", "MIC" },
        { "B.Sc. Physics", "PHY" },
        { "B.Sc. Zoology", "ZOO" }
    };

    public async Task<IActionResult> OnGetAsync()
    {
        var adminUser = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!adminUser.Succeeded)
        {
            return RedirectToPage("/Login", new { role = "Admin" });
        }

        if (string.IsNullOrEmpty(SelectedCourse) || !CourseList.Contains(SelectedCourse))
        {
            SelectedCourse = CourseList.First();
        }

        var query = _context.Subjects.AsQueryable();

        // Course filter
        query = query.Where(s => s.Course == SelectedCourse);

        // Semester filter
        if (!string.IsNullOrEmpty(SelectedSemester) && SelectedSemester != "All")
        {
            query = query.Where(s => s.Semester == SelectedSemester);
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();
            query = query.Where(s => s.SubjectName.ToLower().Contains(term) || s.SubjectCode.ToLower().Contains(term));
        }

        // Order A-Z by Subject Name as required
        SubjectsList = await query.OrderBy(s => s.SubjectName).ToListAsync();

        // Attach Subject Components
        var subjectIds = SubjectsList.Select(s => s.SubjectId).ToList();
        var allComponents = await _context.SubjectComponents
            .Where(c => subjectIds.Contains(c.SubjectId))
            .ToListAsync();

        foreach (var sub in SubjectsList)
        {
            sub.Components = allComponents.Where(c => c.SubjectId == sub.SubjectId).ToList();
        }

        TotalSubjectsCount = SubjectsList.Count;
        ActiveSubjectsCount = SubjectsList.Count(s => s.Status == "Active");

        return Page();
    }

    public async Task<IActionResult> OnPostAddSubjectAsync(
        string subjectName,
        string subjectCode,
        string course,
        string semester,
        decimal credits,
        bool hasInternal,
        decimal internalMax,
        bool hasTheory,
        decimal theoryMax,
        bool hasPractical,
        decimal practicalMax,
        bool hasProject,
        decimal projectMax,
        bool hasViva,
        decimal vivaMax)
    {
        var adminUser = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!adminUser.Succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            ErrorMessage = "Subject Name is required.";
            return RedirectToPage(new { course = course, semester = semester });
        }

        if (string.IsNullOrWhiteSpace(subjectCode))
        {
            // Auto-generate code if empty
            var courseCode = CourseToCodeMap.GetValueOrDefault(course, "SCI");
            var semNum = semester.Replace("Semester ", "").Trim();
            var existingCount = await _context.Subjects.CountAsync(s => s.Course == course && s.Semester == semester);
            subjectCode = $"{courseCode}{semNum}0{existingCount + 1}";
        }

        if (credits <= 0) credits = 4.0m;

        // Internal marks validation: strictly maximum 30
        if (hasInternal && (internalMax <= 0 || internalMax > 30))
        {
            ErrorMessage = "Internal Assessment Maximum Marks must be between 1 and 30.";
            return RedirectToPage(new { course = course, semester = semester });
        }

        var dept = CourseToDeptMap.GetValueOrDefault(course, course.Replace("B.Sc. ", ""));
        var courseEntity = await _context.Courses.FirstOrDefaultAsync(c => c.CourseName == course);

        var newSubject = new Subject
        {
            SubjectName = subjectName.Trim(),
            SubjectCode = subjectCode.Trim().ToUpper(),
            Course = course,
            CourseId = courseEntity?.CourseId,
            Department = dept,
            Semester = semester,
            Credits = credits,
            Status = "Active"
        };

        _context.Subjects.Add(newSubject);
        await _context.SaveChangesAsync();

        // Add Configured Components
        var components = new List<SubjectComponent>();

        if (hasInternal)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Internal",
                MaximumMarks = internalMax > 0 ? Math.Min(internalMax, 30m) : 30m,
                IsRequired = true
            });
        }

        if (hasTheory)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Theory",
                MaximumMarks = theoryMax > 0 ? theoryMax : 70m,
                IsRequired = true
            });
        }

        if (hasPractical)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Practical",
                MaximumMarks = practicalMax > 0 ? practicalMax : 50m,
                IsRequired = true
            });
        }

        if (hasProject)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Project",
                MaximumMarks = projectMax > 0 ? projectMax : 100m,
                IsRequired = true
            });
        }

        if (hasViva)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Viva",
                MaximumMarks = vivaMax > 0 ? vivaMax : 50m,
                IsRequired = true
            });
        }

        // If no component selected by mistake, add default Internal (30) & Theory (70)
        if (components.Count == 0)
        {
            components.Add(new SubjectComponent { SubjectId = newSubject.SubjectId, ComponentType = "Internal", MaximumMarks = 30m, IsRequired = true });
            components.Add(new SubjectComponent { SubjectId = newSubject.SubjectId, ComponentType = "Theory", MaximumMarks = 70m, IsRequired = true });
        }

        _context.SubjectComponents.AddRange(components);
        await _context.SaveChangesAsync();

        SuccessMessage = $"Subject '{newSubject.SubjectName}' ({newSubject.SubjectCode}) added successfully with {components.Count} marks components.";
        return RedirectToPage(new { course = course, semester = semester });
    }

    public async Task<IActionResult> OnPostEditSubjectAsync(
        int subjectId,
        string subjectName,
        string subjectCode,
        string semester,
        decimal credits,
        string status)
    {
        var adminUser = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!adminUser.Succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null)
        {
            ErrorMessage = "Subject not found.";
            return RedirectToPage(new { course = SelectedCourse, semester = SelectedSemester });
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            ErrorMessage = "Subject Name cannot be empty.";
            return RedirectToPage(new { course = subject.Course, semester = subject.Semester });
        }

        subject.SubjectName = subjectName.Trim();
        if (!string.IsNullOrWhiteSpace(subjectCode))
        {
            subject.SubjectCode = subjectCode.Trim().ToUpper();
        }
        subject.Semester = semester;
        subject.Credits = credits > 0 ? credits : 4.0m;
        subject.Status = status == "Inactive" ? "Inactive" : "Active";

        await _context.SaveChangesAsync();

        SuccessMessage = $"Subject '{subject.SubjectName}' updated successfully.";
        return RedirectToPage(new { course = subject.Course, semester = subject.Semester });
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int subjectId)
    {
        var adminUser = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!adminUser.Succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null)
        {
            ErrorMessage = "Subject not found.";
            return RedirectToPage(new { course = SelectedCourse, semester = SelectedSemester });
        }

        subject.Status = (subject.Status == "Active") ? "Inactive" : "Active";
        await _context.SaveChangesAsync();

        SuccessMessage = $"Subject '{subject.SubjectName}' status set to {subject.Status}.";
        return RedirectToPage(new { course = subject.Course, semester = subject.Semester });
    }

    public async Task<IActionResult> OnPostConfigureComponentsAsync(
        int subjectId,
        bool hasInternal,
        decimal internalMax,
        bool hasTheory,
        decimal theoryMax,
        bool hasPractical,
        decimal practicalMax,
        bool hasProject,
        decimal projectMax,
        bool hasViva,
        decimal vivaMax)
    {
        var adminUser = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!adminUser.Succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null)
        {
            ErrorMessage = "Subject not found.";
            return RedirectToPage(new { course = SelectedCourse, semester = SelectedSemester });
        }

        // Validate Internal max <= 30
        if (hasInternal && (internalMax <= 0 || internalMax > 30))
        {
            ErrorMessage = "Internal assessment must have a maximum of 30 marks (range: 1 - 30).";
            return RedirectToPage(new { course = subject.Course, semester = subject.Semester });
        }

        // Remove existing components
        var existing = await _context.SubjectComponents.Where(c => c.SubjectId == subjectId).ToListAsync();
        _context.SubjectComponents.RemoveRange(existing);

        var components = new List<SubjectComponent>();

        if (hasInternal)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Internal",
                MaximumMarks = internalMax > 0 ? Math.Min(internalMax, 30m) : 30m,
                IsRequired = true
            });
        }

        if (hasTheory)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Theory",
                MaximumMarks = theoryMax > 0 ? theoryMax : 70m,
                IsRequired = true
            });
        }

        if (hasPractical)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Practical",
                MaximumMarks = practicalMax > 0 ? practicalMax : 50m,
                IsRequired = true
            });
        }

        if (hasProject)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Project",
                MaximumMarks = projectMax > 0 ? projectMax : 100m,
                IsRequired = true
            });
        }

        if (hasViva)
        {
            components.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Viva",
                MaximumMarks = vivaMax > 0 ? vivaMax : 50m,
                IsRequired = true
            });
        }

        if (components.Count == 0)
        {
            components.Add(new SubjectComponent { SubjectId = subjectId, ComponentType = "Internal", MaximumMarks = 30m, IsRequired = true });
        }

        _context.SubjectComponents.AddRange(components);
        await _context.SaveChangesAsync();

        SuccessMessage = $"Marks components configured successfully for '{subject.SubjectName}'.";
        return RedirectToPage(new { course = subject.Course, semester = subject.Semester });
    }
}

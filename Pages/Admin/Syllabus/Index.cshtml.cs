using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;
using CollegeManagementSystem.Helpers;
using CollegeManagementSystem.Hubs;

namespace CollegeManagementSystem.Pages.AdminPortal.SyllabusAdmin;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<CollegeManagementHub> _hubContext;

    public IndexModel(ApplicationDbContext context, IHubContext<CollegeManagementHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // Top Filter Bindings
    [BindProperty(SupportsGet = true)]
    public string SelectedAcademicYear { get; set; } = "2025-26";

    [BindProperty(SupportsGet = true)]
    public string SelectedCourse { get; set; } = "B.Sc. Botany";

    [BindProperty(SupportsGet = true)]
    public string SelectedSemester { get; set; } = "Semester 1";

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    // Page Data
    public List<AcademicYear> AcademicYearsList { get; set; } = new();
    public List<Subject> SubjectsList { get; set; } = new();
    public int TotalSubjectsCount { get; set; }
    public decimal TotalSemesterCredits { get; set; }
    public decimal TotalSemesterMaxMarks { get; set; }

    // Standard Course List (A-Z strictly sorted)
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

    public async Task<IActionResult> OnGetAsync(string? academicYear = null, string? course = null, string? semester = null)
    {
        if (!string.IsNullOrEmpty(academicYear)) SelectedAcademicYear = academicYear;
        if (!string.IsNullOrEmpty(course)) SelectedCourse = course;
        if (!string.IsNullOrEmpty(semester)) SelectedSemester = semester;

        var (succeeded, _, _, _) = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!succeeded)
        {
            return RedirectToPage("/Login", new { role = "Admin" });
        }

        // 1. Fetch available Academic Years
        AcademicYearsList = await _context.AcademicYears.OrderBy(y => y.DisplayOrder).ToListAsync();
        if (AcademicYearsList.Count == 0)
        {
            AcademicYearsList = new List<AcademicYear>
            {
                new() { YearCode = "2024-25", YearName = "Academic Year 2024-2025" },
                new() { YearCode = "2025-26", YearName = "Academic Year 2025-2026" },
                new() { YearCode = "2026-27", YearName = "Academic Year 2026-2027" },
                new() { YearCode = "2027-28", YearName = "Academic Year 2027-2028" }
            };
        }

        if (string.IsNullOrEmpty(SelectedAcademicYear) || !AcademicYearsList.Any(y => y.YearCode == SelectedAcademicYear))
        {
            SelectedAcademicYear = AcademicYearsList.FirstOrDefault(y => y.IsActive)?.YearCode ?? "2025-26";
        }

        if (string.IsNullOrEmpty(SelectedCourse) || !CourseList.Contains(SelectedCourse))
        {
            SelectedCourse = CourseList.First();
        }

        if (string.IsNullOrEmpty(SelectedSemester) || !SemesterList.Contains(SelectedSemester))
        {
            SelectedSemester = SemesterList.First();
        }

        // 2. Query Subjects for this Academic Year + Course + Semester
        var query = _context.Subjects
            .Where(s => s.Course == SelectedCourse && s.Semester == SelectedSemester);

        // Academic year versioning match: if subject has AcademicYear column populated, filter by it
        query = query.Where(s => s.AcademicYear == SelectedAcademicYear || string.IsNullOrEmpty(s.AcademicYear));

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();
            query = query.Where(s => s.SubjectName.ToLower().Contains(term) || s.SubjectCode.ToLower().Contains(term));
        }

        // Order by DisplayOrder then SubjectName (PART 13 & 33)
        SubjectsList = await query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.SubjectName).ToListAsync();

        // 3. Attach Subject Components
        var subjectIds = SubjectsList.Select(s => s.SubjectId).ToList();
        var allComponents = await _context.SubjectComponents
            .Where(c => subjectIds.Contains(c.SubjectId))
            .ToListAsync();

        foreach (var sub in SubjectsList)
        {
            sub.Components = allComponents.Where(c => c.SubjectId == sub.SubjectId).ToList();
        }

        TotalSubjectsCount = SubjectsList.Count;
        TotalSemesterCredits = SubjectsList.Where(s => s.Status == "Active").Sum(s => s.Credits);
        TotalSemesterMaxMarks = SubjectsList.Where(s => s.Status == "Active").Sum(s => s.Components.Sum(c => c.MaximumMarks));

        return Page();
    }

    // 1. ADD NEW SUBJECT TO SYLLABUS
    public async Task<IActionResult> OnPostAddSubjectAsync(
        string academicYear,
        string course,
        string semester,
        string subjectName,
        string subjectCode,
        decimal credits,
        int displayOrder,
        bool hasInternal,
        decimal internalMax,
        decimal internalPass,
        bool hasTheory,
        decimal theoryMax,
        decimal theoryPass,
        bool hasPractical,
        decimal practicalMax,
        decimal practicalPass,
        bool hasProject,
        decimal projectMax,
        decimal projectPass,
        bool hasViva,
        decimal vivaMax,
        decimal vivaPass)
    {
        var (succeeded, _, _, _) = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            ErrorMessage = "Subject Name is required.";
            return RedirectToPage(new { academicYear, course, semester });
        }

        if (credits <= 0) credits = 4.0m;
        if (displayOrder <= 0)
        {
            var maxOrder = await _context.Subjects
                .Where(s => s.AcademicYear == academicYear && s.Course == course && s.Semester == semester)
                .Select(s => (int?)s.DisplayOrder)
                .MaxAsync() ?? 0;
            displayOrder = maxOrder + 1;
        }

        if (string.IsNullOrWhiteSpace(subjectCode))
        {
            var dept = CourseToDeptMap.ContainsKey(course) ? CourseToDeptMap[course] : "SCI";
            var prefix = dept.Length >= 3 ? dept.Substring(0, 3).ToUpper() : dept.ToUpper();
            var semNum = semester.Replace("Semester", "").Trim();
            subjectCode = $"{prefix}{semNum}{displayOrder:D2}";
        }

        // Prevent duplicate code in same syllabus
        var duplicate = await _context.Subjects.AnyAsync(s => 
            s.AcademicYear == academicYear && 
            s.Course == course && 
            s.Semester == semester && 
            s.SubjectCode.ToLower() == subjectCode.Trim().ToLower());

        if (duplicate)
        {
            ErrorMessage = $"A subject with code '{subjectCode}' already exists in {academicYear} {course} {semester}.";
            return RedirectToPage(new { academicYear, course, semester });
        }

        string department = CourseToDeptMap.ContainsKey(course) ? CourseToDeptMap[course] : course;

        var newSubject = new Subject
        {
            AcademicYear = academicYear,
            Course = course,
            Department = department,
            Semester = semester,
            SubjectCode = subjectCode.Trim().ToUpper(),
            SubjectName = subjectName.Trim(),
            Credits = credits,
            DisplayOrder = displayOrder,
            Status = "Active"
        };

        _context.Subjects.Add(newSubject);
        await _context.SaveChangesAsync();

        // Add Configured Components
        var componentsToAdd = new List<SubjectComponent>();

        if (hasInternal)
        {
            componentsToAdd.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Internal",
                MaximumMarks = internalMax > 0 ? internalMax : 30,
                PassingMarks = internalPass > 0 ? internalPass : (internalMax > 0 ? Math.Round(internalMax * 0.4m, 1) : 12),
                IsRequired = true
            });
        }

        if (hasTheory)
        {
            componentsToAdd.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Theory",
                MaximumMarks = theoryMax > 0 ? theoryMax : 70,
                PassingMarks = theoryPass > 0 ? theoryPass : (theoryMax > 0 ? Math.Round(theoryMax * 0.4m, 1) : 28),
                IsRequired = true
            });
        }

        if (hasPractical)
        {
            componentsToAdd.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Practical",
                MaximumMarks = practicalMax > 0 ? practicalMax : 50,
                PassingMarks = practicalPass > 0 ? practicalPass : (practicalMax > 0 ? Math.Round(practicalMax * 0.4m, 1) : 20),
                IsRequired = true
            });
        }

        if (hasProject)
        {
            componentsToAdd.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Project",
                MaximumMarks = projectMax > 0 ? projectMax : 100,
                PassingMarks = projectPass > 0 ? projectPass : (projectMax > 0 ? Math.Round(projectMax * 0.4m, 1) : 40),
                IsRequired = true
            });
        }

        if (hasViva)
        {
            componentsToAdd.Add(new SubjectComponent
            {
                SubjectId = newSubject.SubjectId,
                ComponentType = "Viva",
                MaximumMarks = vivaMax > 0 ? vivaMax : 20,
                PassingMarks = vivaPass > 0 ? vivaPass : (vivaMax > 0 ? Math.Round(vivaMax * 0.4m, 1) : 8),
                IsRequired = true
            });
        }

        if (componentsToAdd.Count > 0)
        {
            _context.SubjectComponents.AddRange(componentsToAdd);
            await _context.SaveChangesAsync();
        }

        try
        {
            await _hubContext.Clients.All.SendAsync("CourseUpdated", new { academicYear, course, semester });
        }
        catch { }

        SuccessMessage = $"Subject '{newSubject.SubjectName}' ({newSubject.SubjectCode}) added successfully to {academicYear} {course} {semester}.";
        return RedirectToPage(new { academicYear, course, semester });
    }

    // 2. EDIT SUBJECT DETAILS
    public async Task<IActionResult> OnPostEditSubjectAsync(
        int subjectId,
        string academicYear,
        string course,
        string semester,
        string subjectName,
        string subjectCode,
        decimal credits,
        int displayOrder,
        string status)
    {
        var (succeeded, _, _, _) = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null)
        {
            ErrorMessage = "Subject not found.";
            return RedirectToPage(new { academicYear, course, semester });
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            ErrorMessage = "Subject Name cannot be empty.";
            return RedirectToPage(new { academicYear, course, semester });
        }

        subject.SubjectName = subjectName.Trim();
        if (!string.IsNullOrWhiteSpace(subjectCode)) subject.SubjectCode = subjectCode.Trim().ToUpper();
        subject.Credits = credits > 0 ? credits : 4.0m;
        subject.DisplayOrder = displayOrder > 0 ? displayOrder : subject.DisplayOrder;
        subject.Status = status == "Inactive" ? "Inactive" : (status == "Archived" ? "Archived" : "Active");

        await _context.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("CourseUpdated", new { academicYear = subject.AcademicYear, course = subject.Course, semester = subject.Semester });
        }
        catch { }

        SuccessMessage = $"Subject '{subject.SubjectName}' updated successfully.";
        return RedirectToPage(new { academicYear, course, semester });
    }

    // 3. CONFIGURE EVALUATION COMPONENTS
    public async Task<IActionResult> OnPostConfigureComponentsAsync(
        int subjectId,
        string academicYear,
        string course,
        string semester,
        bool hasInternal,
        decimal internalMax,
        decimal internalPass,
        bool hasTheory,
        decimal theoryMax,
        decimal theoryPass,
        bool hasPractical,
        decimal practicalMax,
        decimal practicalPass,
        bool hasProject,
        decimal projectMax,
        decimal projectPass,
        bool hasViva,
        decimal vivaMax,
        decimal vivaPass)
    {
        var (succeeded, _, _, _) = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null)
        {
            ErrorMessage = "Subject not found.";
            return RedirectToPage(new { academicYear, course, semester });
        }

        // Remove old components and replace with newly configured components
        var oldComponents = await _context.SubjectComponents.Where(c => c.SubjectId == subjectId).ToListAsync();
        _context.SubjectComponents.RemoveRange(oldComponents);

        var newComponents = new List<SubjectComponent>();

        if (hasInternal)
        {
            newComponents.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Internal",
                MaximumMarks = internalMax > 0 ? internalMax : 30,
                PassingMarks = internalPass > 0 ? internalPass : (internalMax > 0 ? Math.Round(internalMax * 0.4m, 1) : 12),
                IsRequired = true
            });
        }

        if (hasTheory)
        {
            newComponents.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Theory",
                MaximumMarks = theoryMax > 0 ? theoryMax : 70,
                PassingMarks = theoryPass > 0 ? theoryPass : (theoryMax > 0 ? Math.Round(theoryMax * 0.4m, 1) : 28),
                IsRequired = true
            });
        }

        if (hasPractical)
        {
            newComponents.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Practical",
                MaximumMarks = practicalMax > 0 ? practicalMax : 50,
                PassingMarks = practicalPass > 0 ? practicalPass : (practicalMax > 0 ? Math.Round(practicalMax * 0.4m, 1) : 20),
                IsRequired = true
            });
        }

        if (hasProject)
        {
            newComponents.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Project",
                MaximumMarks = projectMax > 0 ? projectMax : 100,
                PassingMarks = projectPass > 0 ? projectPass : (projectMax > 0 ? Math.Round(projectMax * 0.4m, 1) : 40),
                IsRequired = true
            });
        }

        if (hasViva)
        {
            newComponents.Add(new SubjectComponent
            {
                SubjectId = subjectId,
                ComponentType = "Viva",
                MaximumMarks = vivaMax > 0 ? vivaMax : 20,
                PassingMarks = vivaPass > 0 ? vivaPass : (vivaMax > 0 ? Math.Round(vivaMax * 0.4m, 1) : 8),
                IsRequired = true
            });
        }

        if (newComponents.Count > 0)
        {
            _context.SubjectComponents.AddRange(newComponents);
        }

        await _context.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("CourseUpdated", new { academicYear = subject.AcademicYear, course = subject.Course, semester = subject.Semester });
        }
        catch { }

        SuccessMessage = $"Components for '{subject.SubjectName}' updated ({newComponents.Count} active components).";
        return RedirectToPage(new { academicYear, course, semester });
    }

    // 4. TOGGLE STATUS (ACTIVE / INACTIVE)
    public async Task<IActionResult> OnPostToggleStatusAsync(int subjectId, string academicYear, string course, string semester)
    {
        var (succeeded, _, _, _) = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject != null)
        {
            subject.Status = (subject.Status == "Active") ? "Inactive" : "Active";
            await _context.SaveChangesAsync();
            SuccessMessage = $"Subject '{subject.SubjectName}' status changed to {subject.Status}.";
        }

        return RedirectToPage(new { academicYear, course, semester });
    }

    // 5. DELETE SUBJECT (SAFE SOFT/HARD DELETE - PART 32)
    public async Task<IActionResult> OnPostDeleteSubjectAsync(int subjectId, string academicYear, string course, string semester)
    {
        var (succeeded, _, _, _) = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        var subject = await _context.Subjects.FindAsync(subjectId);
        if (subject == null)
        {
            ErrorMessage = "Subject not found.";
            return RedirectToPage(new { academicYear, course, semester });
        }

        // Check if historical marks exist for this subject (PART 32!)
        bool hasMarks = await _context.Marks.AnyAsync(m => m.SubjectId == subjectId || m.SubjectName == subject.SubjectName);

        if (hasMarks)
        {
            // Do not delete historical academic records - archive instead
            subject.Status = "Archived";
            await _context.SaveChangesAsync();
            SuccessMessage = $"Subject '{subject.SubjectName}' has historical student marks and was safely archived rather than deleted, preserving historical records.";
        }
        else
        {
            var components = await _context.SubjectComponents.Where(c => c.SubjectId == subjectId).ToListAsync();
            _context.SubjectComponents.RemoveRange(components);
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            SuccessMessage = $"Subject '{subject.SubjectName}' ({subject.SubjectCode}) deleted successfully.";
        }

        try
        {
            await _hubContext.Clients.All.SendAsync("CourseUpdated", new { academicYear, course, semester });
        }
        catch { }

        return RedirectToPage(new { academicYear, course, semester });
    }

    // 6. CLONE / VERSION SYLLABUS TO NEW ACADEMIC YEAR (PART 2, 3 & 28!)
    public async Task<IActionResult> OnPostCloneSyllabusAsync(
        string sourceYear,
        string targetYear,
        string course,
        string semester)
    {
        var (succeeded, _, _, _) = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        if (sourceYear == targetYear)
        {
            ErrorMessage = "Source and Target Academic Years must be different.";
            return RedirectToPage(new { academicYear = sourceYear, course, semester });
        }

        // Ensure target academic year exists
        var targetAy = await _context.AcademicYears.FirstOrDefaultAsync(y => y.YearCode == targetYear);
        if (targetAy == null)
        {
            _context.AcademicYears.Add(new AcademicYear { YearCode = targetYear, YearName = $"Academic Year {targetYear}", IsActive = true });
            await _context.SaveChangesAsync();
        }

        // Check if target year already has subjects for this course & semester
        var existingInTarget = await _context.Subjects
            .Where(s => s.AcademicYear == targetYear && s.Course == course && s.Semester == semester)
            .ToListAsync();

        if (existingInTarget.Count > 0)
        {
            ErrorMessage = $"{targetYear} already has {existingInTarget.Count} subjects configured for {course} {semester}. Cannot overwrite.";
            return RedirectToPage(new { academicYear = targetYear, course, semester });
        }

        // Fetch source subjects and their components
        var sourceSubjects = await _context.Subjects
            .Where(s => (s.AcademicYear == sourceYear || string.IsNullOrEmpty(s.AcademicYear)) && s.Course == course && s.Semester == semester)
            .ToListAsync();

        if (sourceSubjects.Count == 0)
        {
            ErrorMessage = $"No subjects found in source syllabus {sourceYear} {course} {semester} to copy.";
            return RedirectToPage(new { academicYear = sourceYear, course, semester });
        }

        var sourceSubjectIds = sourceSubjects.Select(s => s.SubjectId).ToList();
        var sourceComponents = await _context.SubjectComponents
            .Where(c => sourceSubjectIds.Contains(c.SubjectId))
            .ToListAsync();

        int clonedCount = 0;
        foreach (var srcSub in sourceSubjects)
        {
            var newSub = new Subject
            {
                AcademicYear = targetYear,
                Course = srcSub.Course,
                Department = srcSub.Department,
                Semester = srcSub.Semester,
                SubjectCode = srcSub.SubjectCode,
                SubjectName = srcSub.SubjectName,
                Credits = srcSub.Credits,
                DisplayOrder = srcSub.DisplayOrder,
                Status = srcSub.Status
            };

            _context.Subjects.Add(newSub);
            await _context.SaveChangesAsync(); // Save to get newSub.SubjectId

            var subComps = sourceComponents.Where(c => c.SubjectId == srcSub.SubjectId).ToList();
            foreach (var comp in subComps)
            {
                _context.SubjectComponents.Add(new SubjectComponent
                {
                    SubjectId = newSub.SubjectId,
                    ComponentType = comp.ComponentType,
                    MaximumMarks = comp.MaximumMarks,
                    PassingMarks = comp.PassingMarks,
                    IsRequired = comp.IsRequired
                });
            }
            clonedCount++;
        }

        await _context.SaveChangesAsync();

        SuccessMessage = $"Successfully cloned {clonedCount} subjects from {sourceYear} to {targetYear} for {course} {semester}! The {sourceYear} syllabus remains completely untouched.";
        return RedirectToPage(new { academicYear = targetYear, course, semester });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;
using CollegeManagementSystem.Services;
using CollegeManagementSystem.Helpers;
using CollegeManagementSystem.Hubs;
using MarksEntity = CollegeManagementSystem.Models.Marks;

namespace CollegeManagementSystem.Pages.TeacherPortal.MarksTeacher;

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
    public int? SelectedAdmissionYear { get; set; } = 2025;

    [BindProperty(SupportsGet = true)]
    public string SelectedCourse { get; set; } = "B.Sc. Botany";

    [BindProperty(SupportsGet = true)]
    public string SelectedSemester { get; set; } = "Semester 3";

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedStudentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ActiveTabSemester { get; set; } = "Semester 3";

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public Teacher? CurrentTeacher { get; set; }
    public List<Student> StudentsList { get; set; } = new();
    public Student? SelectedStudent { get; set; }
    public List<Subject> SemesterSubjects { get; set; } = new();
    public List<SubjectComponent> SubjectComponents { get; set; } = new();
    public List<MarksEntity> StudentSemesterMarks { get; set; } = new();

    public SemesterResultDto? CurrentSemesterResult { get; set; }
    public OverallResultDto? OverallAcademicResult { get; set; }

    public static readonly List<int> AdmissionYears = new() { 2024, 2025, 2026, 2027 };
    public static readonly List<string> SemesterList = new()
    {
        "Semester 1", "Semester 2", "Semester 3", "Semester 4", "Semester 5", "Semester 6"
    };

    public List<string> AllowedCourses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var (succeeded, teacherId, teacherName, dept) = await AuthHelper.GetTeacherUserAsync(HttpContext);
        if (!succeeded)
        {
            return RedirectToPage("/Login", new { role = "Teacher" });
        }

        CurrentTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == teacherId);

        var allCourses = await _context.Courses.OrderBy(c => c.CourseName).Select(c => c.CourseName).ToListAsync();
        AllowedCourses = allCourses;

        // If course not set, default to Teacher's department course
        if (string.IsNullOrEmpty(SelectedCourse))
        {
            if (CurrentTeacher != null && !string.IsNullOrEmpty(CurrentTeacher.Department))
            {
                var match = allCourses.FirstOrDefault(c => c.ToLower().Contains(CurrentTeacher.Department.ToLower()));
                SelectedCourse = match ?? allCourses.FirstOrDefault() ?? "B.Sc. Botany";
            }
            else
            {
                SelectedCourse = allCourses.FirstOrDefault() ?? "B.Sc. Botany";
            }
        }

        // Detailed student editor mode
        if (!string.IsNullOrEmpty(SelectedStudentId))
        {
            SelectedStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == SelectedStudentId);
            if (SelectedStudent != null)
            {
                if (string.IsNullOrEmpty(ActiveTabSemester))
                {
                    ActiveTabSemester = SelectedStudent.Semester;
                }

                var allCourseSubjects = await _context.Subjects
                    .Where(s => s.Course == SelectedStudent.Course && s.Status == "Active")
                    .OrderBy(s => s.SubjectName)
                    .ToListAsync();

                SemesterSubjects = allCourseSubjects
                    .Where(s => s.Semester == ActiveTabSemester)
                    .ToList();

                var subjectIds = allCourseSubjects.Select(s => s.SubjectId).ToList();
                SubjectComponents = await _context.SubjectComponents
                    .Where(c => subjectIds.Contains(c.SubjectId))
                    .ToListAsync();

                foreach (var s in SemesterSubjects)
                {
                    s.Components = SubjectComponents.Where(c => c.SubjectId == s.SubjectId).ToList();
                }

                var allStudentMarks = await _context.Marks
                    .Where(m => m.StudentId == SelectedStudent.StudentId)
                    .ToListAsync();

                StudentSemesterMarks = allStudentMarks
                    .Where(m => m.Semester == ActiveTabSemester)
                    .ToList();

                CurrentSemesterResult = ResultCalculationService.CalculateSemesterResult(
                    ActiveTabSemester,
                    SemesterSubjects,
                    SubjectComponents,
                    StudentSemesterMarks);

                OverallAcademicResult = ResultCalculationService.CalculateOverallResult(
                    SelectedStudent,
                    allCourseSubjects,
                    SubjectComponents,
                    allStudentMarks);

                return Page();
            }
        }

        // Filter students from database
        var query = _context.Students.AsQueryable();

        if (!string.IsNullOrEmpty(SelectedCourse))
        {
            query = query.Where(s => s.Course == SelectedCourse);
        }

        if (SelectedAdmissionYear.HasValue && SelectedAdmissionYear.Value > 0)
        {
            query = query.Where(s => s.AdmissionYear == SelectedAdmissionYear.Value);
        }

        if (!string.IsNullOrEmpty(SelectedSemester) && SelectedSemester != "All")
        {
            query = query.Where(s => s.Semester == SelectedSemester);
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();
            query = query.Where(s => s.FullName.ToLower().Contains(term) || s.StudentId.ToLower().Contains(term));
        }

        StudentsList = await query.OrderBy(s => s.FullName).ToListAsync();
        return Page();
    }

    // Save Marks as Teacher (Validation: Internal max 30)
    public async Task<IActionResult> OnPostSaveStudentMarksAsync(
        string studentId,
        string semester,
        List<int> subjectIds,
        List<string> componentTypes,
        List<decimal> maxMarksList,
        List<decimal> obtainedMarksList,
        List<string> examDatesList)
    {
        var (succeeded, _, _, _) = await AuthHelper.GetTeacherUserAsync(HttpContext);
        if (!succeeded)
        {
            return RedirectToPage("/Login", new { role = "Teacher" });
        }

        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (student == null)
        {
            ErrorMessage = "Student not found.";
            return RedirectToPage();
        }

        if (subjectIds == null || subjectIds.Count == 0)
        {
            ErrorMessage = "No subject component marks data provided.";
            return RedirectToPage(new { selectedStudentId = studentId, activeTabSemester = semester });
        }

        int updatedCount = 0;
        int insertedCount = 0;

        for (int i = 0; i < subjectIds.Count; i++)
        {
            int subId = subjectIds[i];
            string compType = componentTypes[i];
            decimal maxMarks = maxMarksList[i];
            decimal obtMarks = obtainedMarksList[i];
            string dateStr = examDatesList.Count > i ? examDatesList[i] : "";

            // Validation for marks (Obtained >= 0 and <= MaximumMarks) (PART 18)
            if (obtMarks < 0 || (maxMarks > 0 && obtMarks > maxMarks))
            {
                ErrorMessage = $"{compType} marks ({obtMarks}) cannot be negative or exceed configured maximum marks ({maxMarks}).";
                return RedirectToPage(new { selectedStudentId = studentId, activeTabSemester = semester });
            }

            DateTime? parsedDate = null;
            if (DateTime.TryParse(dateStr, out var d))
            {
                parsedDate = d;
            }

            var subject = await _context.Subjects.FindAsync(subId);
            var subName = subject?.SubjectName ?? $"Subject {subId}";

            var existing = await _context.Marks.FirstOrDefaultAsync(m =>
                m.StudentId == studentId &&
                (m.SubjectId == subId || m.SubjectName == subName) &&
                m.Semester == semester &&
                (m.ComponentType == compType || m.ExamType == compType));

            if (existing != null)
            {
                existing.SubjectId = subId;
                existing.SubjectName = subName;
                existing.ComponentType = compType;
                existing.ExamType = compType;
                existing.MaxMarks = maxMarks;
                existing.ObtainedMarks = obtMarks;
                existing.ExamDate = parsedDate;
                existing.Course = student.Course;
                existing.Department = student.Department;
                updatedCount++;
            }
            else
            {
                var newMark = new MarksEntity
                {
                    StudentId = studentId,
                    SubjectId = subId,
                    SubjectName = subName,
                    Course = student.Course,
                    Department = student.Department,
                    Semester = semester,
                    ComponentType = compType,
                    ExamType = compType,
                    MaxMarks = maxMarks,
                    ObtainedMarks = obtMarks,
                    ExamDate = parsedDate
                };
                _context.Marks.Add(newMark);
                insertedCount++;
            }
        }

        await _context.SaveChangesAsync();

        // Broadcast Real-Time SignalR Event
        await _hubContext.Clients.All.SendAsync("MarksUpdated", new { studentId, semester });

        SuccessMessage = $"Marks successfully updated for {student.FullName} ({semester}). {insertedCount} inserted, {updatedCount} updated.";
        return RedirectToPage(new { selectedStudentId = studentId, activeTabSemester = semester });
    }

    // Teacher Generate Result PDF
    public async Task<IActionResult> OnGetDownloadPdfAsync(string studentId, string semester)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (student == null) return NotFound("Student not found.");

        var allCourseSubjects = await _context.Subjects
            .Where(s => s.Course == student.Course && s.Status == "Active")
            .ToListAsync();

        var semSubjects = allCourseSubjects.Where(s => s.Semester == semester).ToList();
        var subjectIds = allCourseSubjects.Select(s => s.SubjectId).ToList();

        var components = await _context.SubjectComponents
            .Where(c => subjectIds.Contains(c.SubjectId))
            .ToListAsync();

        var allMarks = await _context.Marks
            .Where(m => m.StudentId == student.StudentId)
            .ToListAsync();

        var semMarks = allMarks.Where(m => m.Semester == semester).ToList();

        var semResult = ResultCalculationService.CalculateSemesterResult(
            semester,
            semSubjects,
            components,
            semMarks);

        var overallResult = ResultCalculationService.CalculateOverallResult(
            student,
            allCourseSubjects,
            components,
            allMarks);

        byte[] pdfBytes = ResultPdfService.GenerateSemesterResultPdf(student, semResult, overallResult);

        var fileName = $"Result_{student.StudentId}_{semester.Replace(" ", "_")}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}

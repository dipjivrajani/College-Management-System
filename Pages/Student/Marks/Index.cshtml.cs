using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;
using CollegeManagementSystem.Services;
using CollegeManagementSystem.Helpers;
using MarksEntity = CollegeManagementSystem.Models.Marks;

namespace CollegeManagementSystem.Pages.StudentPortal.StudentMarks;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Student? CurrentStudent { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SelectedSemester { get; set; } = "Semester 1";

    public List<Subject> SemesterSubjects { get; set; } = new();
    public List<SubjectComponent> SubjectComponents { get; set; } = new();
    public List<MarksEntity> StudentSemesterMarks { get; set; } = new();

    public SemesterResultDto? CurrentSemesterResult { get; set; }
    public OverallResultDto? OverallAcademicResult { get; set; }

    public static readonly List<string> Semesters = new()
    {
        "Semester 1", "Semester 2", "Semester 3", "Semester 4", "Semester 5", "Semester 6"
    };

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

        if (string.IsNullOrEmpty(SelectedSemester))
        {
            SelectedSemester = CurrentStudent.Semester;
        }

        // Load subjects for student's course
        var allCourseSubjects = await _context.Subjects
            .Where(s => s.Course == CurrentStudent.Course && s.Status == "Active")
            .OrderBy(s => s.SubjectName)
            .ToListAsync();

        SemesterSubjects = allCourseSubjects.Where(s => s.Semester == SelectedSemester).ToList();
        var subjectIds = allCourseSubjects.Select(s => s.SubjectId).ToList();

        SubjectComponents = await _context.SubjectComponents
            .Where(c => subjectIds.Contains(c.SubjectId))
            .ToListAsync();

        foreach (var s in SemesterSubjects)
        {
            s.Components = SubjectComponents.Where(c => c.SubjectId == s.SubjectId).ToList();
        }

        // Query only this student's marks
        var allStudentMarks = await _context.Marks
            .Where(m => m.StudentId == targetStudentId)
            .ToListAsync();

        StudentSemesterMarks = allStudentMarks.Where(m => m.Semester == SelectedSemester).ToList();

        // Calculate Semester & Overall Results using centralized ResultCalculationService
        CurrentSemesterResult = ResultCalculationService.CalculateSemesterResult(
            SelectedSemester,
            SemesterSubjects,
            SubjectComponents,
            StudentSemesterMarks);

        OverallAcademicResult = ResultCalculationService.CalculateOverallResult(
            CurrentStudent,
            allCourseSubjects,
            SubjectComponents,
            allStudentMarks);

        return Page();
    }

    // Student Download Result PDF
    public async Task<IActionResult> OnGetDownloadPdfAsync(string semester)
    {
        var (succeeded, studentId, studentName, _, _) = await AuthHelper.GetStudentUserAsync(HttpContext);
        if (!succeeded)
        {
            return RedirectToPage("/Login", new { role = "Student" });
        }

        string targetStudentId = studentId;
        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == targetStudentId);
        if (student == null) return NotFound("Student not found.");

        if (string.IsNullOrEmpty(semester)) semester = student.Semester;

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

        var fileName = $"Official_Result_{student.StudentId}_{semester.Replace(" ", "_")}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}

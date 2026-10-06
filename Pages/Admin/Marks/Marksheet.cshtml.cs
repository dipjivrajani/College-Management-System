using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;
using CollegeManagementSystem.Services;
using CollegeManagementSystem.Helpers;

namespace CollegeManagementSystem.Pages.AdminPortal.MarksAdmin;

public class MarksheetModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public MarksheetModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string StudentId { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Semester { get; set; } = "Semester 1";

    [BindProperty(SupportsGet = true)]
    public int Attempt { get; set; } = 1;

    public Student? Student { get; set; }
    public SemesterResultDto? SemesterResult { get; set; }
    public OverallResultDto? OverallResult { get; set; }
    public ResultAttempt? ResultAttemptRecord { get; set; }
    public DateTime GeneratedDate { get; set; } = DateTime.Now;

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(StudentId))
        {
            return NotFound("Student ID is required.");
        }

        Student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == StudentId);
        if (Student == null)
        {
            return NotFound("Student record not found.");
        }

        if (Attempt <= 0) Attempt = 1;

        // Fetch subjects for student's course + semester respecting academic year version
        var allCourseSubjects = await _context.Subjects
            .Where(s => s.Course == Student.Course && s.Status != "Inactive")
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.SubjectName)
            .ToListAsync();

        var semSubjects = allCourseSubjects.Where(s => s.Semester == Semester).ToList();
        var sIds = allCourseSubjects.Select(s => s.SubjectId).ToList();

        var components = await _context.SubjectComponents.Where(c => sIds.Contains(c.SubjectId)).ToListAsync();

        foreach (var s in semSubjects)
        {
            s.Components = components.Where(c => c.SubjectId == s.SubjectId).ToList();
        }

        // Fetch marks for this student and semester + attempt
        var allStudentMarks = await _context.Marks.Where(m => m.StudentId == StudentId).ToListAsync();
        var semMarks = allStudentMarks.Where(m => m.Semester == Semester && (m.Attempt == Attempt || m.Attempt == 0)).ToList();

        // Calculate semester result using centralized service
        SemesterResult = ResultCalculationService.CalculateSemesterResult(Semester, semSubjects, components, semMarks, Attempt);

        // Calculate overall result across completed semesters
        OverallResult = ResultCalculationService.CalculateOverallResult(Student, allCourseSubjects, components, allStudentMarks);

        // Fetch ResultAttempt metadata
        ResultAttemptRecord = await _context.ResultAttempts
            .FirstOrDefaultAsync(r => r.StudentId == StudentId && r.Semester == Semester && r.Attempt == Attempt);

        return Page();
    }
}

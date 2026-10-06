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

namespace CollegeManagementSystem.Pages.AdminPortal.MarksAdmin;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<CollegeManagementHub> _hubContext;

    public IndexModel(ApplicationDbContext context, IHubContext<CollegeManagementHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // Top Filter Properties (PART 1 & 15)
    [BindProperty(SupportsGet = true)]
    public string SelectedAcademicYear { get; set; } = "2025-26";

    [BindProperty(SupportsGet = true)]
    public string SelectedCourse { get; set; } = "B.Sc. Botany";

    [BindProperty(SupportsGet = true)]
    public string SelectedSemester { get; set; } = "Semester 1";

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    // Student Record View Properties (PART 16 & 17)
    [BindProperty(SupportsGet = true)]
    public string? SelectedStudentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ActiveTabSemester { get; set; } = "Semester 1";

    [BindProperty(SupportsGet = true)]
    public int SelectedAttempt { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    // Data lists
    public List<AcademicYear> AcademicYearsList { get; set; } = new();
    public List<Student> StudentsList { get; set; } = new();
    public Student? SelectedStudent { get; set; }
    public List<Subject> SemesterSubjects { get; set; } = new();
    public List<SubjectComponent> SubjectComponents { get; set; } = new();
    public List<MarksEntity> StudentSemesterMarks { get; set; } = new();
    public List<ResultAttempt> StudentAttemptsList { get; set; } = new();

    public SemesterResultDto? CurrentSemesterResult { get; set; }
    public OverallResultDto? OverallAcademicResult { get; set; }

    // Course list sorted strictly A-Z (PART 4 & 33)
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

        // 2. If a specific student's academic record is being viewed/edited (PART 16)
        if (!string.IsNullOrEmpty(SelectedStudentId))
        {
            SelectedStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == SelectedStudentId);
            if (SelectedStudent != null)
            {
                if (string.IsNullOrEmpty(ActiveTabSemester))
                {
                    ActiveTabSemester = SelectedStudent.Semester;
                }

                // Query all subjects for the student's program respecting syllabus academic year versioning (PART 3 & 28)
                var allCourseSubjects = await _context.Subjects
                    .Where(s => s.Course == SelectedStudent.Course && s.Status != "Inactive")
                    .Where(s => s.AcademicYear == SelectedAcademicYear || string.IsNullOrEmpty(s.AcademicYear))
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.SubjectName)
                    .ToListAsync();

                // Semester-specific subjects (Display ONLY active semester! PART 1, 16, 17)
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

                // Query attempts for this semester (PART 22, 23)
                StudentAttemptsList = await _context.ResultAttempts
                    .Where(r => r.StudentId == SelectedStudent.StudentId && r.Semester == ActiveTabSemester)
                    .OrderBy(r => r.Attempt)
                    .ToListAsync();

                if (SelectedAttempt <= 0)
                {
                    SelectedAttempt = StudentAttemptsList.Count > 0 ? StudentAttemptsList.Max(r => r.Attempt) : 1;
                }

                // Load all marks for this student
                var allStudentMarks = await _context.Marks
                    .Where(m => m.StudentId == SelectedStudent.StudentId)
                    .ToListAsync();

                // Marks for selected semester and attempt
                StudentSemesterMarks = allStudentMarks
                    .Where(m => m.Semester == ActiveTabSemester && (m.Attempt == SelectedAttempt || m.Attempt == 0))
                    .ToList();

                // Calculate selected semester result using centralized service
                CurrentSemesterResult = ResultCalculationService.CalculateSemesterResult(
                    ActiveTabSemester,
                    SemesterSubjects,
                    SubjectComponents,
                    StudentSemesterMarks,
                    SelectedAttempt);

                // Calculate overall academic result across completed semesters (PART 21)
                OverallAcademicResult = ResultCalculationService.CalculateOverallResult(
                    SelectedStudent,
                    allCourseSubjects,
                    SubjectComponents,
                    allStudentMarks);

                return Page();
            }
        }

        // 3. Directory Mode: Query Students matching top filters (PART 15)
        var query = _context.Students.AsQueryable();

        if (!string.IsNullOrEmpty(SelectedCourse))
        {
            query = query.Where(s => s.Course == SelectedCourse);
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

        // Order students A-Z strictly by FullName (PART 33)
        StudentsList = await query.OrderBy(s => s.FullName).ToListAsync();

        return Page();
    }

    // Save Marks Handler (PART 18, 19, 20, 22, 35)
    public async Task<IActionResult> OnPostSaveStudentMarksAsync(
        string studentId,
        string academicYear,
        string semester,
        int attempt,
        List<int> subjectIds,
        List<string> componentTypes,
        List<decimal> maxMarksList,
        List<decimal> obtainedMarksList,
        List<string> examDatesList)
    {
        var (succeeded, _, _, _) = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (student == null)
        {
            ErrorMessage = "Student record not found.";
            return RedirectToPage(new { selectedAcademicYear = academicYear, selectedCourse = SelectedCourse, selectedSemester = semester });
        }

        if (attempt <= 0) attempt = 1;

        if (subjectIds == null || subjectIds.Count == 0)
        {
            ErrorMessage = "No subject component marks data provided.";
            return RedirectToPage(new { selectedAcademicYear = academicYear, selectedCourse = student.Course, selectedSemester = semester, selectedStudentId = studentId, activeTabSemester = semester, selectedAttempt = attempt });
        }

        // Validate each component (PART 18 & 35):
        // Obtained >= 0 and Obtained <= MaxMarks (Do NOT globally force max=30 for internal; use configured max!)
        for (int i = 0; i < subjectIds.Count; i++)
        {
            int subId = subjectIds[i];
            string compType = componentTypes[i];
            decimal maxMarks = maxMarksList[i];
            decimal obtMarks = obtainedMarksList[i];

            if (obtMarks < 0)
            {
                ErrorMessage = $"Marks cannot be negative. ({compType}: {obtMarks})";
                return RedirectToPage(new { selectedAcademicYear = academicYear, selectedCourse = student.Course, selectedSemester = semester, selectedStudentId = studentId, activeTabSemester = semester, selectedAttempt = attempt });
            }

            if (maxMarks > 0 && obtMarks > maxMarks)
            {
                ErrorMessage = $"{compType} obtained marks ({obtMarks}) cannot exceed configured maximum marks ({maxMarks}).";
                return RedirectToPage(new { selectedAcademicYear = academicYear, selectedCourse = student.Course, selectedSemester = semester, selectedStudentId = studentId, activeTabSemester = semester, selectedAttempt = attempt });
            }
        }

        // Save / Update marks in DB
        var existingMarks = await _context.Marks
            .Where(m => m.StudentId == studentId && m.Semester == semester && m.Attempt == attempt)
            .ToListAsync();

        for (int i = 0; i < subjectIds.Count; i++)
        {
            int subId = subjectIds[i];
            string compType = componentTypes[i];
            decimal maxMarks = maxMarksList[i];
            decimal obtMarks = obtainedMarksList[i];
            DateTime? parsedExamDate = null;
            if (examDatesList != null && examDatesList.Count > i && DateTime.TryParse(examDatesList[i], out var d))
            {
                parsedExamDate = d;
            }

            var subject = await _context.Subjects.FindAsync(subId);
            string subName = subject?.SubjectName ?? $"Subject #{subId}";

            var record = existingMarks.FirstOrDefault(m => 
                m.SubjectId == subId && 
                (m.ComponentType.Equals(compType, StringComparison.OrdinalIgnoreCase) || m.ExamType.Equals(compType, StringComparison.OrdinalIgnoreCase)));

            if (record != null)
            {
                record.ObtainedMarks = obtMarks;
                record.MaxMarks = maxMarks;
                record.ComponentType = compType;
                record.ExamType = compType;
                record.AcademicYear = academicYear;
                if (parsedExamDate.HasValue) record.ExamDate = parsedExamDate;
            }
            else
            {
                _context.Marks.Add(new MarksEntity
                {
                    StudentId = studentId,
                    SubjectId = subId,
                    SubjectName = subName,
                    Course = student.Course,
                    Department = student.Department,
                    AcademicYear = academicYear,
                    Semester = semester,
                    Attempt = attempt,
                    ComponentType = compType,
                    ExamType = compType,
                    MaxMarks = maxMarks,
                    ObtainedMarks = obtMarks,
                    ExamDate = parsedExamDate ?? DateTime.Today
                });
            }
        }

        await _context.SaveChangesAsync();

        // Recalculate Semester Result & Update/Insert ResultAttempt (PART 20, 22, 23)
        var allMarksForSem = await _context.Marks
            .Where(m => m.StudentId == studentId && m.Semester == semester && m.Attempt == attempt)
            .ToListAsync();

        var subjects = await _context.Subjects
            .Where(s => s.Course == student.Course && s.Semester == semester)
            .Where(s => s.AcademicYear == academicYear || string.IsNullOrEmpty(s.AcademicYear))
            .ToListAsync();

        var sIds = subjects.Select(s => s.SubjectId).ToList();
        var components = await _context.SubjectComponents.Where(c => sIds.Contains(c.SubjectId)).ToListAsync();

        var semResult = ResultCalculationService.CalculateSemesterResult(semester, subjects, components, allMarksForSem, attempt);

        var existingAttempt = await _context.ResultAttempts
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.Semester == semester && r.Attempt == attempt);

        if (existingAttempt != null)
        {
            existingAttempt.AcademicYear = academicYear;
            existingAttempt.TotalObtainedMarks = semResult.TotalObtainedMarks;
            existingAttempt.TotalMaxMarks = semResult.TotalMaxMarks;
            existingAttempt.Percentage = semResult.Percentage;
            existingAttempt.SGPA = semResult.SGPA;
            existingAttempt.ResultStatus = semResult.ResultStatus;
            existingAttempt.ExamDate = DateTime.Today;
        }
        else
        {
            _context.ResultAttempts.Add(new ResultAttempt
            {
                StudentId = studentId,
                AcademicYear = academicYear,
                Semester = semester,
                Attempt = attempt,
                TotalObtainedMarks = semResult.TotalObtainedMarks,
                TotalMaxMarks = semResult.TotalMaxMarks,
                Percentage = semResult.Percentage,
                SGPA = semResult.SGPA,
                ResultStatus = semResult.ResultStatus,
                ExamDate = DateTime.Today
            });
        }

        await _context.SaveChangesAsync();

        // Real-time broadcast to any open Student result page (PART 34)
        try
        {
            await _hubContext.Clients.All.SendAsync("MarksUpdated", new { studentId, semester, attempt, sgpa = semResult.SGPA, status = semResult.ResultStatus });
        }
        catch { }

        SuccessMessage = $"Marks saved successfully for {student.FullName} ({semester}, Attempt {attempt}). SGPA: {(semResult.SGPA.HasValue ? semResult.SGPA.Value.ToString("0.00") : "--")} | Result: {semResult.ResultStatus}.";

        return RedirectToPage(new
        {
            selectedAcademicYear = academicYear,
            selectedCourse = student.Course,
            selectedSemester = semester,
            selectedStudentId = studentId,
            activeTabSemester = semester,
            selectedAttempt = attempt
        });
    }

    // Add New Attempt for Backlog/Repeat Exam (PART 22 & 23)
    public async Task<IActionResult> OnPostCreateNewAttemptAsync(string studentId, string academicYear, string semester)
    {
        var (succeeded, _, _, _) = await AuthHelper.GetAdminUserAsync(HttpContext);
        if (!succeeded) return RedirectToPage("/Login", new { role = "Admin" });

        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
        if (student == null) return RedirectToPage();

        var existingAttempts = await _context.ResultAttempts
            .Where(r => r.StudentId == studentId && r.Semester == semester)
            .ToListAsync();

        int nextAttempt = (existingAttempts.Count > 0 ? existingAttempts.Max(r => r.Attempt) : 1) + 1;

        var newAttemptRecord = new ResultAttempt
        {
            StudentId = studentId,
            AcademicYear = academicYear,
            Semester = semester,
            Attempt = nextAttempt,
            TotalObtainedMarks = 0,
            TotalMaxMarks = 0,
            Percentage = 0,
            SGPA = null,
            ResultStatus = "INCOMPLETE",
            ExamDate = DateTime.Today,
            Remarks = $"Repeat/Backlog Attempt #{nextAttempt}"
        };

        _context.ResultAttempts.Add(newAttemptRecord);
        await _context.SaveChangesAsync();

        SuccessMessage = $"Created Attempt #{nextAttempt} for {student.FullName} ({semester}). You can now record repeat exam marks.";

        return RedirectToPage(new
        {
            selectedAcademicYear = academicYear,
            selectedCourse = student.Course,
            selectedSemester = semester,
            selectedStudentId = studentId,
            activeTabSemester = semester,
            selectedAttempt = nextAttempt
        });
    }
}

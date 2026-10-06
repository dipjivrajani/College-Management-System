using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;
using CollegeManagementSystem.Helpers;
using CollegeManagementSystem.Hubs;

namespace CollegeManagementSystem.Pages.TeacherPortal.AttendanceTeacher;

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
    public string ActiveTab { get; set; } = "mark";

    [BindProperty(SupportsGet = true)]
    public string SelectedCourse { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string SelectedSemester { get; set; } = "Semester 1";

    [BindProperty(SupportsGet = true)]
    public string MarkDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

    // History Filters
    [BindProperty(SupportsGet = true)]
    public string? HistorySemester { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? HistoryStudent { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DateTo { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public Models.Teacher? CurrentTeacher { get; set; }

    public class StudentAttendanceItem
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string Status { get; set; } = "Present";
        public bool IsAlreadyMarked { get; set; }
    }

    public List<StudentAttendanceItem> StudentsToMark { get; set; } = new();

    public class HistoryItem
    {
        public int AttendanceId { get; set; }
        public DateTime Date { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string Status { get; set; } = "Present";
    }

    public List<HistoryItem> HistoryRecords { get; set; } = new();

    public int TotalClassesCount { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AttendancePercentage { get; set; }

    public List<string> Semesters { get; } = new()
    {
        "Semester 1", "Semester 2", "Semester 3", "Semester 4", "Semester 5", "Semester 6"
    };

    public List<string> Courses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var teacherUser = await AuthHelper.GetTeacherUserAsync(HttpContext);
        if (!teacherUser.Succeeded)
        {
            return RedirectToPage("/Login", new { role = "Teacher" });
        }
        var teacherId = teacherUser.TeacherId;

        CurrentTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == teacherId);

        // Fetch courses list
        Courses = await _context.Courses.OrderBy(c => c.CourseName).Select(c => c.CourseName).ToListAsync();

        // Default SelectedCourse based on Teacher's department
        if (string.IsNullOrEmpty(SelectedCourse))
        {
            if (CurrentTeacher != null && !string.IsNullOrEmpty(CurrentTeacher.Department))
            {
                var match = Courses.FirstOrDefault(c => c.ToLower().Contains(CurrentTeacher.Department.ToLower()));
                SelectedCourse = match ?? (Courses.FirstOrDefault() ?? "B.Sc. Botany");
            }
            else
            {
                SelectedCourse = Courses.FirstOrDefault() ?? "B.Sc. Botany";
            }
        }

        if (!DateTime.TryParse(MarkDate, out var parsedDate))
        {
            parsedDate = DateTime.Today;
            MarkDate = parsedDate.ToString("yyyy-MM-dd");
        }

        // 1. Fetch active students for this course & semester (A-Z)
        var students = await _context.Students
            .Where(s => s.Status == "Active" && s.Course == SelectedCourse && s.Semester == SelectedSemester)
            .OrderBy(s => s.FullName)
            .ToListAsync();

        // Check if attendance already marked
        var existing = await _context.Attendances
            .Where(a => a.Date.Date == parsedDate.Date && a.Course == SelectedCourse && a.Semester == SelectedSemester)
            .ToListAsync();

        var existingMap = existing.ToDictionary(a => a.StudentId, a => a.Status);

        StudentsToMark = students.Select(s => new StudentAttendanceItem
        {
            StudentId = s.StudentId,
            FullName = s.FullName,
            Course = s.Course,
            Semester = s.Semester,
            Status = existingMap.ContainsKey(s.StudentId) ? existingMap[s.StudentId] : "Present",
            IsAlreadyMarked = existingMap.ContainsKey(s.StudentId)
        }).ToList();

        // 2. Fetch history records for this course
        var histQuery = from a in _context.Attendances
                        join s in _context.Students on a.StudentId equals s.StudentId into sGroup
                        from student in sGroup.DefaultIfEmpty()
                        where a.Course == SelectedCourse
                        select new HistoryItem
                        {
                            AttendanceId = a.AttendanceId,
                            Date = a.Date,
                            StudentId = a.StudentId,
                            StudentName = student != null ? student.FullName : a.StudentId,
                            Course = a.Course,
                            Semester = a.Semester,
                            Status = a.Status
                        };

        if (!string.IsNullOrEmpty(HistorySemester) && HistorySemester != "All")
        {
            histQuery = histQuery.Where(h => h.Semester == HistorySemester);
        }

        if (!string.IsNullOrEmpty(HistoryStudent))
        {
            var search = HistoryStudent.Trim().ToLower();
            histQuery = histQuery.Where(h => h.StudentName.ToLower().Contains(search) || h.StudentId.ToLower().Contains(search));
        }

        if (DateTime.TryParse(DateFrom, out var df))
        {
            histQuery = histQuery.Where(h => h.Date.Date >= df.Date);
        }

        if (DateTime.TryParse(DateTo, out var dt))
        {
            histQuery = histQuery.Where(h => h.Date.Date <= dt.Date);
        }

        HistoryRecords = await histQuery
            .OrderByDescending(h => h.Date)
            .ThenBy(h => h.StudentName)
            .ToListAsync();

        TotalClassesCount = HistoryRecords.Count;
        PresentCount = HistoryRecords.Count(h => h.Status.Equals("Present", StringComparison.OrdinalIgnoreCase));
        AbsentCount = HistoryRecords.Count(h => h.Status.Equals("Absent", StringComparison.OrdinalIgnoreCase));
        AttendancePercentage = TotalClassesCount > 0
            ? Math.Round((decimal)PresentCount / TotalClassesCount * 100, 1)
            : 0;

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAttendanceAsync(
        string course,
        string semester,
        string markDate,
        List<string> studentIds,
        List<string> statuses)
    {
        if (string.IsNullOrWhiteSpace(markDate) || !DateTime.TryParse(markDate, out var attendanceDate))
        {
            ErrorMessage = "Valid attendance date is required.";
            return RedirectToPage(new { course, semester, markDate, activeTab = "mark" });
        }

        if (studentIds == null || studentIds.Count == 0)
        {
            ErrorMessage = "No students selected.";
            return RedirectToPage(new { course, semester, markDate, activeTab = "mark" });
        }

        // Derive department from course
        var dept = Pages.Admin.Students.IndexModel.GetDepartment(course);

        var existingRecords = await _context.Attendances
            .Where(a => a.Date.Date == attendanceDate.Date && a.Course == course && a.Semester == semester)
            .ToListAsync();

        int updatedCount = 0;
        int insertedCount = 0;

        for (int i = 0; i < studentIds.Count; i++)
        {
            var sId = studentIds[i];
            var status = (statuses != null && i < statuses.Count) ? statuses[i] : "Present";
            if (status != "Present" && status != "Absent") status = "Present";

            var existing = existingRecords.FirstOrDefault(a => a.StudentId == sId);
            if (existing != null)
            {
                existing.Status = status;
                existing.Department = dept;
                updatedCount++;
            }
            else
            {
                _context.Attendances.Add(new CollegeManagementSystem.Models.Attendance
                {
                    StudentId = sId,
                    Course = course,
                    Department = dept,
                    Semester = semester,
                    Date = attendanceDate,
                    Status = status
                });
                insertedCount++;
            }
        }

        await _context.SaveChangesAsync();

        // Broadcast real-time AttendanceUpdated event via SignalR
        try
        {
            await _hubContext.Clients.All.SendAsync("AttendanceUpdated", new
            {
                course,
                semester,
                markDate = attendanceDate.ToString("yyyy-MM-dd")
            });
        }
        catch { }

        SuccessMessage = $"Attendance recorded successfully for {attendanceDate:dd/MM/yyyy}! ({insertedCount} new, {updatedCount} updated)";
        return RedirectToPage(new { course, semester, markDate, activeTab = "mark" });
    }
}

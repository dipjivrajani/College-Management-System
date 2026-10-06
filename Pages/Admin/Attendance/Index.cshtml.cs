using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;
using CollegeManagementSystem.Hubs;

namespace CollegeManagementSystem.Pages.AdminPortal.AttendanceAdmin;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<CollegeManagementHub> _hubContext;

    public IndexModel(ApplicationDbContext context, IHubContext<CollegeManagementHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // Tab state: "mark" or "history"
    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "mark";

    // Marking Filter Fields
    [BindProperty(SupportsGet = true)]
    public string SelectedDepartment { get; set; } = "Botany";

    [BindProperty(SupportsGet = true)]
    public string SelectedCourse { get; set; } = "B.Sc. Botany";

    [BindProperty(SupportsGet = true)]
    public string SelectedSemester { get; set; } = "Semester 1";

    [BindProperty(SupportsGet = true)]
    public string MarkDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");

    // History Filter Fields
    [BindProperty(SupportsGet = true)]
    public string? HistoryDepartment { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? HistoryCourse { get; set; }

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

    // DTO for students to mark attendance
    public class StudentAttendanceItem
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string Status { get; set; } = "Present"; // Present, Absent
        public bool IsAlreadyMarked { get; set; }
    }

    public List<StudentAttendanceItem> StudentsToMark { get; set; } = new();

    // History Item DTO
    public class AttendanceHistoryItem
    {
        public int AttendanceId { get; set; }
        public DateTime Date { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Semester { get; set; } = string.Empty;
        public string Status { get; set; } = "Present";
    }

    public List<AttendanceHistoryItem> HistoryRecords { get; set; } = new();

    // Calculated Statistics for History View
    public int TotalClassesCount { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AttendancePercentage { get; set; }

    // Dropdown helpers
    public List<string> Departments { get; } = new()
    {
        "Botany", "Chemistry", "Computer Science", "Forensic Science", "Microbiology", "Physics", "Zoology"
    };

    public List<string> Semesters { get; } = new()
    {
        "Semester 1", "Semester 2", "Semester 3", "Semester 4", "Semester 5", "Semester 6"
    };

    public Dictionary<string, string> DepartmentToCourseMap { get; } = new()
    {
        { "Botany", "B.Sc. Botany" },
        { "Chemistry", "B.Sc. Chemistry" },
        { "Computer Science", "B.Sc. Computer Science" },
        { "Forensic Science", "B.Sc. Forensic Science" },
        { "Microbiology", "B.Sc. Microbiology" },
        { "Physics", "B.Sc. Physics" },
        { "Zoology", "B.Sc. Zoology" }
    };

    public async Task OnGetAsync()
    {
        // Sync course with selected department if not explicitly set
        if (!string.IsNullOrEmpty(SelectedDepartment) && DepartmentToCourseMap.ContainsKey(SelectedDepartment))
        {
            if (string.IsNullOrEmpty(SelectedCourse) || SelectedCourse == "All")
            {
                SelectedCourse = DepartmentToCourseMap[SelectedDepartment];
            }
        }

        // Parse mark date
        if (!DateTime.TryParse(MarkDate, out var parsedMarkDate))
        {
            parsedMarkDate = DateTime.Today;
            MarkDate = parsedMarkDate.ToString("yyyy-MM-dd");
        }

        // 1. Load active students for the selected Course & Semester (A-Z by Name)
        var activeStudentsQuery = _context.Students
            .Where(s => s.Status == "Active");

        if (!string.IsNullOrEmpty(SelectedCourse) && SelectedCourse != "All")
        {
            activeStudentsQuery = activeStudentsQuery.Where(s => s.Course == SelectedCourse);
        }

        if (!string.IsNullOrEmpty(SelectedSemester) && SelectedSemester != "All")
        {
            activeStudentsQuery = activeStudentsQuery.Where(s => s.Semester == SelectedSemester);
        }

        var activeStudents = await activeStudentsQuery
            .OrderBy(s => s.FullName)
            .ToListAsync();

        // Check if attendance already exists for these students on this date & course
        var existingAttendance = await _context.Attendances
            .Where(a => a.Date.Date == parsedMarkDate.Date && a.Course == SelectedCourse)
            .ToListAsync();

        var existingMap = existingAttendance.ToDictionary(a => a.StudentId, a => a.Status);

        StudentsToMark = activeStudents.Select(s => new StudentAttendanceItem
        {
            StudentId = s.StudentId,
            FullName = s.FullName,
            Course = s.Course,
            Semester = s.Semester,
            Status = existingMap.ContainsKey(s.StudentId) ? existingMap[s.StudentId] : "Present",
            IsAlreadyMarked = existingMap.ContainsKey(s.StudentId)
        }).ToList();

        // 2. Load Attendance History
        var historyQuery = from a in _context.Attendances
                           join s in _context.Students on a.StudentId equals s.StudentId into sGroup
                           from student in sGroup.DefaultIfEmpty()
                           select new AttendanceHistoryItem
                           {
                               AttendanceId = a.AttendanceId,
                               Date = a.Date,
                               StudentId = a.StudentId,
                               StudentName = student != null ? student.FullName : a.StudentId,
                               Course = a.Course,
                               Department = !string.IsNullOrEmpty(a.Department) ? a.Department : (student != null ? student.Department : ""),
                               Semester = a.Semester,
                               Status = a.Status
                           };

        if (!string.IsNullOrEmpty(HistoryDepartment) && HistoryDepartment != "All")
        {
            historyQuery = historyQuery.Where(h => h.Department == HistoryDepartment);
        }

        if (!string.IsNullOrEmpty(HistoryCourse) && HistoryCourse != "All")
        {
            historyQuery = historyQuery.Where(h => h.Course == HistoryCourse);
        }

        if (!string.IsNullOrEmpty(HistorySemester) && HistorySemester != "All")
        {
            historyQuery = historyQuery.Where(h => h.Semester == HistorySemester);
        }

        if (!string.IsNullOrEmpty(HistoryStudent))
        {
            var search = HistoryStudent.Trim().ToLower();
            historyQuery = historyQuery.Where(h => h.StudentName.ToLower().Contains(search) || h.StudentId.ToLower().Contains(search));
        }

        if (DateTime.TryParse(DateFrom, out var df))
        {
            historyQuery = historyQuery.Where(h => h.Date.Date >= df.Date);
        }

        if (DateTime.TryParse(DateTo, out var dt))
        {
            historyQuery = historyQuery.Where(h => h.Date.Date <= dt.Date);
        }

        // Sort by Date descending (latest first), then Student Name A-Z
        HistoryRecords = await historyQuery
            .OrderByDescending(h => h.Date)
            .ThenBy(h => h.StudentName)
            .ToListAsync();

        // Calculate Stats
        TotalClassesCount = HistoryRecords.Count;
        PresentCount = HistoryRecords.Count(h => h.Status.Equals("Present", StringComparison.OrdinalIgnoreCase));
        AbsentCount = HistoryRecords.Count(h => h.Status.Equals("Absent", StringComparison.OrdinalIgnoreCase));
        AttendancePercentage = TotalClassesCount > 0 
            ? Math.Round((decimal)PresentCount / TotalClassesCount * 100, 1) 
            : 0;
    }

    // Save Attendance (handles both new and updates to prevent duplicates)
    public async Task<IActionResult> OnPostSaveAttendanceAsync(
        string department, 
        string course, 
        string semester, 
        string markDate, 
        List<string> studentIds, 
        List<string> statuses)
    {
        if (string.IsNullOrWhiteSpace(markDate) || !DateTime.TryParse(markDate, out var attendanceDate))
        {
            ErrorMessage = "Valid attendance date is required.";
            return RedirectToPage(new { department, course, semester, markDate, activeTab = "mark" });
        }

        if (studentIds == null || studentIds.Count == 0)
        {
            ErrorMessage = "No active students found to mark attendance.";
            return RedirectToPage(new { department, course, semester, markDate, activeTab = "mark" });
        }

        // Fetch existing records for this date and course to update or add
        var existingRecords = await _context.Attendances
            .Where(a => a.Date.Date == attendanceDate.Date && a.Course == course)
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
                // Update existing record
                existing.Status = status;
                existing.Department = department;
                existing.Semester = semester;
                updatedCount++;
            }
            else
            {
                // Insert new record
                _context.Attendances.Add(new CollegeManagementSystem.Models.Attendance
                {
                    StudentId = sId,
                    Course = course,
                    Department = department,
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

        SuccessMessage = $"Attendance saved successfully for {attendanceDate:dd/MM/yyyy}! ({insertedCount} new, {updatedCount} updated)";
        return RedirectToPage(new { department, course, semester, markDate, activeTab = "mark" });
    }

    // Delete single attendance record with confirmation
    public async Task<IActionResult> OnPostDeleteAttendanceAsync(int attendanceId)
    {
        var record = await _context.Attendances.FindAsync(attendanceId);
        if (record != null)
        {
            _context.Attendances.Remove(record);
            await _context.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("AttendanceUpdated", new
                {
                    course = record.Course,
                    semester = record.Semester,
                    markDate = record.Date.ToString("yyyy-MM-dd")
                });
            }
            catch { }

            SuccessMessage = "Attendance record deleted successfully.";
        }
        else
        {
            ErrorMessage = "Attendance record not found.";
        }

        return RedirectToPage(new { activeTab = "history" });
    }
}

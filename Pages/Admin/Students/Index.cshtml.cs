using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;
using CollegeManagementSystem.Hubs;

namespace CollegeManagementSystem.Pages.Admin.Students;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<CollegeManagementHub> _hubContext;

    public IndexModel(ApplicationDbContext context, IHubContext<CollegeManagementHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // List & Filter properties
    public List<Student> Students { get; set; } = new();
    public List<Course> AvailableCourses { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CourseFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    // Modal state triggers from query params
    public bool ShowAddModal { get; set; }
    public Student? ViewStudent { get; set; }
    public Student? EditStudent { get; set; }
    public Student? PasswordStudent { get; set; }
    public Student? DeleteStudent { get; set; }

    // Success / Feedback Alerts
    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? CreatedCredentials { get; set; }

    // Input model for Adding new student
    [BindProperty]
    public CreateStudentInput NewStudent { get; set; } = new();

    // Input model for Editing student
    [BindProperty]
    public EditStudentInput EditInput { get; set; } = new();

    // Input model for Changing Password
    [BindProperty]
    public ChangePasswordInput PasswordInput { get; set; } = new();

    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }

    public async Task<IActionResult> OnGetAsync(
        string? search,
        string? courseFilter,
        string? statusFilter,
        string? viewId,
        string? editId,
        string? passwordId,
        string? deleteId,
        bool openAdd = false)
    {
        SearchTerm = search;
        CourseFilter = courseFilter;
        StatusFilter = statusFilter;
        ShowAddModal = openAdd;

        AvailableCourses = await _context.Courses
            .Where(c => c.IsActive)
            .OrderBy(c => c.CourseName)
            .ToListAsync();

        TotalCount = await _context.Students.CountAsync();
        ActiveCount = await _context.Students.CountAsync(s => s.Status.ToLower() == "active");
        InactiveCount = await _context.Students.CountAsync(s => s.Status.ToLower() == "inactive");

        var query = _context.Students.AsQueryable();

        // Filter by Search Query
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.FullName.ToLower().Contains(term) ||
                s.StudentId.ToLower().Contains(term) ||
                s.Course.ToLower().Contains(term) ||
                s.Department.ToLower().Contains(term) ||
                s.Mobile.ToLower().Contains(term) ||
                s.Email.ToLower().Contains(term));
        }

        // Filter by Course
        if (!string.IsNullOrWhiteSpace(CourseFilter) && CourseFilter != "All")
        {
            query = query.Where(s => s.Course == CourseFilter);
        }

        // Filter by Status
        if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
        {
            query = query.Where(s => s.Status.ToLower() == StatusFilter.ToLower());
        }

        // Strictly sorted A-Z by Student Name, with natural serial number order
        Students = await query
            .OrderBy(s => s.FullName)
            .ThenBy(s => s.StudentSerialNumber)
            .ToListAsync();

        // Handle View modal
        if (!string.IsNullOrEmpty(viewId))
        {
            ViewStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == viewId);
        }

        // Handle Edit modal
        if (!string.IsNullOrEmpty(editId))
        {
            EditStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == editId);
            if (EditStudent != null)
            {
                EditInput = new EditStudentInput
                {
                    StudentId = EditStudent.StudentId,
                    FullName = EditStudent.FullName,
                    Email = EditStudent.Email,
                    Course = EditStudent.Course,
                    Department = GetDepartment(EditStudent.Course),
                    DateOfBirth = EditStudent.DateOfBirth,
                    Gender = EditStudent.Gender,
                    Mobile = EditStudent.Mobile,
                    Address = EditStudent.Address,
                    City = EditStudent.City,
                    Semester = EditStudent.Semester,
                    FatherName = EditStudent.FatherName,
                    FatherOccupation = EditStudent.FatherOccupation,
                    Status = EditStudent.Status
                };
            }
        }

        // Handle Password modal
        if (!string.IsNullOrEmpty(passwordId))
        {
            PasswordStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == passwordId);
            if (PasswordStudent != null)
            {
                PasswordInput.StudentId = PasswordStudent.StudentId;
                PasswordInput.StudentName = PasswordStudent.FullName;
            }
        }

        // Handle Delete confirmation modal
        if (!string.IsNullOrEmpty(deleteId))
        {
            DeleteStudent = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == deleteId);
        }

        return Page();
    }

    // 1. ADD NEW STUDENT (AUTOMATIC ENROLLMENT NUMBER & INITIAL PASSWORD)
    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewStudent.FullName))
        {
            ModelState.AddModelError("NewStudent.FullName", "Student Full Name is required.");
        }
        if (string.IsNullOrWhiteSpace(NewStudent.Course))
        {
            ModelState.AddModelError("NewStudent.Course", "Please select a Course.");
        }
        if (string.IsNullOrWhiteSpace(NewStudent.Email))
        {
            ModelState.AddModelError("NewStudent.Email", "Email address is required.");
        }

        if (!ModelState.IsValid)
        {
            ShowAddModal = true;
            return await OnGetAsync(SearchTerm, CourseFilter, StatusFilter, null, null, null, null, true);
        }

        int year = NewStudent.AdmissionYear > 0 ? NewStudent.AdmissionYear : DateTime.Today.Year;
        string courseCode = GetCourseCode(NewStudent.Course);
        string prefix = $"{year % 100:D2}SSV{courseCode}";

        // Find independent next serial number for this admission year and course
        var existingSerials = await _context.Students
            .Where(s => s.AdmissionYear == year && s.StudentId.StartsWith(prefix))
            .Select(s => s.StudentSerialNumber)
            .ToListAsync();

        int nextSerial = existingSerials.Any() ? existingSerials.Max() + 1 : 1;
        string serialStr = nextSerial.ToString("D3");
        string enrollmentNumber = $"{prefix}{serialStr}";

        // Auto-generate initial password: Student Name + # + Enrollment serial
        string rawFirstName = NewStudent.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Student";
        string safeFirstName = System.Text.RegularExpressions.Regex.Replace(rawFirstName, @"[^a-zA-Z0-9]", "");
        if (string.IsNullOrEmpty(safeFirstName)) safeFirstName = "Student";
        string initialPlainPassword = $"{safeFirstName}#{serialStr}";
        string hashedPassword = PasswordHelper.HashPassword(initialPlainPassword);

        // Derive department strictly corresponding to the selected course
        string department = GetDepartment(NewStudent.Course);

        var student = new Student
        {
            StudentId = enrollmentNumber,
            FullName = NewStudent.FullName.Trim(),
            Email = NewStudent.Email.Trim(),
            Password = hashedPassword,
            Course = NewStudent.Course,
            Department = department,
            AdmissionYear = year,
            StudentSerialNumber = nextSerial,
            ValidUntil = year + 4, // 5-year validity e.g. 2026-2030
            DateOfBirth = NewStudent.DateOfBirth,
            Gender = NewStudent.Gender,
            Mobile = NewStudent.Mobile?.Trim() ?? string.Empty,
            Address = NewStudent.Address?.Trim() ?? string.Empty,
            City = string.IsNullOrWhiteSpace(NewStudent.City) ? "Bhavnagar" : NewStudent.City.Trim(),
            Semester = string.IsNullOrWhiteSpace(NewStudent.Semester) ? "Semester 1" : NewStudent.Semester,
            FatherName = NewStudent.FatherName?.Trim() ?? string.Empty,
            FatherOccupation = NewStudent.FatherOccupation?.Trim() ?? string.Empty,
            AdmissionDate = NewStudent.AdmissionDate ?? DateTime.Today,
            PhotoPath = "/images/students.jpg",
            Status = string.IsNullOrWhiteSpace(NewStudent.Status) ? "Active" : NewStudent.Status
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("StudentCreated", new { studentId = student.StudentId, fullName = student.FullName, course = student.Course });
            await _hubContext.Clients.All.SendAsync("StudentUpdated", new { studentId = student.StudentId, fullName = student.FullName });
        }
        catch { }

        StatusMessage = $"Student '{student.FullName}' was successfully added with Enrollment No: {student.StudentId}. Valid through {student.ValidUntil}.";
        CreatedCredentials = $"Enrollment Number: {student.StudentId} | Initial Password: {initialPlainPassword}";

        return RedirectToPage("/Admin/Students/Index");
    }

    // 2. EDIT STUDENT DETAILS
    public async Task<IActionResult> OnPostEditAsync()
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == EditInput.StudentId);
        if (student == null)
        {
            StatusMessage = "Error: Student not found.";
            return RedirectToPage("/Admin/Students/Index");
        }

        if (string.IsNullOrWhiteSpace(EditInput.FullName))
        {
            ModelState.AddModelError("EditInput.FullName", "Full Name is required.");
        }
        if (string.IsNullOrWhiteSpace(EditInput.Email))
        {
            ModelState.AddModelError("EditInput.Email", "Email Address is required.");
        }

        if (!ModelState.IsValid)
        {
            return await OnGetAsync(SearchTerm, CourseFilter, StatusFilter, null, EditInput.StudentId, null, null, false);
        }

        student.FullName = EditInput.FullName.Trim();
        student.Email = EditInput.Email.Trim();
        student.DateOfBirth = EditInput.DateOfBirth;
        student.Gender = EditInput.Gender;
        student.Mobile = EditInput.Mobile?.Trim() ?? string.Empty;
        student.Address = EditInput.Address?.Trim() ?? string.Empty;
        student.City = EditInput.City?.Trim() ?? "Bhavnagar";
        student.Course = EditInput.Course;
        student.Department = GetDepartment(EditInput.Course);
        student.Semester = EditInput.Semester;
        student.FatherName = EditInput.FatherName?.Trim() ?? string.Empty;
        student.FatherOccupation = EditInput.FatherOccupation?.Trim() ?? string.Empty;
        student.Status = EditInput.Status;

        await _context.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("StudentUpdated", new { studentId = student.StudentId, fullName = student.FullName, course = student.Course });
        }
        catch { }

        StatusMessage = $"Student '{student.FullName}' ({student.StudentId}) details updated successfully.";
        return RedirectToPage("/Admin/Students/Index");
    }

    // 3. CHANGE PASSWORD (HASHED BEFORE SAVING)
    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == PasswordInput.StudentId);
        if (student == null)
        {
            StatusMessage = "Error: Student not found.";
            return RedirectToPage("/Admin/Students/Index");
        }

        if (string.IsNullOrWhiteSpace(PasswordInput.NewPassword) || PasswordInput.NewPassword.Length < 4)
        {
            StatusMessage = "Error: Password must be at least 4 characters long.";
            return RedirectToPage("/Admin/Students/Index", new { passwordId = PasswordInput.StudentId });
        }

        if (PasswordInput.NewPassword != PasswordInput.ConfirmPassword)
        {
            StatusMessage = "Error: New password and confirmation password do not match.";
            return RedirectToPage("/Admin/Students/Index", new { passwordId = PasswordInput.StudentId });
        }

        student.Password = PasswordHelper.HashPassword(PasswordInput.NewPassword);
        await _context.SaveChangesAsync();

        StatusMessage = $"Password for student '{student.FullName}' ({student.StudentId}) has been successfully updated and securely hashed.";
        return RedirectToPage("/Admin/Students/Index");
    }

    // 4. DELETE STUDENT (CONFIRMED)
    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == id);
        if (student != null)
        {
            // Remove any related attendance/marks to avoid foreign key conflicts
            var attendances = await _context.Attendances.Where(a => a.StudentId == id).ToListAsync();
            _context.Attendances.RemoveRange(attendances);

            var marks = await _context.Marks.Where(m => m.StudentId == id).ToListAsync();
            _context.Marks.RemoveRange(marks);

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("StudentDeleted", new { studentId = id });
                await _hubContext.Clients.All.SendAsync("StudentUpdated", new { studentId = id });
            }
            catch { }

            StatusMessage = $"Student '{student.FullName}' ({student.StudentId}) has been permanently deleted.";
        }

        return RedirectToPage("/Admin/Students/Index");
    }

    // 5. ACTIVATE / DEACTIVATE TOGGLE
    public async Task<IActionResult> OnPostToggleStatusAsync(string id)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == id);
        if (student != null)
        {
            bool wasActive = student.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            student.Status = wasActive ? "Inactive" : "Active";
            await _context.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("StudentUpdated", new { studentId = student.StudentId, status = student.Status });
            }
            catch { }

            StatusMessage = $"Student '{student.FullName}' ({student.StudentId}) is now {(student.Status == "Active" ? "Active" : "Inactive")}.";
        }

        return RedirectToPage("/Admin/Students/Index", new { search = SearchTerm, courseFilter = CourseFilter, statusFilter = StatusFilter });
    }

    // Helpers
    public static string GetCourseCode(string courseName)
    {
        if (string.IsNullOrEmpty(courseName)) return "GEN";
        var upper = courseName.ToUpperInvariant();
        if (upper.Contains("BOTANY") || upper.Contains("BOT")) return "BOT";
        if (upper.Contains("ZOOLOGY") || upper.Contains("ZOO")) return "ZOO";
        if (upper.Contains("CHEMISTRY") || upper.Contains("CHM")) return "CHM";
        if (upper.Contains("PHYSICS") || upper.Contains("PHY")) return "PHY";
        if (upper.Contains("MICROBIOLOGY") || upper.Contains("MIC")) return "MIC";
        if (upper.Contains("FORENSIC") || upper.Contains("FOR")) return "FOR";
        if (upper.Contains("COMPUTER") || upper.Contains("CSC")) return "CSC";
        return "SCI";
    }

    public static string GetDepartment(string courseName)
    {
        var code = GetCourseCode(courseName);
        return code switch
        {
            "BOT" => "Botany",
            "ZOO" => "Zoology",
            "CHM" => "Chemistry",
            "PHY" => "Physics",
            "MIC" => "Microbiology",
            "FOR" => "Forensic Science",
            "CSC" => "Computer Science",
            _ => "Science"
        };
    }
}

public class CreateStudentInput
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Course { get; set; } = "B.Sc. Botany";
    public string Department { get; set; } = "Botany";
    public int AdmissionYear { get; set; } = DateTime.Today.Year;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = "Male";
    public string Mobile { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = "Bhavnagar";
    public string Semester { get; set; } = "Semester 1";
    public string FatherName { get; set; } = string.Empty;
    public string FatherOccupation { get; set; } = string.Empty;
    public DateTime? AdmissionDate { get; set; } = DateTime.Today;
    public string Status { get; set; } = "Active";
}

public class EditStudentInput
{
    public string StudentId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = "Male";
    public string Mobile { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Semester { get; set; } = "Semester 1";
    public string FatherName { get; set; } = string.Empty;
    public string FatherOccupation { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}

public class ChangePasswordInput
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;
using CollegeManagementSystem.Hubs;
using System.ComponentModel.DataAnnotations;

namespace CollegeManagementSystem.Pages.Admin.Teachers;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<CollegeManagementHub> _hubContext;

    public IndexModel(ApplicationDbContext context, IHubContext<CollegeManagementHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // Search and Filters
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DeptFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DesigFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DeptTab { get; set; }

    // Summary Counts
    public int TotalTeachers { get; set; }
    public int ActiveTeachers { get; set; }
    public int InactiveTeachers { get; set; }

    // Department-wise distribution count
    public Dictionary<string, int> DeptCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // List of teachers displayed in the table
    public List<Teacher> Teachers { get; set; } = new();

    // Modals state triggers
    public bool ShowAddModal { get; set; }
    public Teacher? ViewTeacher { get; set; }
    public Teacher? EditTeacher { get; set; }
    public Teacher? PasswordTeacher { get; set; }
    public Teacher? DeleteTeacher { get; set; }

    // Feedback Alerts
    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? CreatedCredentials { get; set; }

    // Input Models
    [BindProperty]
    public CreateTeacherInput NewTeacher { get; set; } = new();

    [BindProperty]
    public EditTeacherInput EditInput { get; set; } = new();

    [BindProperty]
    public ChangePasswordInput PasswordInput { get; set; } = new();

    // Standard Academic Lists
    public static readonly List<string> StandardDepartments = new()
    {
        "Botany",
        "Chemistry",
        "Computer Science",
        "Forensic Science",
        "Microbiology",
        "Physics",
        "Zoology"
    };

    public static readonly List<string> StandardDesignations = new()
    {
        "Director",
        "HOD",
        "Faculty",
        "Lab Assistant",
        "Coordinator",
        "Faculty Trainer",
        "Guest Faculty",
        "Other"
    };

    public async Task<IActionResult> OnGetAsync(
        string? search,
        string? deptFilter,
        string? desigFilter,
        string? statusFilter,
        string? deptTab,
        string? viewId,
        string? editId,
        string? passwordId,
        string? deleteId,
        bool openAdd = false)
    {
        SearchTerm = search;
        DeptFilter = deptFilter;
        DesigFilter = desigFilter;
        StatusFilter = statusFilter;
        DeptTab = deptTab;
        ShowAddModal = openAdd;

        // Synchronize DeptTab and DeptFilter if selected
        if (!string.IsNullOrEmpty(DeptTab) && DeptTab != "All")
        {
            DeptFilter = DeptTab;
        }

        // Metrics
        TotalTeachers = await _context.Teachers.CountAsync();
        ActiveTeachers = await _context.Teachers.CountAsync(t => t.Status.ToLower() == "active");
        InactiveTeachers = await _context.Teachers.CountAsync(t => t.Status.ToLower() == "inactive");

        // Department counts
        var allTeachers = await _context.Teachers.ToListAsync();
        foreach (var dept in StandardDepartments)
        {
            DeptCounts[dept] = allTeachers.Count(t => t.Department.Equals(dept, StringComparison.OrdinalIgnoreCase));
        }

        var query = _context.Teachers.AsQueryable();

        // 1. Filter by Search Query
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLower();
            query = query.Where(t =>
                t.FullName.ToLower().Contains(term) ||
                t.TeacherId.ToLower().Contains(term) ||
                t.Department.ToLower().Contains(term) ||
                t.Designation.ToLower().Contains(term) ||
                t.Email.ToLower().Contains(term) ||
                t.Mobile.ToLower().Contains(term) ||
                t.Qualification.ToLower().Contains(term) ||
                t.Specialization.ToLower().Contains(term));
        }

        // 2. Filter by Department
        if (!string.IsNullOrWhiteSpace(DeptFilter) && DeptFilter != "All")
        {
            query = query.Where(t => t.Department.ToLower() == DeptFilter.ToLower());
        }

        // 3. Filter by Designation
        if (!string.IsNullOrWhiteSpace(DesigFilter) && DesigFilter != "All")
        {
            query = query.Where(t => t.Designation.ToLower() == DesigFilter.ToLower());
        }

        // 4. Filter by Status
        if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
        {
            query = query.Where(t => t.Status.ToLower() == StatusFilter.ToLower());
        }

        // Teacher list must always be sorted A-Z by Teacher Name with natural serial ordering fallback
        Teachers = await query
            .OrderBy(t => t.FullName)
            .ThenBy(t => t.TeacherSerialNumber)
            .ToListAsync();

        // Handle View modal
        if (!string.IsNullOrEmpty(viewId))
        {
            ViewTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == viewId);
        }

        // Handle Edit modal
        if (!string.IsNullOrEmpty(editId))
        {
            EditTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == editId);
            if (EditTeacher != null)
            {
                EditInput = new EditTeacherInput
                {
                    TeacherId = EditTeacher.TeacherId,
                    FullName = EditTeacher.FullName,
                    Email = EditTeacher.Email,
                    Department = EditTeacher.Department,
                    Designation = EditTeacher.Designation,
                    Qualification = EditTeacher.Qualification,
                    Specialization = EditTeacher.Specialization,
                    Mobile = EditTeacher.Mobile,
                    DateOfBirth = EditTeacher.DateOfBirth,
                    Gender = EditTeacher.Gender,
                    Address = EditTeacher.Address,
                    City = EditTeacher.City,
                    JoiningDate = EditTeacher.JoiningDate,
                    Status = EditTeacher.Status
                };
            }
        }

        // Handle Password modal
        if (!string.IsNullOrEmpty(passwordId))
        {
            PasswordTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == passwordId);
            if (PasswordTeacher != null)
            {
                PasswordInput.TeacherId = PasswordTeacher.TeacherId;
                PasswordInput.TeacherName = PasswordTeacher.FullName;
            }
        }

        // Handle Delete modal
        if (!string.IsNullOrEmpty(deleteId))
        {
            DeleteTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == deleteId);
        }

        return Page();
    }

    // 1. ADD NEW TEACHER
    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTeacher.FullName))
        {
            ModelState.AddModelError("NewTeacher.FullName", "Full Name is required.");
        }
        if (string.IsNullOrWhiteSpace(NewTeacher.Email))
        {
            ModelState.AddModelError("NewTeacher.Email", "Email Address is required.");
        }
        if (string.IsNullOrWhiteSpace(NewTeacher.Department))
        {
            ModelState.AddModelError("NewTeacher.Department", "Please select an Academic Department.");
        }

        if (!ModelState.IsValid)
        {
            ShowAddModal = true;
            return await OnGetAsync(SearchTerm, DeptFilter, DesigFilter, StatusFilter, DeptTab, null, null, null, null, true);
        }

        // Auto-generate Teacher ID: SSVT + SERIAL (e.g. SSVT001, SSVT002, SSVT010)
        var existingSerials = await _context.Teachers
            .Select(t => t.TeacherSerialNumber)
            .ToListAsync();

        int nextSerial = existingSerials.Any() ? existingSerials.Max() + 1 : 1;
        string serialStr = nextSerial.ToString("D3");
        string teacherId = $"SSVT{serialStr}";

        // Auto-generate initial password: Teacher Name + # + Teacher Serial (e.g. Rahul Patel#001)
        string trimmedFullName = NewTeacher.FullName.Trim();
        string initialPlainPassword = $"{trimmedFullName}#{serialStr}";
        string hashedPassword = PasswordHelper.HashPassword(initialPlainPassword);

        var teacher = new Teacher
        {
            TeacherId = teacherId,
            TeacherSerialNumber = nextSerial,
            FullName = trimmedFullName,
            Email = NewTeacher.Email.Trim(),
            Password = hashedPassword,
            Department = NewTeacher.Department,
            Designation = string.IsNullOrWhiteSpace(NewTeacher.Designation) ? "Faculty" : NewTeacher.Designation,
            Qualification = NewTeacher.Qualification?.Trim() ?? string.Empty,
            Specialization = NewTeacher.Specialization?.Trim() ?? string.Empty,
            Mobile = NewTeacher.Mobile?.Trim() ?? string.Empty,
            DateOfBirth = NewTeacher.DateOfBirth,
            Gender = string.IsNullOrWhiteSpace(NewTeacher.Gender) ? "Male" : NewTeacher.Gender,
            Address = NewTeacher.Address?.Trim() ?? string.Empty,
            City = string.IsNullOrWhiteSpace(NewTeacher.City) ? "Bhavnagar" : NewTeacher.City.Trim(),
            JoiningDate = NewTeacher.JoiningDate ?? DateTime.Today,
            PhotoPath = "/images/teachers.jpg",
            Status = string.IsNullOrWhiteSpace(NewTeacher.Status) ? "Active" : NewTeacher.Status
        };

        _context.Teachers.Add(teacher);
        await _context.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("TeacherCreated", new { teacherId = teacher.TeacherId, fullName = teacher.FullName, department = teacher.Department });
            await _hubContext.Clients.All.SendAsync("TeacherUpdated", new { teacherId = teacher.TeacherId, fullName = teacher.FullName });
        }
        catch { }

        StatusMessage = $"Faculty member '{teacher.FullName}' successfully registered with Teacher ID: {teacher.TeacherId}.";
        CreatedCredentials = $"Teacher ID: {teacher.TeacherId} | Initial Password: {initialPlainPassword}";

        return RedirectToPage("/Admin/Teachers/Index");
    }

    // 2. EDIT TEACHER
    public async Task<IActionResult> OnPostEditAsync()
    {
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == EditInput.TeacherId);
        if (teacher == null)
        {
            StatusMessage = "Error: Teacher record not found.";
            return RedirectToPage("/Admin/Teachers/Index");
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
            return await OnGetAsync(SearchTerm, DeptFilter, DesigFilter, StatusFilter, DeptTab, null, EditInput.TeacherId, null, null, false);
        }

        // Update details (Teacher ID remains completely unchangeable)
        teacher.FullName = EditInput.FullName.Trim();
        teacher.Email = EditInput.Email.Trim();
        teacher.Department = EditInput.Department;
        teacher.Designation = EditInput.Designation;
        teacher.Qualification = EditInput.Qualification?.Trim() ?? string.Empty;
        teacher.Specialization = EditInput.Specialization?.Trim() ?? string.Empty;
        teacher.Mobile = EditInput.Mobile?.Trim() ?? string.Empty;
        teacher.DateOfBirth = EditInput.DateOfBirth;
        teacher.Gender = EditInput.Gender;
        teacher.Address = EditInput.Address?.Trim() ?? string.Empty;
        teacher.City = string.IsNullOrWhiteSpace(EditInput.City) ? "Bhavnagar" : EditInput.City.Trim();
        teacher.JoiningDate = EditInput.JoiningDate;
        teacher.Status = EditInput.Status;

        await _context.SaveChangesAsync();

        try
        {
            await _hubContext.Clients.All.SendAsync("TeacherUpdated", new { teacherId = teacher.TeacherId, fullName = teacher.FullName, department = teacher.Department });
        }
        catch { }

        StatusMessage = $"Teacher '{teacher.FullName}' ({teacher.TeacherId}) updated successfully.";
        return RedirectToPage("/Admin/Teachers/Index");
    }

    // 3. CHANGE PASSWORD
    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == PasswordInput.TeacherId);
        if (teacher == null)
        {
            StatusMessage = "Error: Teacher record not found.";
            return RedirectToPage("/Admin/Teachers/Index");
        }

        if (string.IsNullOrWhiteSpace(PasswordInput.NewPassword) || PasswordInput.NewPassword.Length < 4)
        {
            StatusMessage = "Error: Password must be at least 4 characters long.";
            return RedirectToPage("/Admin/Teachers/Index", new { passwordId = PasswordInput.TeacherId });
        }

        if (PasswordInput.NewPassword != PasswordInput.ConfirmPassword)
        {
            StatusMessage = "Error: New password and confirmation password do not match.";
            return RedirectToPage("/Admin/Teachers/Index", new { passwordId = PasswordInput.TeacherId });
        }

        teacher.Password = PasswordHelper.HashPassword(PasswordInput.NewPassword);
        await _context.SaveChangesAsync();

        StatusMessage = $"Password for teacher '{teacher.FullName}' ({teacher.TeacherId}) updated and securely hashed.";
        return RedirectToPage("/Admin/Teachers/Index");
    }

    // 4. ACTIVATE / DEACTIVATE TOGGLE
    public async Task<IActionResult> OnPostToggleStatusAsync(string id)
    {
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == id);
        if (teacher != null)
        {
            bool wasActive = teacher.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            teacher.Status = wasActive ? "Inactive" : "Active";
            await _context.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("TeacherUpdated", new { teacherId = teacher.TeacherId, status = teacher.Status });
            }
            catch { }

            StatusMessage = $"Teacher '{teacher.FullName}' ({teacher.TeacherId}) is now {(teacher.Status == "Active" ? "Active" : "Inactive")}.";
        }

        return RedirectToPage("/Admin/Teachers/Index", new
        {
            search = SearchTerm,
            deptFilter = DeptFilter,
            desigFilter = DesigFilter,
            statusFilter = StatusFilter,
            deptTab = DeptTab
        });
    }

    // 5. DELETE TEACHER (CONFIRMED)
    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.TeacherId == id);
        if (teacher != null)
        {
            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            try
            {
                await _hubContext.Clients.All.SendAsync("TeacherDeleted", new { teacherId = id });
                await _hubContext.Clients.All.SendAsync("TeacherUpdated", new { teacherId = id });
            }
            catch { }

            StatusMessage = $"Teacher '{teacher.FullName}' ({teacher.TeacherId}) has been permanently deleted.";
        }

        return RedirectToPage("/Admin/Teachers/Index");
    }
}

// Input Models
public class CreateTeacherInput
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = "Botany";
    public string Designation { get; set; } = "Faculty";
    public string Qualification { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = "Male";
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = "Bhavnagar";
    public DateTime? JoiningDate { get; set; } = DateTime.Today;
    public string Status { get; set; } = "Active";
}

public class EditTeacherInput
{
    public string TeacherId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = "Botany";
    public string Designation { get; set; } = "Faculty";
    public string Qualification { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = "Male";
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = "Bhavnagar";
    public DateTime? JoiningDate { get; set; }
    public string Status { get; set; } = "Active";
}

public class ChangePasswordInput
{
    public string TeacherId { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

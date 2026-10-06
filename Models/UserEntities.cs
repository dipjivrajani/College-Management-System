using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace CollegeManagementSystem.Models;

public class Admin
{
    [Key]
    public int AdminId { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = "Admin#123";

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [StringLength(100)]
    public string FullName { get; set; } = "System Administrator";

    [StringLength(100)]
    public string Email { get; set; } = "admin@ssv.edu.in";

    public string Role { get; set; } = "Admin";
}

public class Teacher
{
    [Key]
    [StringLength(20)]
    public string TeacherId { get; set; } = string.Empty; // e.g. SSVT001

    public int TeacherSerialNumber { get; set; } = 1;

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    public string Department { get; set; } = string.Empty;

    public string Designation { get; set; } = "Faculty"; // Director, HOD, Faculty, Lab Assistant, Coordinator, Faculty Trainer, Guest Faculty, Other

    public string Qualification { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; } = "Male";

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = "Bhavnagar";

    [DataType(DataType.Date)]
    public DateTime? JoiningDate { get; set; } = DateTime.Today;

    public string PhotoPath { get; set; } = "/images/teachers.jpg";

    public string Status { get; set; } = "Active"; // Active, Inactive
}

public static class PasswordHelper
{
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("SSV_Institute_Of_Science_Salt_2026!");

    public static string HashPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword)) return string.Empty;
        using var hmac = new HMACSHA256(Salt);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(plainPassword.Trim()));
        return Convert.ToHexString(hashBytes);
    }

    public static bool VerifyPassword(string enteredPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(enteredPassword) || string.IsNullOrEmpty(storedHash)) return false;
        if (enteredPassword == storedHash) return true; // support unhashed legacy/demo passwords
        var hashOfEntered = HashPassword(enteredPassword);
        return string.Equals(hashOfEntered, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}

public class Student
{
    [Key]
    [StringLength(30)]
    public string StudentId { get; set; } = string.Empty; // e.g. 26SSVBOT001

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course is required.")]
    public string Course { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public int AdmissionYear { get; set; } = DateTime.Now.Year;

    public int StudentSerialNumber { get; set; } = 1;

    public int ValidUntil { get; set; } = DateTime.Now.Year + 4; // 5-year validity (e.g. 2026 through 2030)

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; } = "Male";

    public string Mobile { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = "Bhavnagar";

    public string Semester { get; set; } = "Semester 1";

    public string FatherName { get; set; } = string.Empty;

    public string FatherOccupation { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime AdmissionDate { get; set; } = DateTime.Today;

    public string PhotoPath { get; set; } = string.Empty;

    public string Status { get; set; } = "Active"; // Active, Inactive
}

public class AcademicYear
{
    [Key]
    public int AcademicYearId { get; set; }

    [Required]
    [StringLength(20)]
    public string YearCode { get; set; } = "2025-26"; // 2024-25, 2025-26, 2026-27

    [Required]
    [StringLength(50)]
    public string YearName { get; set; } = "Academic Year 2025-2026";

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 1;
}

public class Subject
{
    [Key]
    public int SubjectId { get; set; }

    [StringLength(20)]
    public string AcademicYear { get; set; } = "2025-26"; // Enables full syllabus versioning per year

    [StringLength(30)]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject Name is required.")]
    [StringLength(150)]
    public string SubjectName { get; set; } = string.Empty;

    public int? CourseId { get; set; }

    [Required(ErrorMessage = "Course is required.")]
    [StringLength(100)]
    public string Course { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Semester is required.")]
    [StringLength(50)]
    public string Semester { get; set; } = "Semester 1";

    public decimal Credits { get; set; } = 4.0m;

    public int DisplayOrder { get; set; } = 1; // Admin subject order control

    public string Status { get; set; } = "Active"; // Active, Inactive, Archived

    [NotMapped]
    public List<SubjectComponent> Components { get; set; } = new();
}

public class Attendance
{
    [Key]
    public int AttendanceId { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public string Course { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public string Semester { get; set; } = "Semester 1";

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    public string Status { get; set; } = "Present"; // Present, Absent
}

public class Marks
{
    [Key]
    public int MarkId { get; set; }

    [NotMapped]
    public int MarksId { get => MarkId; set => MarkId = value; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public int? SubjectId { get; set; }

    [Required(ErrorMessage = "Subject Name is required.")]
    public string SubjectName { get; set; } = string.Empty;

    public string Course { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    [StringLength(20)]
    public string AcademicYear { get; set; } = "2025-26";

    public string Semester { get; set; } = "Semester 1";

    public int Attempt { get; set; } = 1; // Attempt 1, Attempt 2 for backlogs/cleared

    public string ComponentType { get; set; } = "Internal"; // Internal, Theory, Practical, Project, Viva

    public string ExamType { get; set; } = "Internal"; // Internal Exam, Theory, Practical, Project, Viva, Other

    public decimal MaxMarks { get; set; } = 30;

    [NotMapped]
    public decimal MaximumMarks { get => MaxMarks; set => MaxMarks = value; }

    public decimal ObtainedMarks { get; set; }

    [NotMapped]
    public decimal MarksObtained { get => ObtainedMarks; set => ObtainedMarks = value; }

    [DataType(DataType.Date)]
    public DateTime? ExamDate { get; set; } = DateTime.Today;
}

public class ResultAttempt
{
    [Key]
    public int ResultAttemptId { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [StringLength(20)]
    public string AcademicYear { get; set; } = "2025-26";

    [Required]
    [StringLength(50)]
    public string Semester { get; set; } = "Semester 1";

    public int Attempt { get; set; } = 1;

    public decimal TotalObtainedMarks { get; set; }

    public decimal TotalMaxMarks { get; set; }

    public decimal Percentage { get; set; }

    public decimal? SGPA { get; set; }

    [StringLength(20)]
    public string ResultStatus { get; set; } = "INCOMPLETE"; // PASS, FAIL, BACKLOG, ABSENT, INCOMPLETE

    [DataType(DataType.Date)]
    public DateTime? ExamDate { get; set; } = DateTime.Today;

    public string? Remarks { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace CollegeManagementSystem.Models;

public class Admin
{
    [Key]
    public int AdminId { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = "admin";

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = "admin123";

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

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = "Teacher@123";

    public string Department { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string Status { get; set; } = "ACTIVE";
}

public class Student
{
    [Key]
    [StringLength(30)]
    public string StudentId { get; set; } = string.Empty; // e.g. 26SSVCHM001

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = "Student@123";

    public string Course { get; set; } = string.Empty;

    public int AdmissionYear { get; set; } = DateTime.Now.Year;

    public int StudentSerialNumber { get; set; } = 1;

    public string Status { get; set; } = "ACTIVE";
}

public class Attendance
{
    [Key]
    public int AttendanceId { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public string Course { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Today;

    public string Status { get; set; } = "Present"; // Present, Absent
}

public class Marks
{
    [Key]
    public int MarkId { get; set; }

    public string StudentId { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public decimal MaxMarks { get; set; } = 100;

    public decimal ObtainedMarks { get; set; }

    public string Semester { get; set; } = "Semester 1";
}

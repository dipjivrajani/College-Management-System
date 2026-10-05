using System.ComponentModel.DataAnnotations;

namespace CollegeManagementSystem.Models;

public class Registration
{
    [Key]
    [StringLength(30)]
    public string RegistrationId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Full Name must be between 3 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of Birth is required.")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-18);

    [Required(ErrorMessage = "Gender is required.")]
    [StringLength(20)]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile Number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit Indian mobile number.")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [StringLength(60)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "School Name is required.")]
    [StringLength(150)]
    public string SchoolName { get; set; } = string.Empty;

    [Required(ErrorMessage = "School City is required.")]
    [StringLength(60)]
    public string SchoolCity { get; set; } = string.Empty;

    [Required(ErrorMessage = "SSC Marks (%) are required.")]
    [Range(35.0, 100.0, ErrorMessage = "SSC Marks must be between 35% and 100%.")]
    public decimal SSCMarks { get; set; }

    [Required(ErrorMessage = "HSC Marks (%) are required.")]
    [Range(35.0, 100.0, ErrorMessage = "HSC Marks must be between 35% and 100%.")]
    public decimal HSCMarks { get; set; }

    [StringLength(200)]
    public string? InterestedSubjects { get; set; }

    [Required(ErrorMessage = "Please select a Course.")]
    [StringLength(100)]
    public string SelectedCourse { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Specialization { get; set; }

    [Required(ErrorMessage = "Father's Name is required.")]
    [StringLength(100)]
    public string FatherName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Father's Occupation is required.")]
    [StringLength(100)]
    public string FatherOccupation { get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    [StringLength(30)]
    public string Status { get; set; } = "NEW"; // NEW, UNDER REVIEW, APPROVED, REJECTED
}

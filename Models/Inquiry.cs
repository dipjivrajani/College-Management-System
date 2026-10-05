using System.ComponentModel.DataAnnotations;

namespace CollegeManagementSystem.Models;

public class Inquiry
{
    [Key]
    public int InquiryId { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit Indian mobile number.")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select who you are.")]
    [StringLength(50)]
    public string InquiryType { get; set; } = "General Inquiry"; // Student, Teacher, Parent, General Inquiry

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, MinimumLength = 4, ErrorMessage = "Subject must be between 4 and 150 characters.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(50)]
    public string? StudentId { get; set; }

    [StringLength(50)]
    public string? TeacherId { get; set; }

    [Required(ErrorMessage = "Description / Message is required.")]
    [StringLength(1500, MinimumLength = 10, ErrorMessage = "Message must be at least 10 characters long.")]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(30)]
    public string Status { get; set; } = "NEW"; // NEW, IN REVIEW, RESOLVED
}

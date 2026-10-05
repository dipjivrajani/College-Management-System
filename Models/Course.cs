using System.ComponentModel.DataAnnotations;

namespace CollegeManagementSystem.Models;

public class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required]
    [StringLength(20)]
    public string CourseCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CourseName { get; set; } = string.Empty;

    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string Duration { get; set; } = "3 Years (6 Semesters)";

    public string Fees { get; set; } = "Details will be updated by the institute.";

    public string Faculty { get; set; } = "Details will be updated by the institute.";

    public string Certification { get; set; } = "Details will be updated by the institute.";

    public string PracticalKnowledge { get; set; } = "Structured laboratory demonstrations, experimental validation, observational record keeping, and scientific analytical techniques.";

    public string Eligibility { get; set; } = "Higher Secondary Certificate (10+2) in Science stream or equivalent recognized examination.";

    public string ImagePath { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

using System.ComponentModel.DataAnnotations;

namespace CollegeManagementSystem.Models;

public class SubjectComponent
{
    [Key]
    public int SubjectComponentId { get; set; }

    public int SubjectId { get; set; }

    [Required]
    [StringLength(50)]
    public string ComponentType { get; set; } = "Internal"; // Internal, Theory, Practical, Project, Viva

    public decimal MaximumMarks { get; set; } = 30; // Configurable per component

    public decimal PassingMarks { get; set; } = 12; // Configurable passing marks

    public bool IsRequired { get; set; } = true;
}

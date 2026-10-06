using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CollegeManagementSystem.Data;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Pages.Admin.Courses;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<CourseItemDto> CoursesList { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class CourseItemDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Duration { get; set; } = "3 Years (6 Semesters)";
        public int NumberOfSemesters { get; set; } = 6;
        public string Description { get; set; } = string.Empty;
        public string PracticalKnowledge { get; set; } = string.Empty;
        public string Eligibility { get; set; } = string.Empty;
        public string Fees { get; set; } = "Details will be updated by the institute.";
        public string Faculty { get; set; } = "Details will be updated by the institute.";
        public bool IsActive { get; set; } = true;
        public int TotalSubjectsCount { get; set; }
    }

    public async Task OnGetAsync()
    {
        var rawCourses = await _context.Courses
            .OrderBy(c => c.CourseName)
            .ToListAsync();

        var subjectCounts = await _context.Subjects
            .GroupBy(s => s.Course)
            .Select(g => new { Course = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Course, x => x.Count);

        CoursesList = rawCourses.Select(c =>
        {
            var dept = Students.IndexModel.GetDepartment(c.CourseName);
            var subCount = subjectCounts.ContainsKey(c.CourseName) ? subjectCounts[c.CourseName] : 0;

            return new CourseItemDto
            {
                CourseId = c.CourseId,
                CourseCode = c.CourseCode,
                CourseName = c.CourseName,
                Department = dept,
                Category = c.Category,
                Duration = !string.IsNullOrEmpty(c.Duration) ? c.Duration : "3 Years (6 Semesters)",
                NumberOfSemesters = 6,
                Description = c.Description,
                PracticalKnowledge = c.PracticalKnowledge,
                Eligibility = c.Eligibility,
                Fees = !string.IsNullOrWhiteSpace(c.Fees) ? c.Fees : "Details will be updated by the institute.",
                Faculty = !string.IsNullOrWhiteSpace(c.Faculty) ? c.Faculty : "Details will be updated by the institute.",
                IsActive = c.IsActive,
                TotalSubjectsCount = subCount
            };
        }).ToList();
    }

    public async Task<IActionResult> OnPostAddCourseAsync(
        string courseCode,
        string courseName,
        string category,
        string description)
    {
        if (string.IsNullOrWhiteSpace(courseCode) || string.IsNullOrWhiteSpace(courseName))
        {
            ErrorMessage = "Course Code and Course Name are required.";
            return RedirectToPage();
        }

        var trimmedCode = courseCode.Trim().ToUpper();
        var trimmedName = courseName.Trim();

        bool exists = await _context.Courses.AnyAsync(c => c.CourseCode == trimmedCode || c.CourseName.ToLower() == trimmedName.ToLower());
        if (exists)
        {
            ErrorMessage = $"A course with Code '{trimmedCode}' or Title '{trimmedName}' already exists.";
            return RedirectToPage();
        }

        var newCourse = new Course
        {
            CourseCode = trimmedCode,
            CourseName = trimmedName,
            Category = !string.IsNullOrWhiteSpace(category) ? category.Trim() : "Science Disciplines",
            Description = !string.IsNullOrWhiteSpace(description) ? description.Trim() : "Undergraduate science academic curriculum.",
            Duration = "3 Years (6 Semesters)",
            Fees = "Details will be updated by the institute.",
            Faculty = "Details will be updated by the institute.",
            Certification = "Details will be updated by the institute.",
            PracticalKnowledge = "Structured laboratory demonstrations, experimental validation, observational record keeping, and scientific analytical techniques.",
            Eligibility = "Higher Secondary Certificate (10+2) in Science stream or equivalent recognized examination.",
            IsActive = true
        };

        _context.Courses.Add(newCourse);
        await _context.SaveChangesAsync();

        SuccessMessage = $"Course '{trimmedName}' ({trimmedCode}) added successfully!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditCourseAsync(
        int editCourseId,
        string editDescription,
        string editDuration,
        bool editIsActive)
    {
        var course = await _context.Courses.FindAsync(editCourseId);
        if (course == null)
        {
            ErrorMessage = "Course not found.";
            return RedirectToPage();
        }

        course.Description = !string.IsNullOrWhiteSpace(editDescription) ? editDescription.Trim() : course.Description;
        course.Duration = !string.IsNullOrWhiteSpace(editDuration) ? editDuration.Trim() : course.Duration;
        course.IsActive = editIsActive;

        await _context.SaveChangesAsync();
        SuccessMessage = $"Course '{course.CourseName}' updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course != null)
        {
            course.IsActive = !course.IsActive;
            await _context.SaveChangesAsync();
            SuccessMessage = $"Course '{course.CourseName}' status set to {(course.IsActive ? "Active" : "Inactive")}.";
        }
        return RedirectToPage();
    }
}

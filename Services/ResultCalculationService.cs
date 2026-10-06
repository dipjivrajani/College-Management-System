using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Services;

public class ComponentResultDto
{
    public string ComponentType { get; set; } = "Internal"; // Internal, Theory, Practical, Project, Viva
    public string ComponentName { get; set; } = "Internal";
    public decimal ObtainedMarks { get; set; }
    public decimal MaximumMarks { get; set; }
    public decimal PassingMarks { get; set; }
    public DateTime? ExamDate { get; set; }
    public bool IsPass => MaximumMarks > 0 && ObtainedMarks >= PassingMarks;
}

public class SubjectResultDto
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public decimal Credits { get; set; } = 4.0m;
    public int DisplayOrder { get; set; } = 1;

    // Dynamic list of configured components for this subject
    public List<ComponentResultDto> ConfiguredComponents { get; set; } = new();

    // Component-wise obtained marks
    public decimal? InternalObtained { get; set; }
    public decimal? InternalMax { get; set; }
    public DateTime? InternalExamDate { get; set; }

    public decimal? TheoryObtained { get; set; }
    public decimal? TheoryMax { get; set; }
    public DateTime? TheoryExamDate { get; set; }

    public decimal? PracticalObtained { get; set; }
    public decimal? PracticalMax { get; set; }
    public DateTime? PracticalExamDate { get; set; }

    public decimal? ProjectObtained { get; set; }
    public decimal? ProjectMax { get; set; }
    public DateTime? ProjectExamDate { get; set; }

    public decimal? VivaObtained { get; set; }
    public decimal? VivaMax { get; set; }
    public DateTime? VivaExamDate { get; set; }

    public decimal TotalObtained { get; set; }
    public decimal TotalMax { get; set; }
    public decimal Percentage => TotalMax > 0 ? Math.Round(TotalObtained / TotalMax * 100, 2) : 0;

    public string Grade { get; set; } = "--";
    public decimal GradePoint { get; set; } = 0;
    public bool IsPass { get; set; } = false;
    public string ResultStatus => IsPass ? "PASS" : (TotalMax > 0 ? "FAIL" : "--");
}

public class SemesterResultDto
{
    public string Semester { get; set; } = string.Empty;
    public int Attempt { get; set; } = 1;
    public List<SubjectResultDto> Subjects { get; set; } = new();

    public decimal TotalCredits => Subjects.Sum(s => s.Credits);
    public decimal TotalMaxMarks => Subjects.Sum(s => s.TotalMax);
    public decimal TotalObtainedMarks => Subjects.Sum(s => s.TotalObtained);
    public decimal Percentage => TotalMaxMarks > 0 ? Math.Round(TotalObtainedMarks / TotalMaxMarks * 100, 2) : 0;

    public decimal? SGPA { get; set; }
    public bool IsCompleted => Subjects.Count > 0 && Subjects.All(s => s.TotalMax > 0 && s.ConfiguredComponents.Count > 0 && s.ConfiguredComponents.All(c => c.ObtainedMarks >= 0));
    public bool HasAnyMarks => Subjects.Any(s => s.TotalObtained > 0 || (s.ConfiguredComponents.Count > 0 && s.TotalMax > 0));
    
    public string ResultStatus
    {
        get
        {
            if (Subjects.Count == 0 || !HasAnyMarks) return "INCOMPLETE";
            if (!IsCompleted) return "INCOMPLETE";
            return Subjects.All(s => s.IsPass) ? "PASS" : "FAIL";
        }
    }
}

public class OverallResultDto
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int AdmissionYear { get; set; }
    public string AcademicYear { get; set; } = "2025-26";
    public string CurrentSemester { get; set; } = "Semester 1";

    public Dictionary<string, SemesterResultDto> SemesterResults { get; set; } = new();
    public decimal? CGPA { get; set; }
    public int CompletedSemestersCount { get; set; } = 0;
    public string OverallResult { get; set; } = "--";
}

public class ResultCalculationService
{
    // Centralized grade and grade point scale
    public static (string Grade, decimal GradePoint) CalculateGrade(decimal percentage)
    {
        if (percentage >= 85.0m) return ("O", 10.0m);
        if (percentage >= 75.0m) return ("A+", 9.0m);
        if (percentage >= 65.0m) return ("A", 8.0m);
        if (percentage >= 55.0m) return ("B+", 7.0m);
        if (percentage >= 50.0m) return ("B", 6.0m);
        if (percentage >= 45.0m) return ("C", 5.0m);
        if (percentage >= 40.0m) return ("P", 4.0m);
        return ("F", 0.0m);
    }

    // Determine pass/fail based on 40% minimum overall and component-wise passing
    public static bool IsSubjectPassed(decimal totalObtained, decimal totalMax, List<SubjectComponent> components, List<Marks> subMarks)
    {
        if (totalMax <= 0) return false;
        var percentage = (totalObtained / totalMax) * 100;
        if (percentage < 40.0m) return false;

        // Check each configured component against its passing threshold
        foreach (var comp in components)
        {
            var m = subMarks.FirstOrDefault(x => x.ComponentType.Equals(comp.ComponentType, StringComparison.OrdinalIgnoreCase)
                                              || x.ExamType.Equals(comp.ComponentType, StringComparison.OrdinalIgnoreCase));
            if (m != null && comp.MaximumMarks > 0)
            {
                decimal passThreshold = comp.PassingMarks > 0 ? comp.PassingMarks : (comp.MaximumMarks * 0.35m);
                if (m.ObtainedMarks < passThreshold) return false;
            }
        }
        return true;
    }

    // Build complete semester result
    public static SemesterResultDto CalculateSemesterResult(
        string semester,
        List<Subject> subjects,
        List<SubjectComponent> allComponents,
        List<Marks> marksForSemester,
        int attempt = 1)
    {
        var result = new SemesterResultDto { Semester = semester, Attempt = attempt };

        // Order subjects by Admin-defined DisplayOrder then SubjectName (PART 13)
        var orderedSubjects = subjects.OrderBy(s => s.DisplayOrder).ThenBy(s => s.SubjectName).ToList();

        foreach (var sub in orderedSubjects)
        {
            var subComps = allComponents.Where(c => c.SubjectId == sub.SubjectId).ToList();
            var subMarks = marksForSemester.Where(m => m.SubjectId == sub.SubjectId || m.SubjectName == sub.SubjectName).ToList();

            var dto = new SubjectResultDto
            {
                SubjectId = sub.SubjectId,
                SubjectCode = !string.IsNullOrEmpty(sub.SubjectCode) ? sub.SubjectCode : $"SCI{sub.SubjectId}",
                SubjectName = sub.SubjectName,
                Credits = sub.Credits > 0 ? sub.Credits : 4.0m,
                DisplayOrder = sub.DisplayOrder
            };

            decimal totObt = 0;
            decimal totMax = 0;

            foreach (var comp in subComps)
            {
                var m = subMarks.FirstOrDefault(x => x.ComponentType.Equals(comp.ComponentType, StringComparison.OrdinalIgnoreCase)
                                                  || x.ExamType.Equals(comp.ComponentType, StringComparison.OrdinalIgnoreCase));

                decimal obt = m?.ObtainedMarks ?? 0;
                decimal max = comp.MaximumMarks;
                decimal passMarks = comp.PassingMarks > 0 ? comp.PassingMarks : Math.Round(max * 0.35m, 1);
                DateTime? date = m?.ExamDate;

                totObt += obt;
                totMax += max;

                dto.ConfiguredComponents.Add(new ComponentResultDto
                {
                    ComponentType = comp.ComponentType,
                    ComponentName = comp.ComponentType,
                    ObtainedMarks = obt,
                    MaximumMarks = max,
                    PassingMarks = passMarks,
                    ExamDate = date
                });

                switch (comp.ComponentType.ToLower())
                {
                    case "internal":
                        dto.InternalObtained = obt;
                        dto.InternalMax = max;
                        dto.InternalExamDate = date;
                        break;
                    case "theory":
                        dto.TheoryObtained = obt;
                        dto.TheoryMax = max;
                        dto.TheoryExamDate = date;
                        break;
                    case "practical":
                        dto.PracticalObtained = obt;
                        dto.PracticalMax = max;
                        dto.PracticalExamDate = date;
                        break;
                    case "project":
                        dto.ProjectObtained = obt;
                        dto.ProjectMax = max;
                        dto.ProjectExamDate = date;
                        break;
                    case "viva":
                        dto.VivaObtained = obt;
                        dto.VivaMax = max;
                        dto.VivaExamDate = date;
                        break;
                }
            }

            dto.TotalObtained = totObt;
            dto.TotalMax = totMax;

            if (totMax > 0 && subMarks.Count > 0)
            {
                var gradeInfo = CalculateGrade(dto.Percentage);
                dto.Grade = gradeInfo.Grade;
                dto.GradePoint = gradeInfo.GradePoint;
                dto.IsPass = IsSubjectPassed(totObt, totMax, subComps, subMarks);
            }

            result.Subjects.Add(dto);
        }

        // Calculate SGPA: Σ(Credit * GradePoint) / Σ(Credits)
        if (result.IsCompleted && result.TotalCredits > 0)
        {
            decimal weightedPoints = result.Subjects.Sum(s => s.Credits * s.GradePoint);
            result.SGPA = Math.Round(weightedPoints / result.TotalCredits, 2);
        }

        return result;
    }

    // Build overall academic result across all 6 semesters
    public static OverallResultDto CalculateOverallResult(
        Student student,
        List<Subject> allSubjectsForCourse,
        List<SubjectComponent> allComponents,
        List<Marks> allMarksForStudent)
    {
        var overall = new OverallResultDto
        {
            StudentId = student.StudentId,
            StudentName = student.FullName,
            Course = student.Course,
            Department = student.Department,
            AdmissionYear = student.AdmissionYear,
            CurrentSemester = student.Semester
        };

        var semesters = new[] { "Semester 1", "Semester 2", "Semester 3", "Semester 4", "Semester 5", "Semester 6" };

        decimal totalCreditsWeighted = 0;
        decimal totalCredits = 0;
        int completedCount = 0;
        bool anyFailed = false;

        foreach (var sem in semesters)
        {
            var semSubs = allSubjectsForCourse.Where(s => s.Semester == sem).ToList();
            var semMarks = allMarksForStudent.Where(m => m.Semester == sem).ToList();

            var semResult = CalculateSemesterResult(sem, semSubs, allComponents, semMarks);
            overall.SemesterResults[sem] = semResult;

            if (semResult.IsCompleted && semResult.SGPA.HasValue)
            {
                completedCount++;
                totalCreditsWeighted += (semResult.SGPA.Value * semResult.TotalCredits);
                totalCredits += semResult.TotalCredits;

                if (semResult.ResultStatus == "FAIL")
                {
                    anyFailed = true;
                }
            }
        }

        overall.CompletedSemestersCount = completedCount;

        // PART 21: Calculate CGPA ONLY from completed semester results!
        // Do not treat incomplete semesters as zero!
        if (totalCredits > 0 && completedCount > 0)
        {
            overall.CGPA = Math.Round(totalCreditsWeighted / totalCredits, 2);
            overall.OverallResult = anyFailed ? "FAIL" : "PASS";
        }
        else
        {
            overall.OverallResult = "INCOMPLETE";
        }

        return overall;
    }
}

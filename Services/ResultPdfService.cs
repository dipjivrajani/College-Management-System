using System.Text;
using CollegeManagementSystem.Models;

namespace CollegeManagementSystem.Services;

public class ResultPdfService
{
    public static byte[] GenerateSemesterResultPdf(
        Student student,
        SemesterResultDto semesterResult,
        OverallResultDto overallResult)
    {
        // 1. Header & Catalog
        // Page dimensions: A4 portrait (595.28 x 841.89 points)
        float pageWidth = 595.28f;
        float pageHeight = 841.89f;

        var contentStream = new StringBuilder();

        // Helper functions for PDF drawing
        void DrawRect(float x, float y, float w, float h, float r, float g, float b, bool fill = false, bool stroke = true)
        {
            if (fill)
            {
                contentStream.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} rg\n", r, g, b);
                contentStream.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} {3:F2} re f\n", x, y, w, h);
            }
            if (stroke)
            {
                contentStream.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} RG\n", 0.1f, 0.2f, 0.3f);
                contentStream.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "0.75 w {0:F2} {1:F2} {2:F2} {3:F2} re S\n", x, y, w, h);
            }
        }

        void DrawLine(float x1, float y1, float x2, float y2, float r = 0.2f, float g = 0.3f, float b = 0.4f, float width = 0.75f)
        {
            contentStream.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} RG {3:F2} w\n", r, g, b, width);
            contentStream.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} m {2:F2} {3:F2} l S\n", x1, y1, x2, y2);
        }

        void DrawText(string text, float x, float y, string font = "/F1", float size = 10, float r = 0.1f, float g = 0.15f, float b = 0.25f)
        {
            if (string.IsNullOrEmpty(text)) return;
            // Escape parentheses and backslashes for PDF text
            var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            contentStream.Append("BT\n");
            contentStream.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0} {1:F2} Tf\n", font, size);
            contentStream.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} {2:F2} rg\n", r, g, b);
            contentStream.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1:F2} Td\n", x, y);
            contentStream.AppendFormat("({0}) Tj\n", escaped);
            contentStream.Append("ET\n");
        }

        void DrawCenteredText(string text, float centerY, string font = "/F2", float size = 12, float r = 0.06f, float g = 0.16f, float b = 0.28f)
        {
            // Approximate width: character count * size * 0.52
            float approxWidth = text.Length * size * 0.52f;
            float startX = Math.Max(30, (pageWidth - approxWidth) / 2);
            DrawText(text, startX, centerY, font, size, r, g, b);
        }

        // --- PAGE BORDER ---
        DrawRect(20, 20, pageWidth - 40, pageHeight - 40, 1, 1, 1, false, true);
        DrawRect(24, 24, pageWidth - 48, pageHeight - 48, 1, 1, 1, false, true);

        // --- TOP INSTITUTION HEADER ---
        // Header background banner
        DrawRect(25, pageHeight - 95, pageWidth - 50, 70, 0.06f, 0.16f, 0.28f, true, false); // Navy #102A43

        DrawCenteredText("SHREE SARSWATI VISHV VIDYALAY", pageHeight - 50, "/F2", 15, 1, 1, 1);
        DrawCenteredText("INSTITUTE OF SCIENCE, BHAVNAGAR", pageHeight - 68, "/F2", 12, 0.95f, 0.75f, 0.25f); // Gold
        DrawCenteredText("OFFICIAL STATEMENT OF MARKS & ACADEMIC GRADE CARD", pageHeight - 84, "/F1", 9.5f, 0.9f, 0.95f, 1);

        // --- STUDENT DETAILS PANEL ---
        float infoY = pageHeight - 110;
        DrawRect(35, infoY - 62, pageWidth - 70, 68, 0.97f, 0.98f, 0.99f, true, true);

        // Column 1
        DrawText("Student Name:", 45, infoY - 14, "/F2", 9, 0.2f, 0.3f, 0.4f);
        DrawText(student.FullName, 130, infoY - 14, "/F2", 9.5f, 0.05f, 0.1f, 0.2f);

        DrawText("Enrollment No:", 45, infoY - 30, "/F2", 9, 0.2f, 0.3f, 0.4f);
        DrawText(student.StudentId, 130, infoY - 30, "/F2", 9.5f, 0.05f, 0.1f, 0.2f);

        DrawText("Admission Year:", 45, infoY - 46, "/F2", 9, 0.2f, 0.3f, 0.4f);
        DrawText(student.AdmissionYear.ToString(), 130, infoY - 46, "/F1", 9, 0.05f, 0.1f, 0.2f);

        // Column 2
        DrawText("Course / Program:", 320, infoY - 14, "/F2", 9, 0.2f, 0.3f, 0.4f);
        DrawText(student.Course, 415, infoY - 14, "/F2", 9.5f, 0.05f, 0.1f, 0.2f);

        DrawText("Department:", 320, infoY - 30, "/F2", 9, 0.2f, 0.3f, 0.4f);
        DrawText(student.Department, 415, infoY - 30, "/F1", 9, 0.05f, 0.1f, 0.2f);

        DrawText("Exam Semester:", 320, infoY - 46, "/F2", 9, 0.2f, 0.3f, 0.4f);
        DrawText(semesterResult.Semester, 415, infoY - 46, "/F2", 9.5f, 0.1f, 0.4f, 0.8f);

        // --- SUBJECT MARKS TABLE ---
        float tableTopY = infoY - 80;
        float rowHeight = 22;

        // Table Header
        DrawRect(35, tableTopY - rowHeight, pageWidth - 70, rowHeight, 0.1f, 0.25f, 0.45f, true, true);

        // Table Columns X coordinates
        // Widths: Code (55), Name (145), Cred (30), Int (35), Th (35), Pr (35), Proj (35), Viva (35), Tot (40), Gr (25), GP (25), Res (35)
        float colCode = 40;
        float colName = 95;
        float colCred = 230;
        float colInt = 265;
        float colTh = 300;
        float colPr = 335;
        float colProj = 370;
        float colViva = 405;
        float colTot = 440;
        float colGr = 475;
        float colGP = 502;
        float colRes = 527;

        float headerTextY = tableTopY - 15;
        DrawText("Code", colCode, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Subject Title", colName, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Cr", colCred, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Int", colInt, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Th", colTh, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Pr", colPr, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Proj", colProj, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Viva", colViva, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Total", colTot, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Gr", colGr, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("GP", colGP, headerTextY, "/F2", 8, 1, 1, 1);
        DrawText("Result", colRes, headerTextY, "/F2", 8, 1, 1, 1);

        // Data Rows
        float currY = tableTopY - rowHeight;

        for (int i = 0; i < semesterResult.Subjects.Count; i++)
        {
            var s = semesterResult.Subjects[i];
            currY -= rowHeight;

            // Zebra striping
            bool isEven = i % 2 == 0;
            DrawRect(35, currY, pageWidth - 70, rowHeight, isEven ? 1 : 0.96f, isEven ? 1 : 0.97f, isEven ? 1 : 0.99f, true, true);

            float rowTextY = currY + 6;

            DrawText(s.SubjectCode, colCode, rowTextY, "/F2", 8, 0.1f, 0.2f, 0.3f);

            // Truncate subject title if long
            string displayName = s.SubjectName;
            if (displayName.Length > 28) displayName = displayName.Substring(0, 26) + "..";
            DrawText(displayName, colName, rowTextY, "/F1", 8, 0.05f, 0.1f, 0.15f);

            DrawText(s.Credits.ToString("0.#"), colCred, rowTextY, "/F1", 8);

            DrawText(s.InternalObtained.HasValue ? $"{s.InternalObtained:0.#}" : "-", colInt, rowTextY, "/F1", 8);
            DrawText(s.TheoryObtained.HasValue ? $"{s.TheoryObtained:0.#}" : "-", colTh, rowTextY, "/F1", 8);
            DrawText(s.PracticalObtained.HasValue ? $"{s.PracticalObtained:0.#}" : "-", colPr, rowTextY, "/F1", 8);
            DrawText(s.ProjectObtained.HasValue ? $"{s.ProjectObtained:0.#}" : "-", colProj, rowTextY, "/F1", 8);
            DrawText(s.VivaObtained.HasValue ? $"{s.VivaObtained:0.#}" : "-", colViva, rowTextY, "/F1", 8);

            DrawText($"{s.TotalObtained:0.#}/{s.TotalMax:0.#}", colTot, rowTextY, "/F2", 7.5f, 0.1f, 0.15f, 0.3f);
            DrawText(s.Grade, colGr, rowTextY, "/F2", 8, 0.1f, 0.2f, 0.4f);
            DrawText($"{s.GradePoint:0.#}", colGP, rowTextY, "/F1", 8);

            // Pass/Fail color
            float resR = s.IsPass ? 0.05f : 0.8f;
            float resG = s.IsPass ? 0.5f : 0.1f;
            float resB = s.IsPass ? 0.2f : 0.1f;
            DrawText(s.ResultStatus, colRes, rowTextY, "/F2", 8, resR, resG, resB);
        }

        // --- SEMESTER SUMMARY CARD ---
        float sumY = currY - 50;
        DrawRect(35, sumY, pageWidth - 70, 42, 0.94f, 0.96f, 0.98f, true, true);

        DrawText("Total Credits:", 45, sumY + 24, "/F2", 8.5f, 0.2f, 0.3f, 0.4f);
        DrawText(semesterResult.TotalCredits.ToString("0.#"), 110, sumY + 24, "/F2", 9, 0.05f, 0.1f, 0.2f);

        DrawText("Marks Scored:", 160, sumY + 24, "/F2", 8.5f, 0.2f, 0.3f, 0.4f);
        DrawText($"{semesterResult.TotalObtainedMarks:0.#} / {semesterResult.TotalMaxMarks:0.#} ({semesterResult.Percentage:0.0}%)", 230, sumY + 24, "/F2", 9, 0.05f, 0.1f, 0.2f);

        DrawText("Semester SGPA:", 380, sumY + 24, "/F2", 9.5f, 0.1f, 0.2f, 0.4f);
        DrawText(semesterResult.SGPA.HasValue ? semesterResult.SGPA.Value.ToString("0.00") : "--", 470, sumY + 24, "/F2", 11, 0.85f, 0.5f, 0.05f); // Gold

        DrawText("Semester Result:", 45, sumY + 8, "/F2", 8.5f, 0.2f, 0.3f, 0.4f);
        float passR = semesterResult.ResultStatus == "PASS" ? 0.05f : 0.8f;
        float passG = semesterResult.ResultStatus == "PASS" ? 0.5f : 0.1f;
        DrawText(semesterResult.ResultStatus, 130, sumY + 8, "/F2", 10, passR, passG, 0.1f);

        // --- CUMULATIVE ACADEMIC PROGRESS (SEM 1 TO 6) ---
        float progY = sumY - 60;
        DrawRect(35, progY, pageWidth - 70, 50, 1, 1, 1, true, true);

        DrawText("CUMULATIVE PERFORMANCE OVERVIEW (SEMESTER 1 - 6)", 45, progY + 36, "/F2", 8.5f, 0.1f, 0.2f, 0.4f);

        // Semesters SGPA row
        float semX = 45;
        for (int semIdx = 1; semIdx <= 6; semIdx++)
        {
            string semKey = $"Semester {semIdx}";
            string val = "--";
            if (overallResult.SemesterResults.ContainsKey(semKey))
            {
                var r = overallResult.SemesterResults[semKey];
                if (r.SGPA.HasValue) val = r.SGPA.Value.ToString("0.00");
            }
            DrawText($"Sem {semIdx}:", semX, progY + 18, "/F1", 7.5f, 0.4f, 0.4f, 0.5f);
            DrawText(val, semX, progY + 6, "/F2", 8, 0.1f, 0.15f, 0.25f);
            semX += 65;
        }

        // Overall CGPA on right
        DrawText("Overall CGPA:", 445, progY + 22, "/F2", 9, 0.1f, 0.2f, 0.4f);
        DrawText(overallResult.CGPA.HasValue ? overallResult.CGPA.Value.ToString("0.00") : "--", 515, progY + 22, "/F2", 11, 0.05f, 0.45f, 0.85f); // Blue
        DrawText($"Overall: {overallResult.OverallResult}", 445, progY + 6, "/F2", 8.5f, overallResult.OverallResult == "PASS" ? 0.05f : 0.7f, overallResult.OverallResult == "PASS" ? 0.5f : 0.1f, 0.1f);

        // --- GRADING SCALE REFERENCE ---
        float scaleY = progY - 32;
        DrawText("Grading Scale: O (85-100%, 10) | A+ (75-84%, 9) | A (65-74%, 8) | B+ (55-64%, 7) | B (50-54%, 6) | C (45-49%, 5) | P (40-44%, 4) | F (<40%, 0)", 35, scaleY, "/F1", 6.8f, 0.45f, 0.5f, 0.6f);

        // --- SIGNATURE AND STAMP AREA ---
        float bottomY = 45;
        DrawLine(45, bottomY + 25, 185, bottomY + 25, 0.6f, 0.6f, 0.6f, 0.75f);
        DrawText("Prepared & Verified By", 55, bottomY + 12, "/F1", 8, 0.3f, 0.35f, 0.4f);

        DrawText("Official Seal / Stamp", 245, bottomY + 12, "/F1", 8, 0.3f, 0.35f, 0.4f);

        DrawLine(400, bottomY + 25, 545, bottomY + 25, 0.6f, 0.6f, 0.6f, 0.75f);
        DrawText("Signature of Authorized Authority", 400, bottomY + 12, "/F1", 8, 0.3f, 0.35f, 0.4f);

        // Generation Date footer
        DrawText($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm} | Shree Sarswati Vishv Vidyalay Institute of Science", 35, 28, "/F1", 6.5f, 0.55f, 0.6f, 0.65f);

        // Assemble Raw PDF Byte Array
        var streamBytes = Encoding.ASCII.GetBytes(contentStream.ToString());

        var body = new StringBuilder();
        var offsets = new List<long>();

        body.Append("%PDF-1.4\n");

        // Object 1: Catalog
        offsets.Add(body.Length);
        body.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        // Object 2: Pages
        offsets.Add(body.Length);
        body.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        // Object 3: Page
        offsets.Add(body.Length);
        body.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {0:F2} {1:F2}] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>\nendobj\n",
            pageWidth, pageHeight);

        // Object 4: Content Stream
        offsets.Add(body.Length);
        body.AppendFormat("4 0 obj\n<< /Length {0} >>\nstream\n", streamBytes.Length);
        body.Append(contentStream.ToString());
        body.Append("\nendstream\nendobj\n");

        // Object 5: Font F1 (Helvetica Regular)
        offsets.Add(body.Length);
        body.Append("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        // Object 6: Font F2 (Helvetica Bold)
        offsets.Add(body.Length);
        body.Append("6 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n");

        // XRef Table
        long xrefStart = body.Length;
        body.AppendFormat("xref\n0 {0}\n", offsets.Count + 1);
        body.Append("0000000000 65535 f \n");
        foreach (var off in offsets)
        {
            body.AppendFormat("{0:D10} 00000 n \n", off);
        }

        body.AppendFormat("trailer\n<< /Size {0} /Root 1 0 R >>\nstartxref\n{1}\n%%EOF\n", offsets.Count + 1, xrefStart);

        return Encoding.ASCII.GetBytes(body.ToString());
    }
}

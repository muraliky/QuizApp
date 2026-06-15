using System;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace QuizApp
{
    public static class ExcelScoreWriter
    {
        public static void WriteScore(string filePath, string fullName, int score, int total, DateTime timestamp)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            FileInfo fileInfo = new FileInfo(filePath);

            // Ensure directory exists
            if (!Directory.Exists(fileInfo.DirectoryName))
                Directory.CreateDirectory(fileInfo.DirectoryName);

            using (ExcelPackage package = fileInfo.Exists
                ? new ExcelPackage(fileInfo)
                : new ExcelPackage())
            {
                ExcelWorksheet ws;

                if (fileInfo.Exists && package.Workbook.Worksheets.Count > 0)
                {
                    ws = package.Workbook.Worksheets[0];
                }
                else
                {
                    ws = package.Workbook.Worksheets.Add("Quiz Scores");
                    CreateHeader(ws);
                }

                // Find next empty row
                int row = ws.Dimension?.End.Row + 1 ?? 2;
                if (row < 2) row = 2;

                double pct = total > 0 ? Math.Round((double)score / total * 100, 1) : 0;

                ws.Cells[row, 1].Value = row - 1;                          // S.No
                ws.Cells[row, 2].Value = fullName;                         // Full Name
                ws.Cells[row, 3].Value = score;                            // Score
                ws.Cells[row, 4].Value = total;                            // Total
                ws.Cells[row, 5].Value = pct / 100;                        // Percentage
                ws.Cells[row, 6].Value = timestamp.ToString("dd-MMM-yyyy HH:mm"); // Date & Time

                // Format percentage cell
                ws.Cells[row, 5].Style.Numberformat.Format = "0.0%";

                // Color code result
                string grade = pct >= 80 ? "Excellent" : pct >= 60 ? "Good" : pct >= 40 ? "Average" : "Needs Improvement";
                ws.Cells[row, 7].Value = grade;

                Color rowColor = pct >= 80 ? Color.FromArgb(198, 239, 206)
                              : pct >= 60 ? Color.FromArgb(255, 235, 156)
                              : Color.FromArgb(255, 199, 206);

                using (var range = ws.Cells[row, 1, row, 7])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(rowColor);
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Color.SetColor(Color.LightGray);
                }

                // Auto-fit columns
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                package.SaveAs(fileInfo);
            }
        }

        private static void CreateHeader(ExcelWorksheet ws)
        {
            // Title row
            ws.Cells[1, 1, 1, 7].Merge = true;
            ws.Cells[1, 1].Value = "Quiz Score Tracker";
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Row(1).Height = 30;

            // Column headers
            string[] headers = { "S.No", "Full Name", "Score", "Total", "Percentage", "Date & Time", "Grade" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[2, i + 1].Value = headers[i];
                ws.Cells[2, i + 1].Style.Font.Bold = true;
                ws.Cells[2, i + 1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[2, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[2, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
                ws.Cells[2, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[2, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.White);
            }
            ws.Row(2).Height = 22;

            // Freeze the header rows
            ws.View.FreezePanes(3, 1);
        }
    }
}

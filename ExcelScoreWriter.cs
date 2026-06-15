using System;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace QuizApp
{
    public static class ExcelScoreWriter
    {
        // Columns: S.No | Full Name | Score | Total | Percentage | Time Taken (sec) | Time Display | Submitted At
        private static readonly string[] Headers =
        {
            "S.No", "Full Name", "Score", "Total",
            "Percentage", "Time Taken (sec)", "Time Display", "Submitted At"
        };
        private const int COL_COUNT = 8;

        public static void WriteScore(string filePath, string fullName,
                                      int score, int total,
                                      DateTime timestamp, int timeTakenSeconds)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            FileInfo fileInfo = new FileInfo(filePath);
            if (!Directory.Exists(fileInfo.DirectoryName))
                Directory.CreateDirectory(fileInfo.DirectoryName);

            using var package = fileInfo.Exists
                ? new ExcelPackage(fileInfo)
                : new ExcelPackage();

            ExcelWorksheet ws;
            if (fileInfo.Exists && package.Workbook.Worksheets.Count > 0)
                ws = package.Workbook.Worksheets[0];
            else
            {
                ws = package.Workbook.Worksheets.Add("Quiz Scores");
                CreateHeader(ws);
            }

            int row = ws.Dimension?.End.Row + 1 ?? 3;
            if (row < 3) row = 3;

            double pct = total > 0 ? Math.Round((double)score / total * 100, 1) : 0;

            ws.Cells[row, 1].Value = row - 2;
            ws.Cells[row, 2].Value = fullName;
            ws.Cells[row, 3].Value = score;
            ws.Cells[row, 4].Value = total;
            ws.Cells[row, 5].Value = pct / 100.0;
            ws.Cells[row, 5].Style.Numberformat.Format = "0.0%";
            ws.Cells[row, 6].Value = timeTakenSeconds;
            ws.Cells[row, 7].Value = FormatTime(timeTakenSeconds);
            ws.Cells[row, 8].Value = timestamp.ToString("dd-MMM-yyyy HH:mm:ss");

            Color rowColor = pct >= 80 ? Color.FromArgb(198, 239, 206)
                           : pct >= 60 ? Color.FromArgb(255, 235, 156)
                                       : Color.FromArgb(255, 199, 206);

            using var range = ws.Cells[row, 1, row, COL_COUNT];
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(rowColor);
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Color.SetColor(Color.LightGray);

            foreach (int c in new[] { 1, 3, 4, 5, 6 })
                ws.Cells[row, c].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            package.SaveAs(fileInfo);
        }

        private static void CreateHeader(ExcelWorksheet ws)
        {
            ws.Cells[1, 1, 1, COL_COUNT].Merge = true;
            ws.Cells[1, 1].Value = "Quiz Score Tracker";
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;
            ws.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
            ws.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
            ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Row(1).Height = 30;

            for (int i = 0; i < Headers.Length; i++)
            {
                var cell = ws.Cells[2, i + 1];
                cell.Value = Headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.White);
            }
            ws.Row(2).Height = 22;
            ws.View.FreezePanes(3, 1);
        }

        public static string FormatTime(int seconds)
        {
            if (seconds < 60) return $"{seconds}s";
            int m = seconds / 60, s = seconds % 60;
            return s == 0 ? $"{m}m" : $"{m}m {s}s";
        }
    }
}

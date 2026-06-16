using ClosedXML.Excel;
using System.Globalization;
using System.Text;

namespace NeuralKernel.Plugins.Document.Office;

public sealed class MsExcelReader : IFileReader
{
    public IReadOnlyList<string> MimeType { get; } =
        [
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        ];

    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        var readerContent = new StringBuilder();
        using var workbook = new XLWorkbook(data);

        foreach (var worksheet in workbook.Worksheets)
        {
            var rowsUsed = worksheet.RangeUsed()?.RowsUsed();
            if (rowsUsed is null) { continue; }

            foreach (var row in rowsUsed)
            {
                if (row == null) { continue; }

                foreach (var cell in row.Cells())
                {
                    if (cell == null || cell.Value.IsBlank)
                    {
                        readerContent.Append(string.Empty);
                    }
                    else if (cell.Value.IsTimeSpan)
                    {
                        readerContent.Append(cell.Value.GetTimeSpan().ToString("g", CultureInfo.CurrentCulture));
                    }
                    else if (cell.Value.IsDateTime)
                    {
                        readerContent.Append(cell.Value.GetDateTime().ToString("d", CultureInfo.CurrentCulture));
                    }
                    else if (cell.Value.IsBoolean)
                    {
                        readerContent.Append(cell.Value.GetBoolean());
                    }
                    else if (cell.Value.IsText)
                    {
                        var value = cell.Value.GetText().Replace("\"", "\"\"", StringComparison.Ordinal);
                        readerContent.Append(string.IsNullOrEmpty(value) ? string.Empty : value);
                    }
                    else if (cell.Value.IsNumber)
                    {
                        readerContent.Append(cell.Value.GetNumber());
                    }
                    else if (cell.Value.IsUnifiedNumber)
                    {
                        readerContent.Append(cell.Value.GetUnifiedNumber());
                    }
                    else if (cell.Value.IsError)
                    {
                        readerContent.Append(cell.Value.GetError().ToString().Replace("\"", "\"\"", StringComparison.Ordinal));
                    }
                }

                readerContent.AppendLine();
            }
        }

        return await Task.FromResult(readerContent.ToString());
    }
}

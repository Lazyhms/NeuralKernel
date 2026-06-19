using ClosedXML.Excel;
using System.Globalization;
using System.Text;

namespace NeuralKernel.Plugins.Document.Office;

public sealed class MsExcelHandler : IDocumentHandler
{
    public IReadOnlyList<string> MimeType { get; } =
        [
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        ];

    public string DefaultExtension { get; } = "xlsx";

    public Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        var readerContent = new StringBuilder();
        using var workbook = new XLWorkbook(data);

        foreach (var worksheet in workbook.Worksheets)
        {
            readerContent.AppendLine($"【工作表: {worksheet.Name}】");

            var rowsUsed = worksheet.RangeUsed()?.RowsUsed();
            if (rowsUsed is null) { continue; }

            foreach (var row in rowsUsed)
            {
                if (row == null) { continue; }

                var cells = row.Cells().ToList();
                for (var i = 0; i < cells.Count; i++)
                {
                    var cell = cells[i];
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

                    if (i < cells.Count - 1)
                    {
                        readerContent.Append('\t');
                    }
                }

                readerContent.AppendLine();
            }
        }

        return Task.FromResult(readerContent.ToString());
    }

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(content);

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sheet1");

            var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

            var delimiter = DetectDelimiter(content);
            var hasTableShape = lines.Any(line => line.Contains(delimiter));

            if (hasTableShape)
            {
                var maxCols = 0;
                var data = new List<string[]>();

                foreach (var line in lines)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var cells = ParseLine(line, delimiter);
                    data.Add(cells);
                    if (cells.Length > maxCols) maxCols = cells.Length;
                }

                if (data.Count > 0)
                {
                    for (var rowIndex = 0; rowIndex < data.Count; rowIndex++)
                    {
                        for (var colIndex = 0; colIndex < data[rowIndex].Length; colIndex++)
                        {
                            var cell = worksheet.Cell(rowIndex + 1, colIndex + 1);
                            var value = data[rowIndex][colIndex];

                            if (rowIndex == 0)
                            {
                                cell.Style.Font.Bold = true;
                                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            }

                            SetCellValue(cell, value);
                        }
                    }

                    worksheet.Columns().AdjustToContents();

                    for (var colIndex = 1; colIndex <= maxCols; colIndex++)
                    {
                        worksheet.Column(colIndex).Width = Math.Max(worksheet.Column(colIndex).Width, 10);
                    }
                }
            }
            else
            {
                worksheet.Cell(1, 1).Value = content;
            }

            workbook.SaveAs(target);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static char DetectDelimiter(string content)
    {
        var tabCount = content.Count(c => c == '\t');
        var commaCount = content.Count(c => c == ',');
        var semicolonCount = content.Count(c => c == ';');

        if (tabCount > commaCount && tabCount > semicolonCount) return '\t';
        if (commaCount > semicolonCount) return ',';
        return ';';
    }

    private static string[] ParseLine(string line, char delimiter)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    current.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    private static void SetCellValue(IXLCell cell, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            cell.Value = string.Empty;
            return;
        }

        if (bool.TryParse(value, out var boolValue))
        {
            cell.Value = boolValue;
            return;
        }

        if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
        {
            cell.Value = intValue;
            return;
        }

        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
        {
            cell.Value = doubleValue;
            return;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
        {
            cell.Value = decimalValue;
            return;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
        {
            cell.Value = dateValue;
            cell.Style.DateFormat.Format = "yyyy-MM-dd";
            return;
        }

        cell.Value = value;
    }
}

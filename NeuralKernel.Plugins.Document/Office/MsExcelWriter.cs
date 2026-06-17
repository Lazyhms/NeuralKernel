using ClosedXML.Excel;

namespace NeuralKernel.Plugins.Document.Office;

/// <summary>
/// Microsoft Excel (.xlsx) 文件写入器。
/// 使用制表符分隔单元格、换行分隔行（例如 LLM 输出的 TSV 表格），
/// 若内容不含制表符则将整体作为单个单元格写入。
/// </summary>
public sealed class MsExcelWriter : IFileWriter
{
    public IReadOnlyList<string> MimeType { get; } =
        [
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        ];

    public string DefaultExtension { get; } = "xlsx";

    public string FormatName { get; } = "Microsoft Excel";

    public string FormatDescription { get; } = "数据表格、统计数据、进度表";

    public string ContentGuide { get; } = "- 内容必须是 TSV（制表符分隔）：列用 \\t，行用 \\n\n- 第一行通常作为表头。";

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (content is null) throw new ArgumentNullException(nameof(content));

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var hasTableShape = content.Contains('\t');

        if (hasTableShape)
        {
            for (var rowIndex = 0; rowIndex < lines.Length; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = lines[rowIndex];
                if (line.Length == 0) { continue; }

                var cells = line.Split('\t');
                for (var colIndex = 0; colIndex < cells.Length; colIndex++)
                {
                    worksheet.Cell(rowIndex + 1, colIndex + 1).Value = cells[colIndex];
                }
            }
        }
        else
        {
            worksheet.Cell(1, 1).Value = content;
        }

        await Task.Run(() => workbook.SaveAs(target), cancellationToken).ConfigureAwait(false);
    }
}

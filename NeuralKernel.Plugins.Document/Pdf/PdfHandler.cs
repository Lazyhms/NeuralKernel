using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace NeuralKernel.Plugins.Document.Pdf;

/// <summary>
/// PDF 文件处理器（只读）
/// </summary>
public class PdfHandler : IFileHandler
{
    public IReadOnlyList<string> MimeType { get; } = ["application/pdf"];

    public string? DefaultExtension { get; } = null;

    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var pdfDocument = PdfDocument.Open(data);
        if (pdfDocument == null) { return string.Empty; }

        var readerContent = new StringBuilder();
        foreach (var page in pdfDocument.GetPages().Where(w => w is not null))
        {
            readerContent.AppendLine(ContentOrderTextExtractor.GetText(page));
        }

        return await Task.FromResult(readerContent.ToString());
    }

    public Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("PDF 写入功能暂未实现");
    }
}

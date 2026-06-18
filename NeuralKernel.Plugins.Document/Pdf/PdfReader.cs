using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace NeuralKernel.Plugins.Document.Pdf;

/// <summary>
/// PDF 文件读取器
/// </summary>
public class PdfReader : IFileReader
{
    /// <summary>
    /// 
    /// </summary>
    public IReadOnlyList<string> MimeType { get; } = ["application/pdf"];

    /// <inheritdoc />
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
}
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace NeuralKernel.Plugins.Document.Office;

/// <summary>
/// Microsoft Word 文件读取�?
/// </summary>
public sealed class MsWordReader : IFileReader
{
    public IReadOnlyList<string> MimeType { get; } =
        [
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];

    /// <inheritdoc />
    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var wordprocessingDocument = WordprocessingDocument.Open(data, false);

        var mainPart = wordprocessingDocument.MainDocumentPart;
        if (mainPart is null) { return await Task.FromResult(string.Empty); }

        var paragraphs = mainPart.Document.Body?.Descendants<Paragraph>();
        if (paragraphs == null) { return await Task.FromResult(string.Empty); }

        var readerContent = new StringBuilder();
        foreach (Paragraph p in paragraphs)
        {
            readerContent.AppendLine(p.InnerText);
        }

        return await Task.FromResult(readerContent.ToString());
    }
}

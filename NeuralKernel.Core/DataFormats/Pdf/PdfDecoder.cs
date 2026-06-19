using NeuralKernel.Core.Pipeline;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace NeuralKernel.Core.DataFormats.Pdf;

public class PdfDecoder(ILoggerFactory loggerFactory) : IContentDecoder
{
    private readonly ILogger<PdfDecoder> _log = loggerFactory.CreateLogger<PdfDecoder>();

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return mimeType != null && mimeType.StartsWith(MimeTypes.Pdf, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(string filename, CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filename);
        return DecodeAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(BinaryData data, CancellationToken cancellationToken = default)
    {
        using var stream = data.ToStream();
        return DecodeAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileContent> DecodeAsync(Stream data, CancellationToken cancellationToken = default)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Extracting text from PDF file");
        }

        var result = new FileContent(MimeTypes.PlainText);
        using PdfDocument? pdfDocument = PdfDocument.Open(data);
        if (pdfDocument == null) { return Task.FromResult(result); }

        foreach (Page? page in pdfDocument.GetPages().Where(x => x != null))
        {
            // Note: no trimming, use original spacing when working with pages
            string pageContent = ContentOrderTextExtractor.GetText(page).NormalizeNewlines(false) ?? string.Empty;

            result.Sections.Add(new Chunk(pageContent, page.Number));
        }

        return Task.FromResult(result);
    }
}

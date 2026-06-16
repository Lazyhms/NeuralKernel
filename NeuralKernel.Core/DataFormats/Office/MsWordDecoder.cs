using NeuralKernel.Core.Pipeline;
using NeuralKernel.Core.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using System.Text;

namespace NeuralKernel.Core.DataFormats.Office;

public sealed class MsWordDecoder(ILoggerFactory loggerFactory) : IContentDecoder
{
    private readonly ILogger<MsWordDecoder> _log = loggerFactory.CreateLogger<MsWordDecoder>();

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
        => mimeType != null && mimeType.StartsWith(MimeTypes.MsWordX, StringComparison.OrdinalIgnoreCase);

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
            _log.LogDebug("Extracting text from MS Word file");
        }

        var result = new FileContent(MimeTypes.PlainText);
        var wordprocessingDocument = WordprocessingDocument.Open(data, false);
        try
        {
            StringBuilder sb = new();

            MainDocumentPart? mainPart = wordprocessingDocument.MainDocumentPart ?? throw new InvalidOperationException("The main document part is missing.");
            Body? body = mainPart.Document.Body ?? throw new InvalidOperationException("The document body is missing.");

            int pageNumber = 1;
            IEnumerable<Paragraph>? paragraphs = body.Descendants<Paragraph>();
            if (paragraphs != null)
            {
                foreach (Paragraph p in paragraphs)
                {
                    // Note: this is just an attempt at counting pages, not 100% reliable
                    // see https://stackoverflow.com/questions/39992870/how-to-access-openxml-content-by-page-number
                    var lastRenderedPageBreak = p.GetFirstChild<Run>()?.GetFirstChild<LastRenderedPageBreak>();
                    if (lastRenderedPageBreak != null)
                    {
                        // Note: no trimming, use original spacing when working with pages
                        string pageContent = sb.ToString().NormalizeNewlines(false);
                        sb.Clear();
                        result.Sections.Add(new Chunk(pageContent, pageNumber));
                        pageNumber++;
                    }

                    sb.AppendLineNix(p.InnerText);
                }
            }

            // Note: no trimming, use original spacing when working with pages
            string lastPageContent = sb.ToString().NormalizeNewlines(false);
            result.Sections.Add(new Chunk(lastPageContent, pageNumber));

            return Task.FromResult(result);
        }
        finally
        {
            wordprocessingDocument.Dispose();
        }
    }
}

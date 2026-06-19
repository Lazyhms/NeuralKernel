using NeuralKernel.Core.Pipeline;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace NeuralKernel.Core.DataFormats.WebPages;

public sealed class HtmlDecoder(ILoggerFactory loggerFactory) : IContentDecoder
{
    private readonly ILogger<HtmlDecoder> _log = loggerFactory.CreateLogger<HtmlDecoder>();

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
    {
        return mimeType != null && mimeType.StartsWith(MimeTypes.Html, StringComparison.OrdinalIgnoreCase);
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
            _log.LogDebug("Extracting text from HTML file");
        }

        var result = new FileContent(MimeTypes.PlainText);
        var doc = new HtmlDocument();
        doc.Load(data);

        result.Sections.Add(new Chunk(doc.DocumentNode.InnerText.NormalizeNewlines(true), 1));

        return Task.FromResult(result);
    }
}

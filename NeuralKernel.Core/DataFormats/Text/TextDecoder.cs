using NeuralKernel.Core.Pipeline;
using Microsoft.Extensions.Logging;
using System.Text;

namespace NeuralKernel.Core.DataFormats.Text;

public sealed class TextDecoder(ILoggerFactory loggerFactory) : IContentDecoder
{
    private readonly ILogger<TextDecoder> _log = loggerFactory.CreateLogger<TextDecoder>();

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType) => mimeType != null && (
            mimeType.StartsWith(MimeTypes.PlainText, StringComparison.OrdinalIgnoreCase) ||
            mimeType.StartsWith(MimeTypes.Json, StringComparison.OrdinalIgnoreCase)
        );

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
    public async Task<FileContent> DecodeAsync(Stream data, CancellationToken cancellationToken = default)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Extracting text from file");
        }

        var result = new FileContent(MimeTypes.PlainText);
        using var reader = new StreamReader(data, Encoding.UTF8, true);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        result.Sections.Add(new(content.Trim(), 1));
        return result;
    }
}

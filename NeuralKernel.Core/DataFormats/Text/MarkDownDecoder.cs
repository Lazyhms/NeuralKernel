using NeuralKernel.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace NeuralKernel.Core.DataFormats.Text;

public sealed class MarkDownDecoder(ILoggerFactory loggerFactory) : IContentDecoder
{
    private readonly ILogger<MarkDownDecoder> _log = loggerFactory.CreateLogger<MarkDownDecoder>();

    /// <inheritdoc />
    public bool SupportsMimeType(string mimeType)
        => mimeType != null && mimeType.StartsWith(MimeTypes.MarkDown, StringComparison.OrdinalIgnoreCase);

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
            _log.LogDebug("Extracting text from markdown file");
        }

        var result = new FileContent(MimeTypes.MarkDown);
        using var reader = new StreamReader(data);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        result.Sections.Add(new(content.Trim(), 1));
        return result;
    }
}

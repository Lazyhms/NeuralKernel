using Markdig;
using System.Text;

namespace NeuralKernel.Plugins.Document;

public interface IDocumentHandler
{
    IReadOnlyList<string> MimeType { get; }

    string DefaultExtension { get; }

    bool SupportMimeType(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType)) return false;

        return MimeType.Any(a => mimeType.StartsWith(a, StringComparison.OrdinalIgnoreCase));
    }

    async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(data, Encoding.UTF8, true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    async Task WriteAsync(Stream data, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(content);

        using var writer = new StreamWriter(data, Encoding.UTF8, leaveOpen: true);
        await writer.WriteAsync(content).ConfigureAwait(false);
    }
}

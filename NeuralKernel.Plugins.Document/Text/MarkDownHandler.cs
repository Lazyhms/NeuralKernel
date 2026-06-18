using Markdig;
using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// Markdown 文件处理器
/// </summary>
public sealed class MarkDownHandler : IFileHandler
{
    public IReadOnlyList<string> MimeType { get; } = ["text/markdown"];

    public string? DefaultExtension { get; } = "md";

    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(data, Encoding.UTF8, true);
        var readerContent = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return Markdown.ToPlainText(readerContent);
    }

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(content);

        var bytes = Encoding.UTF8.GetBytes(content);
        await target.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

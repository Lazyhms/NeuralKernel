using Markdig;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// MarkDown 文件读取器
/// </summary>
public sealed class MarkDownReader : IFileReader
{
    public IReadOnlyList<string> MimeType { get; } = ["text/markdown"];

    /// <inheritdoc />
    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(data, Encoding.UTF8, true);
        var readerContent = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return Markdown.ToPlainText(readerContent);
    }
}

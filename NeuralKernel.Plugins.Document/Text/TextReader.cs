using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// 文本文件读取器
/// </summary>
public sealed class TextReader : IFileReader
{
    public IReadOnlyList<string> MimeType { get; } = ["text/plain", "application/json"];

    /// <inheritdoc />
    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(data, Encoding.UTF8, true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }
}

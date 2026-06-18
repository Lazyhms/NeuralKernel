using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// 文本文件处理器
/// </summary>
public sealed class TextHandler : IFileHandler
{
    public IReadOnlyList<string> MimeType { get; } = ["text/plain"];

    public string? DefaultExtension { get; } = "txt";

    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(data, Encoding.UTF8, true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(content);

        await target.WriteAsync(Encoding.UTF8.GetBytes(content), cancellationToken).ConfigureAwait(false);
    }
}

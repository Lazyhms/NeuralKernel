using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// JSON 文件处理器（只写）
/// </summary>
public sealed class JsonHandler : IFileHandler
{
    public IReadOnlyList<string> MimeType { get; } = ["application/json"];

    public string? DefaultExtension { get; } = "json";

    public Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("JSON 读取功能暂未实现");
    }

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(content);

        var bytes = Encoding.UTF8.GetBytes(content);
        await target.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

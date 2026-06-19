using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// JSON 文件处理器（只写）
/// </summary>
public sealed class JsonHandler : IDocumentHandler
{
    public IReadOnlyList<string> MimeType { get; } = ["application/json"];

    public string DefaultExtension { get; } = "json";

    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(data, Encoding.UTF8, true);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(content);

        var bytes = Encoding.UTF8.GetBytes(content);
        await target.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

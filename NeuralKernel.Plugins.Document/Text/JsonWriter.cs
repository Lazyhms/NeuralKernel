using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// JSON 文件写入器（按 UTF-8 原样写入）。
/// </summary>
public sealed class JsonWriter : IFileWriter
{
    public IReadOnlyList<string> MimeType { get; } = ["application/json"];

    public string DefaultExtension { get; } = "json";

    public string FormatName { get; } = "JSON";

    public string FormatDescription { get; } = "结构化数据、API 响应体、配置";

    public string ContentGuide { get; } = "- 必须是合法的 JSON 字符串。";

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (content is null) throw new ArgumentNullException(nameof(content));

        var bytes = Encoding.UTF8.GetBytes(content);
        await target.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

namespace NeuralKernel.Plugins.Document;

/// <summary>
/// 文件写入器。
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// 支持的 MIME 类型集合。
    /// </summary>
    IReadOnlyList<string> MimeType { get; }

    /// <summary>
    /// 默认输出文件扩展名（不含点）。
    /// </summary>
    string DefaultExtension { get; }

    /// <summary>
    /// 格式名称，用于展示给用户。
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// 格式适用场景描述。
    /// </summary>
    string FormatDescription { get; }

    /// <summary>
    /// 内容写法说明，指导用户如何组织内容。
    /// </summary>
    string ContentGuide { get; }

    /// <summary>
    /// 检查是否支持指定的 MIME 类型。
    /// </summary>
    /// <param name="mimeType">要检查的 MIME 类型。</param>
    /// <returns>支持则为 true，否则为 false。</returns>
    bool SupportMimeType(string mimeType) =>
        !string.IsNullOrWhiteSpace(mimeType) && MimeType.Contains(mimeType, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 将文本内容写入到目标流中。
    /// </summary>
    /// <param name="target">要写入的目标流。</param>
    /// <param name="content">要写入的文本内容。</param>
    /// <param name="cancellationToken">用于取消写入的令牌。</param>
    Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default);
}

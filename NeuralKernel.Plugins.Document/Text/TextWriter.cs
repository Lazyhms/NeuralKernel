using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// 纯文本文件写入器。
/// </summary>
public sealed class TextWriter : IFileWriter
{
    public IReadOnlyList<string> MimeType { get; } = ["text/plain"];

    public string DefaultExtension { get; } = "txt";

    public string FormatName { get; } = "纯文本";

    public string FormatDescription { get; } = "简单备忘录、说明文本";

    public string ContentGuide { get; } = "- 标题居中或用全角等号包裹：=== 周报 === 或 【项目周报】\n- 章节用全角分隔线：--- 或 ★★\n- 正文直接写段落，空行分隔章节。";

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (content is null) throw new ArgumentNullException(nameof(content));

        var bytes = Encoding.UTF8.GetBytes(content);
        await target.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

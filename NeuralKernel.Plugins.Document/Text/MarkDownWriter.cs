using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// Markdown 文件写入器（按 UTF-8 原样写入）。
/// </summary>
public sealed class MarkDownWriter : IFileWriter
{
    public IReadOnlyList<string> MimeType { get; } = ["text/markdown"];

    public string DefaultExtension { get; } = "md";

    public string FormatName { get; } = "Markdown";

    public string FormatDescription { get; } = "技术文档、README、报告";

    public string ContentGuide { get; } = "- 使用标准 Markdown 语法：# 一级标题、## 二级标题、### 三级标题、**粗体**、*斜体*、- 无序列表、1. 有序列表、| 表头 |（表格）、> 引用文本";

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (content is null) throw new ArgumentNullException(nameof(content));

        var bytes = Encoding.UTF8.GetBytes(content);
        await target.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

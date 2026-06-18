using System.Text;

namespace NeuralKernel.Plugins.Document.Html;

/// <summary>
/// HTML 文件写入器。
/// 当内容已包含 <c>&lt;html&gt;</c> 根标签时按原样写入，否则自动包裹最小 HTML 文档结构。
/// </summary>
public sealed class HtmlWriter : IFileWriter
{
    public IReadOnlyList<string> MimeType { get; } = [HtmlReader.Html, HtmlReader.XHTML];

    public string DefaultExtension { get; } = "html";

    public string FormatName { get; } = "HTML";

    public string FormatDescription { get; } = "网页内容、宣传页面";

    public string ContentGuide { get; } = "- 使用完整 HTML 标签：<h1>标题</h1>、<h2>二级标题</h2>、<p>段落</p>、<strong>粗体</strong>、<em>斜体</em>、<ul><li>列表项</li></ul>、<ol><li>有序项</li></ol>、<table>...</table>";

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(content);

        var finalContent = content;
        if (!content.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            finalContent = "<!DOCTYPE html>\n<html>\n<head><meta charset=\"utf-8\"></head>\n<body>\n"
                + content
                + "\n</body>\n</html>";
        }

        var bytes = Encoding.UTF8.GetBytes(finalContent);
        await target.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

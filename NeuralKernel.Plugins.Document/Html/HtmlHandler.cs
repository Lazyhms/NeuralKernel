using HtmlAgilityPack;
using System.Text;

namespace NeuralKernel.Plugins.Document.Html;

/// <summary>
/// HTML 文件处理器
/// </summary>
public sealed class HtmlHandler : IFileHandler
{
    public const string Html = "text/html";
    public const string XHTML = "application/xhtml+xml";
    public const string XML = "application/xml";
    public const string XML2 = "text/xml";

    public IReadOnlyList<string> MimeType { get; } = [Html, XHTML];

    public string? DefaultExtension { get; } = "html";

    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        var document = new HtmlDocument();
        document.Load(data);
        return await Task.FromResult(document.DocumentNode.InnerText);
    }

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

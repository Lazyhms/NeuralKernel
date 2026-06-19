using HtmlAgilityPack;
using Markdig;
using System.Text;

namespace NeuralKernel.Plugins.Document.Html;

/// <summary>
/// HTML 文件处理器
/// </summary>
public sealed class HtmlHandler : IDocumentHandler
{
    private static readonly MarkdownPipeline s_pipeline = 
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public IReadOnlyList<string> MimeType { get; } = ["text/html", "application/xhtml+xml"];

    public string DefaultExtension { get; } = "html";

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

        var bytes = Encoding.UTF8.GetBytes(Markdown.ToHtml(content, s_pipeline));
        await target.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

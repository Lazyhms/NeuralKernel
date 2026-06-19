using HtmlAgilityPack;
using Markdig;
using System.Text;

namespace NeuralKernel.Plugins.Document.Html;

/// <summary>
/// HTML 文件处理器
/// </summary>
public sealed class HtmlHandler : IDocumentHandler
{
    public IReadOnlyList<string> MimeType { get; } = ["text/html", "application/xhtml+xml"];

    public string DefaultExtension { get; } = "html";
}

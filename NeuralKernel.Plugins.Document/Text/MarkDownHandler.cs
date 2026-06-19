using Markdig;
using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// Markdown 文件处理器
/// </summary>
public sealed class MarkDownHandler : IDocumentHandler
{
    public IReadOnlyList<string> MimeType { get; } = ["text/markdown"];

    public string DefaultExtension { get; } = "md";
}

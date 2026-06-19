using Markdig;
using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// 文本文件处理器
/// </summary>
public sealed class TextHandler : IDocumentHandler
{
    public IReadOnlyList<string> MimeType { get; } = ["text/plain"];

    public string DefaultExtension { get; } = "txt";
}

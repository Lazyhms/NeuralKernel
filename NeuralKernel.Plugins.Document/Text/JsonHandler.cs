using System.Text;

namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// JSON 文件处理器（只写）
/// </summary>
public sealed class JsonHandler : IDocumentHandler
{
    public IReadOnlyList<string> MimeType { get; } = ["application/json"];

    public string DefaultExtension { get; } = "json";
}

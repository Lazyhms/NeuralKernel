using HtmlAgilityPack;

namespace NeuralKernel.Plugins.Document.Html;

public sealed class HtmlReader : IFileReader
{
    public const string Html = "text/html";
    public const string XHTML = "application/xhtml+xml";
    public const string XML = "application/xml";
    public const string XML2 = "text/xml";

    public IReadOnlyList<string> MimeType { get; } = ["text/html"];

    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        var document = new HtmlDocument();
        document.Load(data);
        return await Task.FromResult(document.DocumentNode.InnerText);
    }
}

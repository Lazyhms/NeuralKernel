using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;
using Markdig;
using System.Text;

namespace NeuralKernel.Plugins.Document.Office;

public sealed class MsWordHandler : IDocumentHandler
{
    private static readonly MarkdownPipeline s_pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    public IReadOnlyList<string> MimeType { get; } =
        [
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];

    public string DefaultExtension { get; } = "docx";

    public Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var wordprocessingDocument = WordprocessingDocument.Open(data, false);

        var mainPart = wordprocessingDocument.MainDocumentPart;
        if (mainPart is null) { return Task.FromResult(string.Empty); }

        var body = mainPart.Document?.Body;
        if (body is null) { return Task.FromResult(string.Empty); }

        var paragraphs = body.Descendants<Paragraph>();
        if (paragraphs == null) { return Task.FromResult(string.Empty); }

        var readerContent = new StringBuilder();
        foreach (Paragraph p in paragraphs)
        {
            readerContent.AppendLineNix(p.InnerText);
        }

        return Task.FromResult(readerContent.ToString().NormalizeNewlines(false));
    }

    public async Task WriteAsync(Stream data, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(content);

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());

            var converter = new HtmlConverter(mainPart)
            {
                SupportsHeadingNumbering = true,
                ImageProcessing = ImageProcessingMode.Embed,
            };
            var parsedContent = await converter.ParseAsync(Markdown.ToHtml(content, s_pipeline), cancellationToken);
            foreach (var element in parsedContent)
            {
                mainPart.Document.Body!.Append(element);
            }

            mainPart.Document.Save();
        }

        stream.Position = 0;
        await stream.CopyToAsync(data, cancellationToken).ConfigureAwait(false);
    }
}
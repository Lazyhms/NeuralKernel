using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;
using Markdig.Extensions.Tables;
using System.Text;

namespace NeuralKernel.Plugins.Document.Office;

public sealed class MsWordHandler : IFileHandler
{
    private static readonly MarkdownPipeline s_pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public IReadOnlyList<string> MimeType { get; } =
        [
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];

    public string? DefaultExtension { get; } = "docx";

    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var wordprocessingDocument = WordprocessingDocument.Open(data, false);

        var mainPart = wordprocessingDocument.MainDocumentPart;
        if (mainPart is null) { return await Task.FromResult(string.Empty); }

        var paragraphs = mainPart.Document.Body?.Descendants<Paragraph>();
        if (paragraphs == null) { return await Task.FromResult(string.Empty); }

        var readerContent = new StringBuilder();
        foreach (Paragraph p in paragraphs)
        {
            readerContent.AppendLine(p.InnerText);
        }

        return await Task.FromResult(readerContent.ToString());
    }

    public async Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(content);

        using var buffer = new MemoryStream();
        await Task.Run(() =>
        {
            using var document = WordprocessingDocument.Create(buffer, WordprocessingDocumentType.Document, true);
            var mainPart = document.AddMainDocumentPart();
            mainPart.AddParagraphStyles();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new Body());
            var body = mainPart.Document.Body!;

            var markdown = Markdown.Parse(content, s_pipeline);
            var converter = new MarkdownToOpenXmlConverter(mainPart, cancellationToken);
            converter.Convert(markdown, body);

            body.AppendChild(new Paragraph());
            mainPart.Document.Save();
        }, cancellationToken).ConfigureAwait(false);

        buffer.Position = 0;
        await buffer.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
    }
}

file static class WordDocumentExtensions
{
    public static void AddParagraphStyles(this MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.StyleDefinitionsPart;
        if (stylesPart == null)
        {
            stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            stylesPart.Styles = new Styles();
        }

        var styles = stylesPart.Styles!;

        if (styles.Elements<Style>().All(s => s.StyleId?.Value != "Heading1"))
        {
            styles.AppendChild(new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "Heading1",
                StyleName = new StyleName { Val = "heading 1" },
                PrimaryStyle = new PrimaryStyle()
            }
            .WithParagraphProperties(
                new KeepNext(),
                new KeepLines(),
                new SpacingBetweenLines { Before = "240", After = "120" },
                new OutlineLevel { Val = 0 })
            .WithRunProperties(
                new Bold(),
                new FontSize { Val = "36" },
                new FontSizeComplexScript { Val = "36" },
                new Color { Val = "1F3864" }));
        }

        if (styles.Elements<Style>().All(s => s.StyleId?.Value != "Heading2"))
        {
            styles.AppendChild(new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "Heading2",
                StyleName = new StyleName { Val = "heading 2" },
                PrimaryStyle = new PrimaryStyle()
            }
            .WithParagraphProperties(
                new KeepNext(),
                new KeepLines(),
                new SpacingBetweenLines { Before = "200", After = "80" },
                new OutlineLevel { Val = 1 })
            .WithRunProperties(
                new Bold(),
                new FontSize { Val = "28" },
                new FontSizeComplexScript { Val = "28" },
                new Color { Val = "2E75B6" }));
        }

        if (styles.Elements<Style>().All(s => s.StyleId?.Value != "Heading3"))
        {
            styles.AppendChild(new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "Heading3",
                StyleName = new StyleName { Val = "heading 3" },
                PrimaryStyle = new PrimaryStyle()
            }
            .WithParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines { Before = "160", After = "60" },
                new OutlineLevel { Val = 2 })
            .WithRunProperties(
                new Bold(),
                new FontSize { Val = "24" },
                new FontSizeComplexScript { Val = "24" },
                new Color { Val = "538135" }));
        }

        if (styles.Elements<Style>().All(s => s.StyleId?.Value != "Quote"))
        {
            styles.AppendChild(new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "Quote",
                StyleName = new StyleName { Val = "Quote" },
                PrimaryStyle = new PrimaryStyle()
            }
            .WithParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines { Before = "120", After = "120" },
                new OutlineLevel { Val = 9 })
            .WithRunProperties(
                new Italic(),
                new Color { Val = "5A5A5A" }));
        }

        stylesPart.Styles!.Save();
    }

    private static Style WithParagraphProperties(this Style style, params OpenXmlElement[] props)
    {
        var pPr = style.AppendChild(new StyleParagraphProperties());
        foreach (var prop in props) pPr.AppendChild(prop);
        return style;
    }

    private static Style WithRunProperties(this Style style, params OpenXmlElement[] props)
    {
        var rPr = style.AppendChild(new StyleRunProperties());
        foreach (var prop in props) rPr.AppendChild(prop);
        return style;
    }
}

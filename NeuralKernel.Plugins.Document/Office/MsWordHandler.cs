using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using System.Text.RegularExpressions;

namespace NeuralKernel.Plugins.Document.Office;

public sealed class MsWordHandler : IFileHandler
{
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

            var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            var lineIndex = 0;
            var bulletCount = 0;
            var numberCount = 0;
            var inBulletList = false;
            var inNumberList = false;

            while (lineIndex < lines.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = lines[lineIndex];

                if (TryParseHeading(line, out var headingText, out var headingLevel))
                {
                    inBulletList = false;
                    inNumberList = false;
                    bulletCount = 0;
                    numberCount = 0;

                    var paragraph = new Paragraph(new ParagraphProperties(
                        new ParagraphStyleId { Val = $"Heading{headingLevel}" }));
                    var run = paragraph.AppendChild(new Run());
                    ApplyInlineFormat(headingText, run);
                    body.AppendChild(paragraph);
                    lineIndex++;
                }
                else if (TryParseCodeBlock(lines, ref lineIndex, out var codeContent))
                {
                    inBulletList = false;
                    inNumberList = false;
                    bulletCount = 0;
                    numberCount = 0;

                    var paragraph = new Paragraph(new ParagraphProperties(
                        new Justification { Val = JustificationValues.Left },
                        new SpacingBetweenLines { Before = "0", After = "120" }));
                    var run = paragraph.AppendChild(new Run(new RunProperties(
                        new Shading { Val = ShadingPatternValues.Clear, Fill = "F0F0F0" },
                        new FontSize { Val = "20" },
                        new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" })));
                    run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(codeContent) { Space = SpaceProcessingModeValues.Preserve });
                    body.AppendChild(paragraph);
                }
                else if (TryParseTable(lines, ref lineIndex, out var table))
                {
                    inBulletList = false;
                    inNumberList = false;
                    bulletCount = 0;
                    numberCount = 0;

                    body.AppendChild(table);
                }
                else if (TryParseBulletLine(line, out var bulletText))
                {
                    if (!inBulletList)
                    {
                        bulletCount = 0;
                        inBulletList = true;
                        inNumberList = false;
                    }
                    bulletCount++;

                    var paragraph = new Paragraph(new ParagraphProperties(
                        new Indentation { Left = "720", Hanging = "360" }));
                    var run = paragraph.AppendChild(new Run());
                    run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("o ") { Space = SpaceProcessingModeValues.Preserve });
                    ApplyInlineFormat(bulletText, run);
                    body.AppendChild(paragraph);
                    lineIndex++;
                }
                else if (TryParseNumberLine(line, out var numberText))
                {
                    if (!inNumberList)
                    {
                        numberCount = 0;
                        inNumberList = true;
                        inBulletList = false;
                    }
                    numberCount++;

                    var paragraph = new Paragraph(new ParagraphProperties(
                        new Indentation { Left = "720", Hanging = "360" }));
                    var run = paragraph.AppendChild(new Run());
                    run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text($"{numberCount}. ") { Space = SpaceProcessingModeValues.Preserve });
                    ApplyInlineFormat(numberText, run);
                    body.AppendChild(paragraph);
                    lineIndex++;
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    inBulletList = false;
                    inNumberList = false;
                    body.AppendChild(new Paragraph());
                    lineIndex++;
                }
                else
                {
                    inBulletList = false;
                    inNumberList = false;

                    var paragraph = new Paragraph(new ParagraphProperties(
                        new Justification { Val = JustificationValues.Left },
                        new SpacingBetweenLines { Before = "0", After = "120" }));
                    var run = paragraph.AppendChild(new Run());
                    ApplyInlineFormat(line, run);
                    body.AppendChild(paragraph);
                    lineIndex++;
                }
            }

            body.AppendChild(new Paragraph());
            mainPart.Document.Save();
        }, cancellationToken).ConfigureAwait(false);

        buffer.Position = 0;
        await buffer.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryParseHeading(string line, out string text, out int level)
    {
        level = 0;
        text = line;

        if (string.IsNullOrWhiteSpace(line)) return false;

        var trimmed = line.TrimStart();

        if (trimmed.StartsWith("### ", StringComparison.Ordinal))
        {
            level = 1; text = trimmed[4..]; return true;
        }
        if (trimmed.StartsWith("## ", StringComparison.Ordinal))
        {
            level = 2; text = trimmed[3..]; return true;
        }
        if (trimmed.StartsWith("# ", StringComparison.Ordinal))
        {
            level = 3; text = trimmed[2..]; return true;
        }

        return false;
    }

    private static bool TryParseCodeBlock(string[] lines, ref int index, out string content)
    {
        content = string.Empty;

        if (index >= lines.Length || !lines[index].StartsWith("`", StringComparison.Ordinal))
            return false;

        var startIndex = index;
        index++;

        while (index < lines.Length && !lines[index].StartsWith("`", StringComparison.Ordinal))
        {
            if (content.Length > 0) content += "\n";
            content += lines[index];
            index++;
        }

        if (index < lines.Length && lines[index].StartsWith("`", StringComparison.Ordinal))
        {
            index++;
            return true;
        }

        index = startIndex;
        return false;
    }

    private static bool TryParseTable(string[] lines, ref int index, out Table table)
    {
        table = null!;

        if (index + 1 >= lines.Length) return false;

        var firstLine = lines[index];
        if (!firstLine.Contains('|')) return false;

        var secondLine = lines[index + 1];
        if (!secondLine.Contains('|') || !secondLine.Any(c => c == '-' || c == ':')) return false;

        var rows = new List<string[]>();
        rows.Add(firstLine.Split('|').Select(s => s.Trim()).ToArray());

        index += 2;
        while (index < lines.Length && lines[index].Contains('|'))
        {
            rows.Add(lines[index].Split('|').Select(s => s.Trim()).ToArray());
            index++;
        }

        if (rows.Count < 2) return false;

        table = new Table();
        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });

        table.AppendChild(tableProperties);

        for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var tr = new TableRow();
            for (var colIdx = 0; colIdx < rows[rowIdx].Length; colIdx++)
            {
                var tc = new TableCell(new TableCellProperties(new TableCellWidth { Width = "0", Type = TableWidthUnitValues.Auto }));
                var p = new Paragraph();
                var r = p.AppendChild(new Run());

                if (rowIdx == 0)
                {
                    r.PrependChild(new RunProperties(new Bold()));
                }

                r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(rows[rowIdx][colIdx]) { Space = SpaceProcessingModeValues.Preserve });
                tc.AppendChild(p);
                tr.AppendChild(tc);
            }
            table.AppendChild(tr);
        }

        return true;
    }

    private static bool TryParseBulletLine(string line, out string text)
    {
        text = line;

        if (string.IsNullOrWhiteSpace(line)) return false;

        var trimmed = line.TrimStart();

        if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
        {
            text = trimmed.Substring(2);
            return true;
        }

        return false;
    }

    private static bool TryParseNumberLine(string line, out string text)
    {
        text = line;

        if (string.IsNullOrWhiteSpace(line)) return false;

        var trimmed = line.TrimStart();

        var match = Regex.Match(trimmed, @"^(\d+\.)\s+");
        if (match.Success)
        {
            text = trimmed.Substring(match.Length);
            return true;
        }

        return false;
    }

    private static void ApplyInlineFormat(string text, Run rootRun)
    {
        if (string.IsNullOrEmpty(text))
        {
            rootRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(string.Empty));
            return;
        }

        var pattern = @"\*{2}(.+?)\*{2}|\*(.+?)\*|(.+?)";
        var matches = Regex.Matches(text, pattern);
        if (matches.Count == 0)
        {
            rootRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text) { Space = SpaceProcessingModeValues.Preserve });
            return;
        }

        var lastIndex = 0;
        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                var prefix = text[lastIndex..match.Index];
                rootRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(prefix) { Space = SpaceProcessingModeValues.Preserve });
            }

            if (match.Value.StartsWith("**", StringComparison.Ordinal) && match.Groups[1].Success)
            {
                var boldRun = new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(match.Groups[1].Value) { Space = SpaceProcessingModeValues.Preserve },
                    new RunProperties(new Bold()));
                rootRun.Parent!.AppendChild(boldRun);
            }
            else if (match.Value.StartsWith("*", StringComparison.Ordinal) && !match.Value.StartsWith("**", StringComparison.Ordinal) && match.Groups[2].Success)
            {
                var italicRun = new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(match.Groups[2].Value) { Space = SpaceProcessingModeValues.Preserve },
                    new RunProperties(new Italic()));
                rootRun.Parent!.AppendChild(italicRun);
            }
            else if (match.Value.StartsWith("", StringComparison.Ordinal) && match.Groups[3].Success)
            {
                var codeRun = new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(match.Groups[3].Value) { Space = SpaceProcessingModeValues.Preserve },
                    new RunProperties(new Bold(), new Shading { Val = ShadingPatternValues.Clear, Fill = "E8E8E8" }));
                rootRun.Parent!.AppendChild(codeRun);
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            rootRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text[lastIndex..]) { Space = SpaceProcessingModeValues.Preserve });
        }
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

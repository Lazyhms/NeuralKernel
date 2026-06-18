using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text;

namespace NeuralKernel.Plugins.Document.Office;

public sealed class MarkdownToOpenXmlConverter
{
    private readonly MainDocumentPart _mainPart;
    private readonly CancellationToken _cancellationToken;

    public MarkdownToOpenXmlConverter(MainDocumentPart mainPart, CancellationToken cancellationToken = default)
    {
        _mainPart = mainPart;
        _cancellationToken = cancellationToken;
    }

    public void Convert(MarkdownDocument document, Body body)
    {
        foreach (var block in document)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            AppendBlock(block, body);
        }
    }

    private void AppendBlock(Block block, OpenXmlElement parent)
    {
        switch (block)
        {
            case HeadingBlock heading:
                AppendHeading(heading, parent);
                break;
            case ParagraphBlock paragraph:
                AppendParagraph(paragraph, parent);
                break;
            case QuoteBlock quote:
                AppendQuote(quote, parent);
                break;
            case FencedCodeBlock code:
                AppendFencedCodeBlock(code, parent);
                break;
            case ListBlock list:
                AppendList(list, parent);
                break;
            case Markdig.Extensions.Tables.Table table:
                AppendTable(table, parent);
                break;
            case ThematicBreakBlock:
                AppendThematicBreak(parent);
                break;
            case HtmlBlock html:
                AppendHtmlBlock(html, parent);
                break;
        }
    }

    private void AppendHeading(HeadingBlock block, OpenXmlElement parent)
    {
        var level = block.Level;
        if (level < 1 || level > 6) level = 1;

        var paragraph = new Paragraph(new ParagraphProperties(
            new ParagraphStyleId { Val = $"Heading{level}" }));

        if (block.Inline != null)
        {
            foreach (var inline in block.Inline)
            {
                AppendInline(inline, paragraph);
            }
        }

        parent.AppendChild(paragraph);
    }

    private void AppendParagraph(ParagraphBlock block, OpenXmlElement parent)
    {
        var paragraph = new Paragraph();

        if (block.Inline != null)
        {
            foreach (var inline in block.Inline)
            {
                AppendInline(inline, paragraph);
            }
        }

        if (paragraph.HasChildren)
        {
            parent.AppendChild(paragraph);
        }
    }

    private void AppendQuote(QuoteBlock block, OpenXmlElement parent)
    {
        foreach (var childBlock in block)
        {
            if (childBlock is ParagraphBlock paragraph)
            {
                var p = new Paragraph(new ParagraphProperties(
                    new ParagraphStyleId { Val = "Quote" }));

                if (paragraph.Inline != null)
                {
                    foreach (var inline in paragraph.Inline)
                    {
                        AppendInline(inline, p);
                    }
                }

                parent.AppendChild(p);
            }
        }
    }

    private void AppendFencedCodeBlock(FencedCodeBlock block, OpenXmlElement parent)
    {
        var language = block.Info ?? string.Empty;
        var content = block.Lines.ToString().TrimEnd('\n', '\r');

        var paragraph = new Paragraph(new ParagraphProperties(
            new Justification { Val = JustificationValues.Left },
            new SpacingBetweenLines { Before = "0", After = "120" }));

        var run = paragraph.AppendChild(new Run(new RunProperties(
            new Shading { Val = ShadingPatternValues.Clear, Fill = "F0F0F0" },
            new FontSize { Val = "20" },
            new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" })));

        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(content)
        {
            Space = SpaceProcessingModeValues.Preserve
        });

        parent.AppendChild(paragraph);
    }

    private void AppendThematicBreak(OpenXmlElement parent)
    {
        var paragraph = new Paragraph(new ParagraphProperties(
            new ParagraphBorders(
                new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "auto" })));

        parent.AppendChild(paragraph);
    }

    private void AppendList(ListBlock block, OpenXmlElement parent)
    {
        foreach (var item in block)
        {
            if (item is ListItemBlock listItem)
            {
                AppendListItem(listItem, parent, block.IsOrdered);
            }
        }
    }

    private void AppendListItem(ListItemBlock item, OpenXmlElement parent, bool isOrdered)
    {
        foreach (var itemBlock in item)
        {
            if (itemBlock is ParagraphBlock paragraph)
            {
                var p = new Paragraph(new ParagraphProperties(
                    new Indentation { Left = "720", Hanging = "360" }));

                if (paragraph.Inline != null)
                {
                    foreach (var inline in paragraph.Inline)
                    {
                        AppendInline(inline, p);
                    }
                }

                parent.AppendChild(p);
            }
        }
    }

    private void AppendTable(Markdig.Extensions.Tables.Table block, OpenXmlElement parent)
    {
        var table = new DocumentFormat.OpenXml.Wordprocessing.Table();

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

        var rowIndex = 0;
        foreach (var rowBlock in block)
        {
            if (rowBlock is Markdig.Extensions.Tables.TableRow tableRow)
            {
                var tr = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                var isHeader = tableRow.IsHeader;

                foreach (var cellBlock in tableRow)
                {
                    if (cellBlock is Markdig.Extensions.Tables.TableCell tableCell)
                    {
                        var tc = new DocumentFormat.OpenXml.Wordprocessing.TableCell(new TableCellProperties(
                            new TableCellWidth { Width = "0", Type = TableWidthUnitValues.Auto }));

                        foreach (var cellContent in tableCell)
                        {
                            if (cellContent is ParagraphBlock cellParagraph)
                            {
                                var p = new Paragraph();
                                if (cellParagraph.Inline != null)
                                {
                                    foreach (var inline in cellParagraph.Inline)
                                    {
                                        AppendInline(inline, p, isHeader);
                                    }
                                }
                                tc.AppendChild(p);
                            }
                        }

                        tr.AppendChild(tc);
                    }
                }

                table.AppendChild(tr);
                rowIndex++;
            }
        }

        parent.AppendChild(table);
    }

    private void AppendHtmlBlock(HtmlBlock block, OpenXmlElement parent)
    {
        var content = block.Lines.ToString().TrimEnd('\n', '\r');
        if (string.IsNullOrWhiteSpace(content)) return;

        var paragraph = new Paragraph(new ParagraphProperties(
            new Justification { Val = JustificationValues.Left }));

        var run = paragraph.AppendChild(new Run(new RunProperties(
            new FontSize { Val = "20" })));

        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(content)
        {
            Space = SpaceProcessingModeValues.Preserve
        });

        parent.AppendChild(paragraph);
    }

    private void AppendInline(Inline inline, OpenXmlElement parent, bool isHeader = false)
    {
        switch (inline)
        {
            case LiteralInline literal:
                AppendLiteral(literal, parent, isHeader);
                break;
            case EmphasisInline emphasis:
                AppendEmphasis(emphasis, parent);
                break;
            case LineBreakInline:
                AppendLineBreak(parent);
                break;
            case CodeInline code:
                AppendCodeInline(code, parent);
                break;
            case LinkInline link:
                AppendLink(link, parent);
                break;
            case AutolinkInline autolink:
                AppendAutolink(autolink, parent);
                break;
            case HtmlInline html:
                AppendHtmlInline(html, parent);
                break;
        }
    }

    private void AppendLiteral(LiteralInline inline, OpenXmlElement parent, bool isHeader = false)
    {
        var run = GetOrCreateRun(parent);
        if (isHeader)
        {
            run.PrependChild(new Bold());
        }
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(inline.Content.ToString())
        {
            Space = SpaceProcessingModeValues.Preserve
        });
    }

    private void AppendEmphasis(EmphasisInline inline, OpenXmlElement parent)
    {
        var text = inline.ToString() ?? string.Empty;
        var isBold = text.StartsWith("**", StringComparison.Ordinal) && text.EndsWith("**", StringComparison.Ordinal);
        var isItalic = !isBold && text.StartsWith("*", StringComparison.Ordinal) && text.EndsWith("*", StringComparison.Ordinal);

        foreach (var child in inline)
        {
            if (child is LiteralInline literal)
            {
                var props = new RunProperties();
                if (isBold) props.AppendChild(new Bold());
                if (isItalic) props.AppendChild(new Italic());

                var newRun = new Run(props);
                newRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(literal.Content.ToString())
                {
                    Space = SpaceProcessingModeValues.Preserve
                });
                parent.AppendChild(newRun);
            }
        }
    }

    private void AppendLineBreak(OpenXmlElement parent)
    {
        var run = GetOrCreateRun(parent);
        run.AppendChild(new Break());
    }

    private void AppendCodeInline(CodeInline inline, OpenXmlElement parent)
    {
        var run = new Run(new RunProperties(
            new Shading { Val = ShadingPatternValues.Clear, Fill = "E8E8E8" },
            new FontSize { Val = "20" },
            new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" }));

        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(inline.Content.ToString())
        {
            Space = SpaceProcessingModeValues.Preserve
        });

        parent.AppendChild(run);
    }

    private void AppendLink(LinkInline link, OpenXmlElement parent)
    {
        foreach (var child in link)
        {
            if (child is LiteralInline literal)
            {
                var props = new RunProperties();
                props.AppendChild(new Color { Val = "0563C1" });
                props.AppendChild(new Underline { Val = UnderlineValues.Single });

                var newRun = new Run(props);
                newRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(literal.Content.ToString())
                {
                    Space = SpaceProcessingModeValues.Preserve
                });
                parent.AppendChild(newRun);
            }
        }
    }

    private void AppendAutolink(AutolinkInline inline, OpenXmlElement parent)
    {
        var uri = inline.ToString();
        var displayText = uri;

        if (uri.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            displayText = uri["mailto:".Length..];
        }

        var props = new RunProperties();
        props.AppendChild(new Color { Val = "0563C1" });
        props.AppendChild(new Underline { Val = UnderlineValues.Single });

        var newRun = new Run(props);
        newRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(displayText)
        {
            Space = SpaceProcessingModeValues.Preserve
        });

        parent.AppendChild(newRun);
    }

    private void AppendHtmlInline(HtmlInline inline, OpenXmlElement parent)
    {
        var run = GetOrCreateRun(parent);
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(inline.Tag)
        {
            Space = SpaceProcessingModeValues.Preserve
        });
    }

    private static Run GetOrCreateRun(OpenXmlElement parent)
    {
        if (parent is Run run)
        {
            return run;
        }

        if (parent is Paragraph paragraph)
        {
            var lastRun = paragraph.Elements<Run>().LastOrDefault();
            if (lastRun != null)
            {
                return lastRun;
            }
        }

        return parent.AppendChild(new Run());
    }
}

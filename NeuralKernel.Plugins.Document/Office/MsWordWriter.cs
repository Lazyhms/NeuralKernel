using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace NeuralKernel.Plugins.Document.Office;

/// <summary>
/// Microsoft Word (.docx) 文件写入器。
/// 支持通过文本标记表达标题层级与段落样式：
/// - 以 <c>### 一级标题</c> 开头 → Heading1（一级标题）
/// - 以 <c>## 二级标题</c> 开头 → Heading2（二级标题）
/// - 以 <c># 三级标题</c> 开头 → Heading3（三级标题）
/// - 空行 → 段落分隔（空行本身不输出）
/// - 其余行 → Normal 段落
/// - 支持 Markdown 风格的行内格式：<c>**粗体**</c>、<c>*斜体*</c>、<c>`行内代码`</c>
/// </summary>
public sealed class MsWordWriter : IFileWriter
{
    public IReadOnlyList<string> MimeType { get; } =
        [
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];

    public string DefaultExtension { get; } = "docx";

    public string FormatName { get; } = "Microsoft Word";

    public string FormatDescription { get; } = "正式报告、合同、公文";

    public string ContentGuide { get; } = "- 使用行首标记表达标题层级：\n  ### 一级标题 → 大标题（18pt 深蓝加粗）\n  ## 二级标题 → 副标题（14pt 蓝色加粗）\n  # 三级标题 → 小标题（12pt 绿色加粗）\n- 普通段落直接写文本行，支持行内格式：**粗体**、*斜体*、`行内代码`\n- 用空行分隔章节层次。";

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

            // 行内格式化：先处理 bold/italic/code，再处理 heading 标记
            var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 跳过连续空行（保留一个间隔）
                if (string.IsNullOrWhiteSpace(line))
                {
                    // 空行作为段落分隔，不输出空段落
                    continue;
                }

                var paragraph = new Paragraph();

                if (TryParseHeading(line, out var headingText, out var headingLevel))
                {
                    // 标题行
                    var run = paragraph.AppendChild(new Run());
                    ApplyInlineFormat(headingText, run);
                    paragraph.PrependChild<ParagraphProperties>(new ParagraphProperties(
                        new ParagraphStyleId { Val = $"Heading{headingLevel}" }));
                }
                else
                {
                    // 普通段落
                    var run = paragraph.AppendChild(new Run());
                    ApplyInlineFormat(line, run);
                }

                body.AppendChild(paragraph);
            }

            // 修复文档末尾
            body.AppendChild(new Paragraph());

            mainPart.Document.Save();
        }, cancellationToken).ConfigureAwait(false);

        buffer.Position = 0;
        await buffer.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 解析行首的标题标记，返回不带标记的标题文本与级别（1-3）。
    /// </summary>
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

    /// <summary>
    /// 将 Markdown 风格的行内标记转换为 Word Run 元素。
    /// 支持 <c>**粗体**</c>、<c>*斜体*</c>、<c>`行内代码`</c>。
    /// </summary>
    private static void ApplyInlineFormat(string text, Run rootRun)
    {
        if (string.IsNullOrEmpty(text))
        {
            rootRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(string.Empty));
            return;
        }

        // 匹配 **粗体**、*斜体*、`代码`（不跨越换行）
        var pattern = @"\*{2}(.+?)\*{2}|\*(.+?)\*|`(.+?)`";
        var matches = Regex.Matches(text, pattern);
        if (matches.Count == 0)
        {
            rootRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text)
            {
                Space = SpaceProcessingModeValues.Preserve
            });
            return;
        }

        var lastIndex = 0;
        foreach (Match match in matches)
        {
            // 前缀
            if (match.Index > lastIndex)
            {
                var prefix = text[lastIndex..match.Index];
                rootRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(prefix)
                {
                    Space = SpaceProcessingModeValues.Preserve
                });
            }

            // 加粗
            if (match.Value.StartsWith("**", StringComparison.Ordinal) && match.Groups[1].Success)
            {
                var boldRun = new Run(
                    new DocumentFormat.OpenXml.Wordprocessing.Text(match.Groups[1].Value)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    },
                    new RunProperties(new Bold()));
                rootRun.Parent!.AppendChild(boldRun);
            }
            // 斜体
            else if (match.Value.StartsWith("*", StringComparison.Ordinal) && !match.Value.StartsWith("**", StringComparison.Ordinal) && match.Groups[2].Success)
            {
                var italicRun = new Run(
                    new DocumentFormat.OpenXml.Wordprocessing.Text(match.Groups[2].Value)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    },
                    new RunProperties(new Italic()));
                rootRun.Parent!.AppendChild(italicRun);
            }
            // 代码
            else if (match.Value.StartsWith("`", StringComparison.Ordinal) && match.Groups[3].Success)
            {
                var codeRun = new Run(
                    new DocumentFormat.OpenXml.Wordprocessing.Text(match.Groups[3].Value)
                    {
                        Space = SpaceProcessingModeValues.Preserve
                    },
                    new RunProperties(
                        new Bold(),
                        new Shading { Val = ShadingPatternValues.Clear, Fill = "E8E8E8" }));
                rootRun.Parent!.AppendChild(codeRun);
            }

            lastIndex = match.Index + match.Length;
        }

        // 后缀
        if (lastIndex < text.Length)
        {
            rootRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text[lastIndex..])
            {
                Space = SpaceProcessingModeValues.Preserve
            });
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

        // Heading1 — 大标题
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
                new FontSize { Val = "36" },      // 18pt
                new FontSizeComplexScript { Val = "36" },
                new Color { Val = "1F3864" }));
        }

        // Heading2 — 二级标题
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
                new FontSize { Val = "28" },      // 14pt
                new FontSizeComplexScript { Val = "28" },
                new Color { Val = "2E75B6" }));
        }

        // Heading3 — 三级标题
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
                new FontSize { Val = "24" },      // 12pt
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

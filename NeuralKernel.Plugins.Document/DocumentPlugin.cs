﻿﻿﻿using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace NeuralKernel.Plugins.Document;

public sealed class DocumentPlugin(IEnumerable<IDocumentHandler> handlers, ITempFileStorage tempFileStorage, ILogger<DocumentPlugin> logger)
{
    [KernelFunction("Read"), Description("读取文档内容，将文档转换为纯文本格式")]
    public async Task<string> Read(Stream stream, string mimeType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            logger.LogError("MIME类型不能为空");
            throw new ArgumentException("MIME类型不能为空", nameof(mimeType));
        }

        var handler = handlers.FirstOrDefault(r => r.SupportMimeType(mimeType));
        if (handler is null)
        {
            logger.LogError("不支持的MIME类型: {MimeType}", mimeType);
            throw new NotSupportedException($"不支持的MIME类型: {mimeType}");
        }

        return await handler.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    [KernelFunction, Description(@"将Markdown内容保存为指定格式的文档文件。

支持的MIME类型：
- application/vnd.openxmlformats-officedocument.wordprocessingml.document (Word文档.docx)
- application/vnd.openxmlformats-officedocument.spreadsheetml.sheet (Excel表格.xlsx)
- application/vnd.openxmlformats-officedocument.presentationml.presentation (PowerPoint演示文稿.pptx)
- text/markdown (Markdown文件.md)
- text/plain (纯文本.txt)
- text/html (HTML网页.html)
- application/json (JSON文件.json)

内容格式要求：
- 使用标准Markdown语法编写内容
- 使用 # ## ### 表示标题层级
- 使用 - 或 1. 表示列表
- 使用 | - | 格式表示表格
- 使用 **加粗** 表示重点内容")]
    public async Task<TempFileInfo> WriteAsync(
        [Description("要保存的Markdown格式文档内容")] string content,
        [Description("目标文档的MIME类型")] string mimeType,
        [Description("文件名（不含扩展名），可选")] string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            logger.LogError("MIME类型不能为空");
            throw new ArgumentException("MIME类型不能为空", nameof(mimeType));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            logger.LogWarning("内容为空");
            throw new ArgumentException("内容不能为空", nameof(content));
        }

        var handler = handlers.FirstOrDefault(w => w.SupportMimeType(mimeType));
        if (handler is null)
        {
            logger.LogError("不支持的MIME类型: {MimeType}", mimeType);
            throw new NotSupportedException($"不支持的MIME类型: {mimeType}");
        }

        var safeFileName = SanitizeFileName(fileName);
        var actualFileName = string.IsNullOrWhiteSpace(safeFileName)
            ? $"document_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.{handler.DefaultExtension}"
            : $"{safeFileName}.{handler.DefaultExtension}";

        using var stream = new MemoryStream();
        await handler.WriteAsync(stream, content, cancellationToken).ConfigureAwait(false);
        stream.Position = 0;

        var fileInfo = await tempFileStorage.SaveAsync(stream, actualFileName, mimeType, cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation("文档已生成: {FileName}, 大小: {Size}字节", fileInfo.FileName, fileInfo.Size);

        return fileInfo;
    }

    private static string? SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

        return sanitized.Trim();
    }
}

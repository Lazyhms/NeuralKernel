﻿using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace NeuralKernel.Plugins.Document;

public sealed class DocumentPlugin(IEnumerable<IDocumentHandler> handlers, ILogger<DocumentPlugin> logger)
{
    [KernelFunction, Description("列出当前请求中上传的所有文档文件，返回文件名、类型和大小")]
    public static string ListDocuments()
    {
        var files = DocumentFileContext.Current;
        if (files is null || files.Count == 0)
            return "当前请求中没有上传任何文档文件。";

        var lines = new List<string>();
        foreach (var file in files)
        {
            lines.Add($"- {file.Name}（{file.MimeType}，{file.Size} 字节）");
        }
        return $"已上传 {files.Count} 个文档文件：\n{string.Join("\n", lines)}";
    }

    [KernelFunction, Description("根据文件名读取上传文档的内容并转换为纯文本")]
    public async Task<string> Read(
        [Description("要读取的文档文件名")] string fileName,
        CancellationToken cancellationToken = default)
    {
        var files = DocumentFileContext.Current;
        if (files is null)
            throw new InvalidOperationException("当前请求上下文中没有可用的文档文件");

        var file = files.FirstOrDefault(f => string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase));
        if (file is null)
            throw new FileNotFoundException($"未找到文件：{fileName}");

        using var stream = new MemoryStream(file.Content);
        return await Read(stream, file.MimeType, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> Read(Stream stream, string mimeType, CancellationToken cancellationToken = default)
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

    [KernelFunction, Description("将文档内容保存为指定格式的文件；调用成功后用户即可下载该文件")]
    public async Task<string> Save(
        [Description("要保存的文档正文（必须是纯文本）")] string content,
        [Description("目标文档的 MIME 类型")] string mimeType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            logger.LogError("MIME类型不能为空");
            throw new ArgumentException("MIME类型不能为空", nameof(mimeType));
        }

        content ??= string.Empty;

        var output = DocumentOutputContext.Current
            ?? throw new InvalidOperationException("当前请求上下文中没有可用的文档输出接收器");

        var handler = handlers.FirstOrDefault(w => w.SupportMimeType(mimeType));
        if (handler is null)
        {
            logger.LogError("不支持的MIME类型: {MimeType}", mimeType);
            throw new NotSupportedException($"不支持的MIME类型: {mimeType}");
        }

        using var stream = new MemoryStream();
        await handler.WriteAsync(stream, content, cancellationToken).ConfigureAwait(false);

        output.MimeType = mimeType;
        output.Data = stream.ToArray();
        output.FileName = $"document_{DateTime.Now:yyyyMMddHHmmss}.{handler.DefaultExtension}";
        output.HasOutput = true;

        return $"已保存 {output.FileName}（{output.Data.Length} 字节，{output.MimeType}）";
    }
}

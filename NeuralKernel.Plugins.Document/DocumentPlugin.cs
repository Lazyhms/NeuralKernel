using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace NeuralKernel.Plugins.Document;

public sealed class DocumentPlugin(IEnumerable<IFileReader> readers, IEnumerable<IFileWriter> writers, ILogger<DocumentPlugin> logger)
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

    [KernelFunction, Description("获取所有支持的文档格式及其内容写法说明；在生成文档前调用以了解如何组织内容")]
    public string DocumentFormats(
        [Description("可选：指定格式名称或MIME类型以获取该格式的详细说明；不传则返回所有格式")] string? format = null)
    {
        var formatInfo = writers.SelectMany(w => w.MimeType.Select(mimeType => new
        {
            MimeType = mimeType,
            Writer = w
        })).ToDictionary(x => x.MimeType, x => x.Writer);

        if (!string.IsNullOrWhiteSpace(format))
        {
            var normalized = format.ToLowerInvariant();
            if (formatInfo.TryGetValue(normalized, out var writer))
                return BuildFormatInfo(writer, normalized);

            foreach (var (key, w) in formatInfo)
            {
                if (key.Contains(normalized) || normalized.Contains(key.Split('/')[1]))
                    return BuildFormatInfo(w, key);
            }

            return $"未找到格式：{format}。可用格式：{string.Join(", ", formatInfo.Keys)}";
        }

        return string.Join("\n\n", formatInfo.Select(kv => BuildFormatInfo(kv.Value, kv.Key)));
    }

    private static string BuildFormatInfo(IFileWriter writer, string mimeType) =>
        $"【{mimeType}（{writer.FormatName}，.{writer.DefaultExtension}）】\n适合：{writer.FormatDescription}。\n{writer.ContentGuide}";

    [KernelFunction, Description("根据文件名读取上传文档的内容并转换为纯文本")]
    public async Task<string> ReadDocument(
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
        return await ReadDocument(stream, file.MimeType, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ReadDocument(Stream stream, string mimeType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            logger.LogError("MIME类型不能为空");
            throw new ArgumentException("MIME类型不能为空", nameof(mimeType));
        }

        var reader = readers.FirstOrDefault(r => r.SupportMimeType(mimeType));
        if (reader is null)
        {
            logger.LogError("不支持的MIME类型: {MimeType}", mimeType);
            throw new NotSupportedException($"不支持的MIME类型: {mimeType}");
        }

        return await reader.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 将指定 MIME 类型的文档内容保存到当前请求的输出缓冲区，供上层端点作为文件返回。
    /// 仅由 LLM 在自动函数调用 (FunctionChoiceBehavior.Auto) 中触发。
    /// </summary>
    [KernelFunction, Description("将文档内容保存为指定格式的文件；调用成功后用户即可下载该文件")]
    public async Task<string> SaveDocument(
        [Description("要保存的文档正文（必须是纯文本，不含 Markdown 围栏或解释性文字）")] string content,
        [Description("目标文档的 MIME 类型，可选值：text/plain、text/markdown、text/html、application/json、application/vnd.openxmlformats-officedocument.wordprocessingml.document、application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")] string mimeType,
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

        var writer = writers.FirstOrDefault(w => w.SupportMimeType(mimeType));
        if (writer is null)
        {
            logger.LogError("不支持的MIME类型: {MimeType}", mimeType);
            throw new NotSupportedException($"不支持的MIME类型: {mimeType}");
        }

        using var stream = new MemoryStream();
        await writer.WriteAsync(stream, content, cancellationToken).ConfigureAwait(false);

        output.MimeType = mimeType;
        output.Data = stream.ToArray();
        output.FileName = $"document_{DateTime.Now:yyyyMMddHHmmss}.{writer.DefaultExtension}";
        output.HasOutput = true;

        return $"已保存 {output.FileName}（{output.Data.Length} 字节，{output.MimeType}）";
    }
}

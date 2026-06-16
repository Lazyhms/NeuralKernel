using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace NeuralKernel.Plugins.Document;

public sealed class DocumentPlugin(IEnumerable<IFileReader> readers, ILogger<DocumentPlugin> logger)
{
    [KernelFunction, Description("从文件流读取文档内容并转换为纯文本")]
    public async Task<string> ReadDocumentFromStream(
        [Description("文档文件流")] Stream stream,
        [Description("文件的MIME类型，如 application/pdf、text/plain 等")] string mimeType,
        CancellationToken cancellationToken = default)
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
}
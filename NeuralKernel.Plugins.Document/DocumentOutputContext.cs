namespace NeuralKernel.Plugins.Document;

/// <summary>
/// 单次请求中由 LLM 通过 <c>SaveDocument</c> 函数调用产出的文档。
/// </summary>
public sealed class DocumentOutput
{
    public string MimeType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public bool HasOutput { get; set; }
}

/// <summary>
/// 基于 <see cref="AsyncLocal{T}"/> 的请求级输出上下文，供 <c>DocumentPlugin.SaveDocument</c> 写入、
/// 上层端点读取，避免在 LLM 函数调用栈中显式传递输出流。
/// </summary>
public static class DocumentOutputContext
{
    private static readonly AsyncLocal<DocumentOutput?> _current = new();

    public static DocumentOutput? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}

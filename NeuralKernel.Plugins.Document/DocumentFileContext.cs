namespace NeuralKernel.Plugins.Document;

public sealed class DocumentFileInfo
{
    public string Name { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long Size { get; init; }
    public byte[] Content { get; init; } = [];
}

public static class DocumentFileContext
{
    private static readonly AsyncLocal<List<DocumentFileInfo>?> _files = new();

    public static List<DocumentFileInfo>? Current
    {
        get => _files.Value;
        set => _files.Value = value;
    }
}

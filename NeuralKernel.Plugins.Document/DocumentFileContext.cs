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
    private static readonly AsyncLocal<IReadOnlyList<DocumentFileInfo>?> _files = new();

    public static IReadOnlyList<DocumentFileInfo>? Current
    {
        get => _files.Value;
        set => _files.Value = value;
    }
}

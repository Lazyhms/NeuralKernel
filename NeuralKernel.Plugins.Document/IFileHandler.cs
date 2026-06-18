namespace NeuralKernel.Plugins.Document;

public interface IFileHandler
{
    IReadOnlyList<string> MimeType { get; }
    string? DefaultExtension { get; }
    bool SupportMimeType(string mimeType) =>
        !string.IsNullOrWhiteSpace(mimeType) && MimeType.Contains(mimeType, StringComparer.OrdinalIgnoreCase);
    Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default);
    Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default);
}

﻿namespace NeuralKernel.Plugins.Document;

public interface IDocumentHandler
{
    IReadOnlyList<string> MimeType { get; }

    string DefaultExtension { get; }

    bool SupportMimeType(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType)) return false;

        return MimeType.Any(a => mimeType.StartsWith(a, StringComparison.OrdinalIgnoreCase));
    }

    Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default);

    Task WriteAsync(Stream target, string content, CancellationToken cancellationToken = default);
}

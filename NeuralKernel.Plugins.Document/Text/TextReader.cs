namespace NeuralKernel.Plugins.Document.Text;

/// <summary>
/// �ı��ļ���ȡ��
/// </summary>
public sealed class TextReader : IFileReader
{
    public IReadOnlyList<string> MimeType { get; } = ["text/plain", "application/json"];

    /// <inheritdoc />
    public async Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(data);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }
}

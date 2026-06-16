namespace NeuralKernel.Plugins.Document;

/// <summary>
/// �ļ���ȡ��
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// 
    /// </summary>
    IReadOnlyList<string> MimeType { get; }

    /// <summary>
    /// ����Ƿ�֧��ָ���� MIME ���͡�
    /// </summary>
    /// <param name="mimeType">Ҫ���� MIME ���͡�</param>
    /// <returns>���֧����Ϊ true������Ϊ false��</returns>
    bool SupportMimeType(string mimeType) =>
        !string.IsNullOrWhiteSpace(mimeType) && MimeType.Contains(mimeType, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// �첽��ȡ�ļ����е����ݲ�ת��Ϊ���ı���
    /// </summary>
    /// <param name="data">Ҫ��ȡ���ļ�����</param>
    /// <param name="cancellationToken">����ȡ�����������ơ�</param>
    /// <returns>�����ļ����ݵ��ַ�������</returns>
    Task<string> ReadAsync(Stream data, CancellationToken cancellationToken = default);
}
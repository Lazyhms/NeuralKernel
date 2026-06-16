using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace NeuralKernel.Plugins.Core.FileIO;

/// <summary>
/// �ļ�ϵͳ����������ṩ�ļ���ȡ��д�롢Ŀ¼��������
/// </summary>
[KernelPlugin]
[Description("�ļ�ϵͳ����������ṩ�ļ���ȡ��д�롢Ŀ¼��������")]
public sealed class FileIOPlugin
{
    private HashSet<string>? _allowedFolders = [AppContext.BaseDirectory];

    /// <summary>
    /// �����д���ļ����б�������ļ��е���Ŀ¼Ҳͬ��ӵ�в���Ȩ�ޡ�
    /// </summary>
    public IEnumerable<string>? AllowedFolders
    {
        get => _allowedFolders;
        set => _allowedFolders = value is null ? null : new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ����Ϊ false ����������Ѵ��ڵ��ļ�
    /// </summary>
    public bool DisableFileOverwrite { get; set; } = true;

    /// <summary>
    /// ��ȡָ��·�����ı��ļ�����
    /// </summary>
    /// <param name="path">�ļ�������·��</param>
    [KernelFunction, Description("��ȡָ��·�����ı��ļ�����������")]
    public async Task<string> ReadTextFile(
        [Description("�ļ�����������·��")] string path,
        CancellationToken cancellationToken = default)
    {
        if (!IsFileAllowed(path))
        {
            throw new InvalidOperationException("�������ȡָ��·�����ļ���");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("�ļ������ڡ�");
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    /// <summary>
    /// ���ı�����д��ָ��·�����ļ�������ļ��������򴴽��������򸲸�
    /// </summary>
    /// <param name="filePath">Ҫд����ļ�·��</param>
    /// <param name="contents">Ҫд����ı�����</param>
    [KernelFunction, Description("���ı�����д���ļ����ļ��������򴴽��������򸲸�")]
    public async Task WriteAllText(
        [Description("Ŀ���ļ�������·��")] string path,
        [Description("Ҫд���ļ����ı�����")] string? contents,
        CancellationToken cancellationToken = default)
    {
        if (!IsFileAllowed(path))
        {
            throw new InvalidOperationException("������д��ָ��·�����ļ���");
        }

        if (DisableFileOverwrite && File.Exists(path))
        {
            throw new InvalidOperationException("��ֹ���������ļ���");
        }

        await File.WriteAllTextAsync(path, contents, cancellationToken);
    }

    /// <summary>
    /// ��ȡĿ¼�µ��ļ�����Ŀ¼�б�
    /// </summary>
    [KernelFunction, Description("��ȡָ��Ŀ¼�µ��ļ�����Ŀ¼·���б��֧��ͨ�������")]
    public string[] GetFileSystemEntries(
        [Description("Ҫ������Ŀ¼·��")] string path,
        [Description("����ͨ��������� *.*")] string searchPattern = "*.*",
        [Description("����ģʽ������ǰĿ¼ / ����������Ŀ¼")] bool allDirectories = false)
    {
        if (!IsDirectoryAllowed(path))
        {
            throw new InvalidOperationException("���������ָ��·����Ŀ¼��");
        }

        return Directory.GetFileSystemEntries(path, searchPattern, new EnumerationOptions
        {
            MaxRecursionDepth = 10,
            IgnoreInaccessible = true,
            MatchType = MatchType.Win32,
            ReturnSpecialDirectories = false,
            RecurseSubdirectories = allDirectories,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        });
    }

    /// <summary>
    /// У�顾�ļ�·�����Ƿ��ڰ�������
    /// </summary>
    private bool IsFileAllowed(string path) =>
        ValidatePathCommon(path, Path.GetDirectoryName(path)!);

    /// <summary>
    /// У�顾Ŀ¼·�����Ƿ��ڰ������ڣ��޸���Ŀ¼ר�ã�
    /// </summary>
    private bool IsDirectoryAllowed(string path) =>
        ValidatePathCommon(path, path);

    /// <summary>
    /// ����У���߼������ô��룩
    /// </summary>
    private bool ValidatePathCommon(string originalPath, string targetDirPath)
    {
        // ��ֹUNC����·��
        if (originalPath.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("��Ч���ļ�·������֧��UNC����·����", nameof(originalPath));
        }

        if (string.IsNullOrEmpty(targetDirPath))
        {
            throw new ArgumentException("��Ч��·��������ָ�������ľ���·����", nameof(originalPath));
        }

        // �ļ�ֻ��У�飨���ļ���Ҫ��Ŀ¼������
        if (File.Exists(originalPath) && File.GetAttributes(originalPath).HasFlag(FileAttributes.ReadOnly))
        {
            throw new UnauthorizedAccessException($"�ļ�Ϊֻ��״̬��{originalPath}");
        }

        // ������Ϊ�գ��ܾ����в���
        if (_allowedFolders is null || _allowedFolders.Count == 0)
        {
            return false;
        }

        // �淶��Ŀ��·��
        var canonicalDir = Path.GetFullPath(targetDirPath);
        var separator = Path.DirectorySeparatorChar.ToString();

        // ����������У��
        foreach (var allowedFolder in _allowedFolders)
        {
            var canonicalAllowed = Path.GetFullPath(allowedFolder);
            if (!canonicalAllowed.EndsWith(separator))
            {
                canonicalAllowed += separator;
            }

            if (canonicalDir.StartsWith(canonicalAllowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
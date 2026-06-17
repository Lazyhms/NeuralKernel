using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace NeuralKernel.Plugins.Core.FileIO;

/// <summary>
/// 文件系统插件，提供文件读取、写入、目录查询等功能。
/// </summary>
[KernelPlugin]
[Description("文件系统插件，提供文件读取、写入、目录查询等功能。")]
public sealed class FileIOPlugin
{
    private HashSet<string>? _allowedFolders = [AppContext.BaseDirectory];

    /// <summary>
    /// 允许写入的文件夹列表。文件夹中的子目录也同样被授予访问权限。
    /// </summary>
    public IEnumerable<string>? AllowedFolders
    {
        get => _allowedFolders;
        set => _allowedFolders = value is null ? null : new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 设置为false时允许覆盖已存在的文件。
    /// </summary>
    public bool DisableFileOverwrite { get; set; } = true;

    /// <summary>
    /// 获取指定路径上的文本文件内容。
    /// </summary>
    /// <param name="path">文件的绝对路径</param>
    [KernelFunction, Description("获取指定路径上的文本文件内容。")]
    public async Task<string> ReadTextFile(
        [Description("文件的绝对路径")] string path,
        CancellationToken cancellationToken = default)
    {
        if (!IsFileAllowed(path))
        {
            throw new InvalidOperationException("不允许读取指定路径的文件。");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("文件不存在。");
        }

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    /// <summary>
    /// 将文本内容写入指定路径的文件。如果文件不存在则创建，如果存在则覆盖。
    /// </summary>
    /// <param name="filePath">要写入的文件路径</param>
    /// <param name="contents">要写入的文本内容</param>
    [KernelFunction, Description("将文本内容写入文件。文件不存在则创建，存在则覆盖。")]
    public async Task WriteAllText(
        [Description("目标文件的绝对路径")] string path,
        [Description("要写入文件的文本内容")] string? contents,
        CancellationToken cancellationToken = default)
    {
        if (!IsFileAllowed(path))
        {
            throw new InvalidOperationException("不允许写入指定路径的文件。");
        }

        if (DisableFileOverwrite && File.Exists(path))
        {
            throw new InvalidOperationException("禁止覆盖现有文件。");
        }

        await File.WriteAllTextAsync(path, contents, cancellationToken);
    }

    /// <summary>
    /// 获取目录中的文件和子目录列表
    /// </summary>
    [KernelFunction, Description("获取指定目录中的文件和子目录路径列表。支持通配符搜索。")]
    public string[] GetFileSystemEntries(
        [Description("要搜索的目录路径")] string path,
        [Description("搜索通配符模式，默认*.*")] string searchPattern = "*.*",
        [Description("搜索模式，仅限当前目录/包括子目录")] bool allDirectories = false)
    {
        if (!IsDirectoryAllowed(path))
        {
            throw new InvalidOperationException("不允许访问指定路径的目录。");
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
    /// 检查文件路径是否在允许列表中
    /// </summary>
    private bool IsFileAllowed(string path) =>
        ValidatePathCommon(path, Path.GetDirectoryName(path)!);

    /// <summary>
    /// 检查目录路径是否在允许列表中。（修改为目录专用）
    /// </summary>
    private bool IsDirectoryAllowed(string path) =>
        ValidatePathCommon(path, path);

    /// <summary>
    /// 通用验证逻辑（可复用）
    /// </summary>
    private bool ValidatePathCommon(string originalPath, string targetDirPath)
    {
        // 防止UNC共享路径
        if (originalPath.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("无效的文件路径。不支持UNC共享路径。", nameof(originalPath));
        }

        if (string.IsNullOrEmpty(targetDirPath))
        {
            throw new ArgumentException("无效的路径。请指定有效的目录路径。", nameof(originalPath));
        }

        // 文件只读检查（如果文件存在）
        if (File.Exists(originalPath) && File.GetAttributes(originalPath).HasFlag(FileAttributes.ReadOnly))
        {
            throw new UnauthorizedAccessException($"文件为只读状态：{originalPath}");
        }

        // 如果列表为空，则拒绝所有访问
        if (_allowedFolders is null || _allowedFolders.Count == 0)
        {
            return false;
        }

        // 规范化目标路径
        var canonicalDir = Path.GetFullPath(targetDirPath);
        var separator = Path.DirectorySeparatorChar.ToString();

        // 遍历列表验证
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

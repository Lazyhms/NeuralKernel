namespace NeuralKernel.Plugins.Document;

/// <summary>
/// 临时文件信息
/// </summary>
/// <param name="FileId">文件唯一标识</param>
/// <param name="FileName">原始文件名</param>
/// <param name="MimeType">MIME类型</param>
/// <param name="Size">文件大小(字节)</param>
/// <param name="ExpiresAt">过期时间</param>
public record TempFileInfo(string FileId, string FileName, string MimeType, long Size, DateTimeOffset ExpiresAt);

/// <summary>
/// 临时文件存储服务接口
/// </summary>
public interface ITempFileStorage
{
    /// <summary>
    /// 保存文件到临时存储
    /// </summary>
    /// <param name="content">文件内容流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="mimeType">MIME类型</param>
    /// <param name="expiration">过期时间，默认30分钟</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>临时文件信息</returns>
    Task<TempFileInfo> SaveAsync(Stream content, string fileName, string mimeType, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取临时文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件流和元信息，如果文件不存在或已过期则返回null</returns>
    Task<(Stream? Stream, TempFileInfo? Info)> GetAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除临时文件
    /// </summary>
    /// <param name="fileId">文件ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(string fileId, CancellationToken cancellationToken = default);
}
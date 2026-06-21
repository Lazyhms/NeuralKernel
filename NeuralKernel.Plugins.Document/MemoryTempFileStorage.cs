using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace NeuralKernel.Plugins.Document;

/// <summary>
/// 基于内存缓存的临时文件存储实现
/// </summary>
public sealed class MemoryTempFileStorage : ITempFileStorage
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryTempFileStorage> _logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(30);

    public MemoryTempFileStorage(IMemoryCache cache, ILogger<MemoryTempFileStorage> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TempFileInfo> SaveAsync(Stream content, string fileName, string mimeType, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var fileId = Guid.NewGuid().ToString("N");
        var actualExpiration = expiration ?? _defaultExpiration;
        var expiresAt = DateTimeOffset.UtcNow.Add(actualExpiration);

        // 将流内容复制到内存
        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        var data = memoryStream.ToArray();

        var info = new TempFileInfo(fileId, fileName, mimeType, data.Length, expiresAt);

        var entry = _cache.CreateEntry(fileId);
        entry.AbsoluteExpiration = expiresAt;
        entry.Value = (data, info);
        entry.Dispose();

        _logger.LogInformation("临时文件已保存: {FileId}, 文件名: {FileName}, 大小: {Size}字节, 过期时间: {ExpiresAt}",
            fileId, fileName, data.Length, expiresAt);

        return info;
    }

    public Task<(Stream? Stream, TempFileInfo? Info)> GetAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        if (_cache.TryGetValue<(byte[] Data, TempFileInfo Info)>(fileId, out var entry))
        {
            _logger.LogInformation("临时文件已获取: {FileId}", fileId);
            return Task.FromResult<(Stream?, TempFileInfo?)>((new MemoryStream(entry.Data), entry.Info));
        }

        _logger.LogWarning("临时文件不存在或已过期: {FileId}", fileId);
        return Task.FromResult<(Stream?, TempFileInfo?)>((null, null));
    }

    public Task DeleteAsync(string fileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        _cache.Remove(fileId);
        _logger.LogInformation("临时文件已删除: {FileId}", fileId);

        return Task.CompletedTask;
    }
}
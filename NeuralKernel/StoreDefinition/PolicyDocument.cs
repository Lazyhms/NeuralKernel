using Microsoft.Extensions.VectorData;

namespace NeuralKernel.StoreDefinition;

/// <summary>
/// 用于 Qdrant 向量数据库存储策略文档的实现类
/// </summary>
public class PolicyDocument
{
    /// <summary>
    /// 向量ID（Qdrant 强制要求是 Guid / ulong）
    /// </summary>
    [VectorStoreKey]
    public Guid VectorId { get; set; }

    /// <summary>
    /// 向量字段
    /// </summary>
    [VectorStoreVector(4096, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vectors { get; set; }

    /// <summary>
    /// 文档ID
    /// </summary>
    [VectorStoreData(StorageName = "id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 标签列表（用于匹配相关数据）
    /// </summary>
    [VectorStoreData(StorageName = "tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// 负载数据（JSON格式）
    /// </summary>
    [VectorStoreData(StorageName = "payload")]
    public string Payload { get; set; } = default!;
}

public class Payload
{
    /// <summary>
    /// 来源文件名
    /// </summary>
    public string File { get; set; } = string.Empty;

    /// <summary>
    /// 文档文本内容
    /// </summary>
    public string Text { get; set; } = string.Empty;
}

using Microsoft.Extensions.VectorData;

namespace NeuralKernel.StoreDefinition;

/// <summary>
/// ���� Qdrant �ٷ����������ƶ��ĵ�ʵ����
/// </summary>
public class PolicyDocument
{
    /// <summary>
    /// ������Qdrant ǿ��Ҫ��Guid / ulong��
    /// </summary>
    [VectorStoreKey]
    public Guid VectorId { get; set; }

    /// <summary>
    /// �����ֶ�
    /// </summary>
    [VectorStoreVector(4096, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vectors { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [VectorStoreData(StorageName = "id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ��ǩ���飨ƥ��������ݣ�
    /// </summary>
    [VectorStoreData(StorageName = "tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// 
    /// </summary>
    [VectorStoreData(StorageName = "payload")]
    public string Payload { get; set; } = default!;
}

public class Payload
{
    /// <summary>
    /// 
    /// </summary>
    public string File { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string Text { get; set; } = string.Empty;
}
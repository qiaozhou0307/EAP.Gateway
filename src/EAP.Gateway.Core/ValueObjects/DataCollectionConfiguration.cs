using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 数据采集配置
/// </summary>
public class DataCollectionConfiguration : ValueObject
{
    /// <summary>
    /// 采集间隔（毫秒）
    /// </summary>
    public int CollectionInterval { get; }

    /// <summary>
    /// 数据变量ID列表
    /// </summary>
    public IReadOnlyList<uint> DataVariableIds { get; }

    /// <summary>
    /// 事件ID列表
    /// </summary>
    public IReadOnlyList<uint> EventIds { get; }

    /// <summary>
    /// 批量大小
    /// </summary>
    public int BatchSize { get; }

    /// <summary>
    /// 是否启用数据压缩
    /// </summary>
    public bool EnableCompression { get; }

    public DataCollectionConfiguration(
        int collectionInterval = 1000,
        IEnumerable<uint>? dataVariableIds = null,
        IEnumerable<uint>? eventIds = null,
        int batchSize = 100,
        bool enableCompression = false)
    {
        CollectionInterval = collectionInterval > 0 ? collectionInterval : throw new ArgumentException("Collection interval must be positive", nameof(collectionInterval));
        DataVariableIds = dataVariableIds?.ToList().AsReadOnly() ?? new List<uint>().AsReadOnly();
        EventIds = eventIds?.ToList().AsReadOnly() ?? new List<uint>().AsReadOnly();
        BatchSize = batchSize > 0 ? batchSize : throw new ArgumentException("Batch size must be positive", nameof(batchSize));
        EnableCompression = enableCompression;
    }

    /// <summary>
    /// 创建默认数据采集配置
    /// </summary>
    /// <returns>默认配置</returns>
    public static DataCollectionConfiguration Default() => new();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CollectionInterval;
        yield return BatchSize;
        yield return EnableCompression;

        foreach (var id in DataVariableIds)
            yield return id;

        foreach (var id in EventIds)
            yield return id;
    }
}

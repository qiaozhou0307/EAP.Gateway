using EAP.Gateway.Core.Common;
using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Aggregates.EquipmentAggregate;

/// <summary>
/// 设备聚合根仓储接口
/// 定义设备持久化操作的契约
/// </summary>
public interface IEquipmentRepository
{
    /// <summary>
    /// 根据设备ID获取设备聚合根
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备聚合根，不存在时返回null</returns>
    Task<Equipment?> GetByIdAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据设备ID获取设备聚合根，如果不存在则抛出异常
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备聚合根</returns>
    /// <exception cref="EquipmentNotFoundException">设备不存在时抛出</exception>
    Task<Equipment> GetByIdRequiredAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据网络端点查找设备
    /// </summary>
    /// <param name="endpoint">网络端点</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备聚合根，不存在时返回null</returns>
    Task<Equipment?> GetByEndpointAsync(IpEndpoint endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据设备状态查询设备列表
    /// </summary>
    /// <param name="state">设备状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配状态的设备列表</returns>
    Task<IReadOnlyList<Equipment>> GetByStateAsync(EquipmentState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据连接状态查询设备列表
    /// </summary>
    /// <param name="isConnected">连接状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配连接状态的设备列表</returns>
    Task<IReadOnlyList<Equipment>> GetByConnectionStateAsync(bool isConnected, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有设备列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有设备列表</returns>
    Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询设备列表
    /// </summary>
    /// <param name="pageNumber">页码（从1开始）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页设备列表</returns>
    Task<PagedResult<Equipment>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据过滤条件查询设备
    /// </summary>
    /// <param name="filter">查询过滤器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配条件的设备列表</returns>
    Task<IReadOnlyList<Equipment>> GetByFilterAsync(EquipmentFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查设备是否存在
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备是否存在</returns>
    Task<bool> ExistsAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加新设备
    /// </summary>
    /// <param name="equipment">设备聚合根</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加的设备</returns>
    Task<Equipment> AddAsync(Equipment equipment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新设备
    /// </summary>
    /// <param name="equipment">设备聚合根</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的设备</returns>
    Task<Equipment> UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除设备
    /// </summary>
    /// <param name="equipmentId">设备标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新设备状态
    /// </summary>
    /// <param name="updates">状态更新列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新成功的设备数量</returns>
    Task<int> BatchUpdateStateAsync(IEnumerable<EquipmentStateUpdate> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取设备统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备统计信息</returns>
    Task<EquipmentStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存工作单元的更改
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>影响的行数</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">结果项类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 结果项列表
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        TotalCount = totalCount >= 0 ? totalCount : throw new ArgumentOutOfRangeException(nameof(totalCount));
        PageNumber = pageNumber > 0 ? pageNumber : throw new ArgumentOutOfRangeException(nameof(pageNumber));
        PageSize = pageSize > 0 ? pageSize : throw new ArgumentOutOfRangeException(nameof(pageSize));
    }

    /// <summary>
    /// 创建空的分页结果
    /// </summary>
    public static PagedResult<T> Empty(int pageNumber, int pageSize)
    {
        return new PagedResult<T>(Array.Empty<T>(), 0, pageNumber, pageSize);
    }
}

/// <summary>
/// 设备查询过滤器
/// </summary>
public class EquipmentFilter
{
    /// <summary>
    /// 设备名称过滤（支持模糊查询）
    /// </summary>
    public string? NameFilter { get; set; }

    /// <summary>
    /// 设备状态过滤
    /// </summary>
    public EquipmentState? State { get; set; }

    /// <summary>
    /// 连接状态过滤
    /// </summary>
    public bool? IsConnected { get; set; }

    /// <summary>
    /// 制造商过滤
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// 设备型号过滤
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// IP地址过滤
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 是否有活动报警
    /// </summary>
    public bool? HasActiveAlarms { get; set; }

    /// <summary>
    /// 最后更新时间范围（开始）
    /// </summary>
    public DateTime? LastUpdatedFrom { get; set; }

    /// <summary>
    /// 最后更新时间范围（结束）
    /// </summary>
    public DateTime? LastUpdatedTo { get; set; }

    /// <summary>
    /// 是否启用数据采集
    /// </summary>
    public bool? EnableDataCollection { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// 排序方向
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// 创建空过滤器
    /// </summary>
    public static EquipmentFilter Empty() => new();

    /// <summary>
    /// 创建按状态过滤的过滤器
    /// </summary>
    public static EquipmentFilter ByState(EquipmentState state) => new() { State = state };

    /// <summary>
    /// 创建按连接状态过滤的过滤器
    /// </summary>
    public static EquipmentFilter ByConnectionState(bool isConnected) => new() { IsConnected = isConnected };

    /// <summary>
    /// 创建按名称过滤的过滤器
    /// </summary>
    public static EquipmentFilter ByName(string nameFilter) => new() { NameFilter = nameFilter };
}

/// <summary>
/// 排序方向枚举
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// 升序
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// 降序
    /// </summary>
    Descending = 1
}

/// <summary>
/// 设备状态更新DTO
/// </summary>
public class EquipmentStateUpdate
{
    /// <summary>
    /// 设备标识
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 新状态
    /// </summary>
    public EquipmentState NewState { get; }

    /// <summary>
    /// 更新原因
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EquipmentStateUpdate(EquipmentId equipmentId, EquipmentState newState, string? reason = null, DateTime? updatedAt = null)
    {
        EquipmentId = equipmentId ?? throw new ArgumentNullException(nameof(equipmentId));
        NewState = newState;
        Reason = reason;
        UpdatedAt = updatedAt ?? DateTime.UtcNow;
    }
}

/// <summary>
/// 设备统计信息
/// </summary>
public class EquipmentStatistics
{
    /// <summary>
    /// 设备总数
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// 已连接设备数
    /// </summary>
    public int ConnectedCount { get; }

    /// <summary>
    /// 断开连接设备数
    /// </summary>
    public int DisconnectedCount { get; }

    /// <summary>
    /// 各状态设备统计
    /// </summary>
    public IReadOnlyDictionary<EquipmentState, int> StateStatistics { get; }

    /// <summary>
    /// 有活动报警的设备数
    /// </summary>
    public int EquipmentWithAlarmsCount { get; }

    /// <summary>
    /// 需要关注的设备数（故障、报警、停机）
    /// </summary>
    public int EquipmentRequiringAttentionCount { get; }

    /// <summary>
    /// 统计时间
    /// </summary>
    public DateTime StatisticsTime { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EquipmentStatistics(
        int totalCount,
        int connectedCount,
        int disconnectedCount,
        IReadOnlyDictionary<EquipmentState, int> stateStatistics,
        int equipmentWithAlarmsCount,
        int equipmentRequiringAttentionCount,
        DateTime? statisticsTime = null)
    {
        TotalCount = totalCount >= 0 ? totalCount : throw new ArgumentOutOfRangeException(nameof(totalCount));
        ConnectedCount = connectedCount >= 0 ? connectedCount : throw new ArgumentOutOfRangeException(nameof(connectedCount));
        DisconnectedCount = disconnectedCount >= 0 ? disconnectedCount : throw new ArgumentOutOfRangeException(nameof(disconnectedCount));
        StateStatistics = stateStatistics ?? throw new ArgumentNullException(nameof(stateStatistics));
        EquipmentWithAlarmsCount = equipmentWithAlarmsCount >= 0 ? equipmentWithAlarmsCount : throw new ArgumentOutOfRangeException(nameof(equipmentWithAlarmsCount));
        EquipmentRequiringAttentionCount = equipmentRequiringAttentionCount >= 0 ? equipmentRequiringAttentionCount : throw new ArgumentOutOfRangeException(nameof(equipmentRequiringAttentionCount));
        StatisticsTime = statisticsTime ?? DateTime.UtcNow;
    }

    /// <summary>
    /// 连接率（百分比）
    /// </summary>
    public double ConnectionRate => TotalCount > 0 ? (double)ConnectedCount / TotalCount * 100 : 0;

    /// <summary>
    /// 可用率（百分比）- 排除故障、维护、停机状态
    /// </summary>
    public double AvailabilityRate
    {
        get
        {
            if (TotalCount == 0) return 0;

            var unavailableCount = StateStatistics.Where(kvp => !kvp.Key.IsAvailable()).Sum(kvp => kvp.Value);
            return (double)(TotalCount - unavailableCount) / TotalCount * 100;
        }
    }

    /// <summary>
    /// 健康度评分（0-100）
    /// </summary>
    public double HealthScore
    {
        get
        {
            if (TotalCount == 0) return 100;

            // 基础分数基于连接率
            var baseScore = ConnectionRate;

            // 扣除故障和报警的影响
            var faultPenalty = StateStatistics.GetValueOrDefault(EquipmentState.FAULT, 0) * 20;
            var alarmPenalty = StateStatistics.GetValueOrDefault(EquipmentState.ALARM, 0) * 10;
            var downPenalty = StateStatistics.GetValueOrDefault(EquipmentState.DOWN, 0) * 15;

            var totalPenalty = (faultPenalty + alarmPenalty + downPenalty) / (double)TotalCount;

            return Math.Max(0, baseScore - totalPenalty);
        }
    }

    public override string ToString()
    {
        return $"Equipment Statistics: {ConnectedCount}/{TotalCount} connected ({ConnectionRate:F1}%), " +
               $"Health Score: {HealthScore:F1}, Requiring Attention: {EquipmentRequiringAttentionCount}";
    }
}

/// <summary>
/// 设备未找到异常
/// </summary>
public class EquipmentNotFoundException : Exception
{
    /// <summary>
    /// 设备标识
    /// </summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EquipmentNotFoundException(EquipmentId equipmentId)
        : base($"Equipment with ID '{equipmentId}' was not found.")
    {
        EquipmentId = equipmentId;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EquipmentNotFoundException(EquipmentId equipmentId, string message)
        : base(message)
    {
        EquipmentId = equipmentId;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public EquipmentNotFoundException(EquipmentId equipmentId, string message, Exception innerException)
        : base(message, innerException)
    {
        EquipmentId = equipmentId;
    }
}

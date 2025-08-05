using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Aggregates.MessageAggregate;

namespace EAP.Gateway.Core.Repositories;

/// <summary>
/// 消息仓储接口
/// </summary>
public interface IMessageRepository : IGenericRepository<SecsMessage, MessageId>
{
    Task<IEnumerable<SecsMessage>> GetByEquipmentIdAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecsMessage>> GetRecentMessagesAsync(TimeSpan timeRange, CancellationToken cancellationToken = default);
    Task<SecsMessage?> GetBySystemBytesAsync(uint systemBytes, CancellationToken cancellationToken = default);
}

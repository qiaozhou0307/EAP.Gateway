using System.Security.Claims;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;

namespace EAP.Gateway.Core.Repositories;

/// <summary>
/// 报警仓储接口
/// </summary>
public interface IAlarmRepository : IGenericRepository<Alarm, AlarmId>
{
    Task<IEnumerable<Alarm>> GetByEquipmentIdAsync(EquipmentId equipmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default);
    Task<Alarm?> GetByAlarmCodeAsync(string alarmCode, CancellationToken cancellationToken = default);
}

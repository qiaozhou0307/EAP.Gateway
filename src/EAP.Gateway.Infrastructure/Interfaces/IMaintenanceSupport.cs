namespace EAP.Gateway.Infrastructure.Interfaces;

/// <summary>
/// 维护支持接口
/// </summary>
public interface IMaintenanceSupport
{
    Task PerformMaintenanceAsync(CancellationToken cancellationToken = default);
}

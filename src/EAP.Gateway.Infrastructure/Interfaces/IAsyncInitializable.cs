namespace EAP.Gateway.Infrastructure.Interfaces;

/// <summary>
/// 异步初始化接口
/// </summary>
public interface IAsyncInitializable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

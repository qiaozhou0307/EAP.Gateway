namespace EAP.Gateway.Infrastructure.Persistence;

/// <summary>
/// 数据库初始化器接口
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SeedAsync(CancellationToken cancellationToken = default);
}

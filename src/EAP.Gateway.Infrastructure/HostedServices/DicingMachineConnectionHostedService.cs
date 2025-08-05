using EAP.Gateway.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.HostedServices;

/// <summary>
/// 裂片机连接管理后台服务 - 分离HostedService职责
/// 避免与业务服务的生命周期冲突
/// </summary>
public class DicingMachineConnectionHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DicingMachineConnectionHostedService> _logger;
    private IMultiDicingMachineConnectionManager? _connectionManager;

    public DicingMachineConnectionHostedService(
        IServiceProvider serviceProvider,
        ILogger<DicingMachineConnectionHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("启动裂片机连接管理后台服务");

        try
        {
            // 从服务容器获取连接管理器
            _connectionManager = _serviceProvider.GetRequiredService<IMultiDicingMachineConnectionManager>();

            // 如果连接管理器支持异步初始化，在这里调用
            if (_connectionManager is IAsyncInitializable initializable)
            {
                await initializable.InitializeAsync(cancellationToken);
            }

            _logger.LogInformation("裂片机连接管理后台服务启动成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动裂片机连接管理后台服务失败");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("停止裂片机连接管理后台服务");

        try
        {
            if (_connectionManager != null)
            {
                // 如果连接管理器支持异步释放，在这里调用
                if (_connectionManager is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (_connectionManager is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _logger.LogInformation("裂片机连接管理后台服务已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止裂片机连接管理后台服务时发生异常");
        }
    }
}

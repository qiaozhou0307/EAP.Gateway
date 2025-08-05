using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Core.ValueObjects;
using EAP.Gateway.Infrastructure.Configuration;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// HSMS客户端工厂实现
/// </summary>
public class HsmsClientFactory : IHsmsClientFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EapSecsGemOptions _options;
    private readonly ILogger<HsmsClientFactory> _logger;

    public HsmsClientFactory(
        IServiceProvider serviceProvider,
        IOptions<EapSecsGemOptions> options,
        ILogger<HsmsClientFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IHsmsClient CreateClient(DeviceConnectionConfig config)
    {
        try
        {
            _logger.LogDebug("创建HSMS客户端 [IP: {IpAddress}:{Port}]", config.IpAddress, config.Port);

            // 生成一个临时的设备ID用于客户端创建
            var tempEquipmentId = EquipmentId.Create();

            // 获取所需的依赖服务
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var hsmsLogger = _serviceProvider.GetRequiredService<ILogger<HsmsClient>>();

            // 创建HSMS客户端
            var hsmsClient = new HsmsClient(
                tempEquipmentId,
                config,
                Options.Create(_options),
                mediator,
                hsmsLogger);

            _logger.LogInformation("HSMS客户端创建成功 [IP: {IpAddress}:{Port}]", config.IpAddress, config.Port);
            return hsmsClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建HSMS客户端失败 [IP: {IpAddress}:{Port}]", config.IpAddress, config.Port);
            throw;
        }
    }

    public void ReleaseClient(IHsmsClient client)
    {
        try
        {
            if (client != null)
            {
                _logger.LogDebug("释放HSMS客户端 [设备ID: {EquipmentId}]", client.EquipmentId);

                if (client is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (client is IAsyncDisposable asyncDisposable)
                {
                    // 注意：这里是同步调用，在实际使用中可能需要改为异步
                    asyncDisposable.DisposeAsync().AsTask().Wait();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放HSMS客户端时发生异常");
        }
    }
}

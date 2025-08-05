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
/// SECS设备服务工厂实现（修复版本）
/// </summary>
public class SecsDeviceServiceFactory : ISecsDeviceServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EapSecsGemOptions _options;
    private readonly ILogger<SecsDeviceServiceFactory> _logger;

    public SecsDeviceServiceFactory(
        IServiceProvider serviceProvider,
        IOptions<EapSecsGemOptions> options,
        ILogger<SecsDeviceServiceFactory> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ISecsDeviceService CreateDeviceService(EquipmentId equipmentId, DeviceConnectionConfig config)
    {
        try
        {
            _logger.LogDebug("创建SECS设备服务 [设备ID: {EquipmentId}]", equipmentId);

            // 创建HSMS客户端
            var hsmsClientFactory = _serviceProvider.GetRequiredService<IHsmsClientFactory>();
            var hsmsClient = hsmsClientFactory.CreateClient(config);

            // 获取所需的依赖服务
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var deviceServiceLogger = _serviceProvider.GetRequiredService<ILogger<SecsDeviceService>>();

            // 创建设备服务
            var deviceService = new SecsDeviceService(
                equipmentId,
                hsmsClient,
                mediator,
                Options.Create(_options),
                deviceServiceLogger);

            _logger.LogInformation("SECS设备服务创建成功 [设备ID: {EquipmentId}]", equipmentId);
            return deviceService;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建SECS设备服务失败 [设备ID: {EquipmentId}]", equipmentId);
            throw;
        }
    }

    public void ReleaseDeviceService(EquipmentId equipmentId)
    {
        _logger.LogDebug("释放SECS设备服务 [设备ID: {EquipmentId}]", equipmentId);
        // 实现具体的释放逻辑
        // 注意：具体的设备服务实例管理可能需要在这里实现
    }
}

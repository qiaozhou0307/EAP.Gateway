using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Core.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.Communications.SecsGem;

/// <summary>
/// SECS设备服务工厂实现
/// </summary>
public class SecsDeviceServiceFactory : ISecsDeviceServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SecsDeviceServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public ISecsDeviceService CreateDeviceService(EquipmentId equipmentId, EquipmentConfiguration configuration)
    {
        // 创建 HSMS 客户端
        var hsmsClient = new HsmsClient(
            equipmentId,
            configuration,
            _serviceProvider.GetRequiredService<IMediator>(),
            _serviceProvider.GetRequiredService<ILogger<HsmsClient>>());

        // 创建设备服务
        return new SecsDeviceService(
            equipmentId,
            hsmsClient,
            _serviceProvider.GetRequiredService<IMediator>(),
            _serviceProvider.GetRequiredService<ILogger<SecsDeviceService>>());
    }
}

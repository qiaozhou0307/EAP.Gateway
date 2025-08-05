using EAP.Gateway.Core.ValueObjects;

namespace EAP.Gateway.Core.Services;

/// <summary>
/// HSMS客户端工厂接口
/// </summary>
public interface IHsmsClientFactory
{
    IHsmsClient CreateClient(DeviceConnectionConfig config);
    void ReleaseClient(IHsmsClient client);
}

using EAP.Gateway.Core.Common;
using System.Net;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// IP端点值对象
/// </summary>
public class IpEndpoint : ValueObject
{
    public string IpAddress { get; }
    public int Port { get; }

    public IpEndpoint(string ipAddress, int port)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));

        if (!IPAddress.TryParse(ipAddress, out _))
            throw new ArgumentException("Invalid IP address format", nameof(ipAddress));

        if (port <= 0 || port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

        IpAddress = ipAddress;
        Port = port;
    }

    /// <summary>
    /// 检查端点是否有效
    /// </summary>
    public bool IsValid
    {
        get
        {
            // 检查IP地址格式是否有效
            if (string.IsNullOrWhiteSpace(IpAddress) || !IPAddress.TryParse(IpAddress, out var parsedIp))
                return false;

            // 检查端口范围是否有效
            if (Port <= 0 || Port > 65535)
                return false;

            // 检查是否为保留地址
            if (IsReservedAddress(parsedIp))
                return false;

            return true;
        }
    }

    /// <summary>
    /// 检查是否为本地地址
    /// </summary>
    public bool IsLocalAddress
    {
        get
        {
            if (!IPAddress.TryParse(IpAddress, out var parsedIp))
                return false;

            return IPAddress.IsLoopback(parsedIp) ||
                   parsedIp.Equals(IPAddress.Any) ||
                   parsedIp.Equals(IPAddress.IPv6Any);
        }
    }

    /// <summary>
    /// 检查是否为私有地址
    /// </summary>
    public bool IsPrivateAddress
    {
        get
        {
            if (!IPAddress.TryParse(IpAddress, out var parsedIp))
                return false;

            if (parsedIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            var bytes = parsedIp.GetAddressBytes();

            // 检查私有IP地址范围
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            return false;
        }
    }

    /// <summary>
    /// 检查是否为保留地址
    /// </summary>
    private bool IsReservedAddress(IPAddress ipAddress)
    {
        if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;

        var bytes = ipAddress.GetAddressBytes();

        // 0.0.0.0/8 - 保留地址
        if (bytes[0] == 0)
            return true;

        // 127.0.0.0/8 - 环回地址
        if (bytes[0] == 127)
            return false; // 环回地址是有效的

        // 169.254.0.0/16 - 链路本地地址
        if (bytes[0] == 169 && bytes[1] == 254)
            return true;

        // 224.0.0.0/4 - 组播地址
        if (bytes[0] >= 224 && bytes[0] <= 239)
            return true;

        // 240.0.0.0/4 - 保留地址
        if (bytes[0] >= 240)
            return true;

        return false;
    }

    /// <summary>
    /// 验证端点连通性（异步）
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒）</param>
    /// <returns>是否可连通</returns>
    public async Task<bool> IsReachableAsync(int timeoutMs = 5000)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync(IpAddress, Port);
            var timeoutTask = Task.Delay(timeoutMs);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
                return false; // 超时

            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 创建网络端点
    /// </summary>
    /// <returns>IPEndPoint实例</returns>
    public IPEndPoint ToIPEndPoint()
    {
        return new IPEndPoint(IPAddress.Parse(IpAddress), Port);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IpAddress;
        yield return Port;
    }

    public override string ToString() => $"{IpAddress}:{Port}";

    /// <summary>
    /// 解析端点字符串
    /// </summary>
    /// <param name="endpoint">端点字符串</param>
    /// <returns>IpEndpoint实例</returns>
    public static IpEndpoint Parse(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

        var parts = endpoint.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException("Invalid endpoint format. Expected format: 'ip:port'", nameof(endpoint));

        if (!int.TryParse(parts[1], out var port))
            throw new ArgumentException("Invalid port number", nameof(endpoint));

        return new IpEndpoint(parts[0], port);
    }

    /// <summary>
    /// 尝试解析端点字符串
    /// </summary>
    /// <param name="endpoint">端点字符串</param>
    /// <param name="result">解析结果</param>
    /// <returns>是否解析成功</returns>
    public static bool TryParse(string endpoint, out IpEndpoint? result)
    {
        result = null;

        try
        {
            result = Parse(endpoint);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 创建本地端点
    /// </summary>
    /// <param name="port">端口号</param>
    /// <returns>本地端点</returns>
    public static IpEndpoint Localhost(int port) => new("127.0.0.1", port);

    /// <summary>
    /// 创建任意地址端点
    /// </summary>
    /// <param name="port">端口号</param>
    /// <returns>任意地址端点</returns>
    public static IpEndpoint Any(int port) => new("0.0.0.0", port);
}

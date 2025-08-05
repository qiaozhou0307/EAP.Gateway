using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 通信配置
/// </summary>
public class CommunicationConfiguration : ValueObject
{
    /// <summary>
    /// 消息发送重试次数
    /// </summary>
    public int MessageRetryCount { get; }

    /// <summary>
    /// 消息发送重试间隔（毫秒）
    /// </summary>
    public int MessageRetryInterval { get; }

    /// <summary>
    /// 是否启用消息压缩
    /// </summary>
    public bool EnableMessageCompression { get; }

    /// <summary>
    /// 最大消息大小（字节）
    /// </summary>
    public int MaxMessageSize { get; }

    /// <summary>
    /// 是否启用消息加密
    /// </summary>
    public bool EnableMessageEncryption { get; }

    public CommunicationConfiguration(
        int messageRetryCount = 3,
        int messageRetryInterval = 1000,
        bool enableMessageCompression = false,
        int maxMessageSize = 1048576, // 1MB
        bool enableMessageEncryption = false)
    {
        MessageRetryCount = messageRetryCount >= 0 ? messageRetryCount : throw new ArgumentException("Message retry count must be non-negative", nameof(messageRetryCount));
        MessageRetryInterval = messageRetryInterval > 0 ? messageRetryInterval : throw new ArgumentException("Message retry interval must be positive", nameof(messageRetryInterval));
        EnableMessageCompression = enableMessageCompression;
        MaxMessageSize = maxMessageSize > 0 ? maxMessageSize : throw new ArgumentException("Max message size must be positive", nameof(maxMessageSize));
        EnableMessageEncryption = enableMessageEncryption;
    }

    /// <summary>
    /// 创建默认通信配置
    /// </summary>
    public static CommunicationConfiguration Default() => new();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MessageRetryCount;
        yield return MessageRetryInterval;
        yield return EnableMessageCompression;
        yield return MaxMessageSize;
        yield return EnableMessageEncryption;
    }
}

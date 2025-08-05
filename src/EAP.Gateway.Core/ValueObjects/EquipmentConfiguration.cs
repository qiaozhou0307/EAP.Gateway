using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 设备配置值对象（完整版本）
/// </summary>
public class EquipmentConfiguration : ValueObject
{
    /// <summary>
    /// 网络端点
    /// </summary>
    public IpEndpoint Endpoint { get; }

    /// <summary>
    /// HSMS超时配置
    /// </summary>
    public HsmsTimeouts Timeouts { get; }

    /// <summary>
    /// 连接模式
    /// </summary>
    public ConnectionMode ConnectionMode { get; }

    /// <summary>
    /// 是否启用自动重连
    /// </summary>
    public bool EnableAutoReconnect { get; }

    /// <summary>
    /// 重试配置
    /// </summary>
    public RetryConfiguration RetryConfig { get; }

    /// <summary>
    /// 心跳间隔（秒）
    /// </summary>
    public int HeartbeatInterval { get; }

    /// <summary>
    /// 是否启用数据采集
    /// </summary>
    public bool EnableDataCollection { get; }

    /// <summary>
    /// 数据采集配置
    /// </summary>
    public DataCollectionConfiguration? DataCollectionConfig { get; }

    /// <summary>
    /// 是否启用报警处理（新增）
    /// </summary>
    public bool EnableAlarmHandling { get; }

    /// <summary>
    /// 是否启用远程控制（新增）
    /// </summary>
    public bool EnableRemoteControl { get; }

    /// <summary>
    /// 报警配置（新增）
    /// </summary>
    public AlarmConfiguration? AlarmConfig { get; }

    /// <summary>
    /// 远程控制配置（新增）
    /// </summary>
    public RemoteControlConfiguration? RemoteControlConfig { get; }

    /// <summary>
    /// 通信配置
    /// </summary>
    public CommunicationConfiguration? CommunicationConfig { get; }

    /// <summary>
    /// 安全配置
    /// </summary>
    public SecurityConfiguration? SecurityConfig { get; }

    public EquipmentConfiguration(
        IpEndpoint endpoint,
        HsmsTimeouts? timeouts = null,
        ConnectionMode connectionMode = ConnectionMode.Active,
        bool enableAutoReconnect = true,
        RetryConfiguration? retryConfig = null,
        int heartbeatInterval = 30,
        bool enableDataCollection = true,
        DataCollectionConfiguration? dataCollectionConfig = null,
        bool enableAlarmHandling = true,
        bool enableRemoteControl = true,
        AlarmConfiguration? alarmConfig = null,
        RemoteControlConfiguration? remoteControlConfig = null,
        CommunicationConfiguration? communicationConfig = null,
        SecurityConfiguration? securityConfig = null)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Timeouts = timeouts ?? HsmsTimeouts.Default();
        ConnectionMode = connectionMode;
        EnableAutoReconnect = enableAutoReconnect;
        RetryConfig = retryConfig ?? RetryConfiguration.Default();
        HeartbeatInterval = heartbeatInterval > 0 ? heartbeatInterval : throw new ArgumentException("Heartbeat interval must be positive", nameof(heartbeatInterval));
        EnableDataCollection = enableDataCollection;
        DataCollectionConfig = dataCollectionConfig;
        EnableAlarmHandling = enableAlarmHandling;
        EnableRemoteControl = enableRemoteControl;
        AlarmConfig = alarmConfig;
        RemoteControlConfig = remoteControlConfig;
        CommunicationConfig = communicationConfig;
        SecurityConfig = securityConfig;
    }

    /// <summary>
    /// 检查配置是否有效
    /// </summary>
    public bool IsValid => Endpoint.IsValid && ValidateTimeouts() && ValidateRetryConfig() && ValidateFeatureConfigs();

    /// <summary>
    /// 验证超时配置
    /// </summary>
    private bool ValidateTimeouts()
    {
        return Timeouts.T3 > 0 && Timeouts.T5 > 0 && Timeouts.T6 > 0 &&
               Timeouts.T7 > 0 && Timeouts.T8 > 0;
    }

    /// <summary>
    /// 验证重试配置
    /// </summary>
    private bool ValidateRetryConfig()
    {
        return RetryConfig.MaxRetries >= 0 && RetryConfig.InitialDelay > 0;
    }

    /// <summary>
    /// 验证功能配置
    /// </summary>
    private bool ValidateFeatureConfigs()
    {
        // 如果启用报警处理但没有配置，使用默认配置
        if (EnableAlarmHandling && AlarmConfig == null)
        {
            // 这里可以添加默认配置验证逻辑
        }

        // 如果启用远程控制但没有配置，使用默认配置
        if (EnableRemoteControl && RemoteControlConfig == null)
        {
            // 这里可以添加默认配置验证逻辑
        }

        return true;
    }

    /// <summary>
    /// 获取验证错误
    /// </summary>
    /// <returns>验证错误列表</returns>
    public IEnumerable<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (!Endpoint.IsValid)
            errors.Add("Invalid endpoint configuration");

        if (!ValidateTimeouts())
            errors.Add("Invalid timeout configuration");

        if (!ValidateRetryConfig())
            errors.Add("Invalid retry configuration");

        if (HeartbeatInterval <= 0)
            errors.Add("Heartbeat interval must be positive");

        return errors;
    }

    /// <summary>
    /// 创建配置副本并修改报警处理设置
    /// </summary>
    /// <param name="enableAlarmHandling">是否启用报警处理</param>
    /// <param name="alarmConfig">报警配置</param>
    /// <returns>新的配置实例</returns>
    public EquipmentConfiguration WithAlarmHandling(bool enableAlarmHandling, AlarmConfiguration? alarmConfig = null)
    {
        return new EquipmentConfiguration(
            Endpoint, Timeouts, ConnectionMode, EnableAutoReconnect, RetryConfig,
            HeartbeatInterval, EnableDataCollection, DataCollectionConfig,
            enableAlarmHandling, EnableRemoteControl, alarmConfig ?? AlarmConfig,
            RemoteControlConfig, CommunicationConfig, SecurityConfig);
    }

    /// <summary>
    /// 创建配置副本并修改远程控制设置
    /// </summary>
    /// <param name="enableRemoteControl">是否启用远程控制</param>
    /// <param name="remoteControlConfig">远程控制配置</param>
    /// <returns>新的配置实例</returns>
    public EquipmentConfiguration WithRemoteControl(bool enableRemoteControl, RemoteControlConfiguration? remoteControlConfig = null)
    {
        return new EquipmentConfiguration(
            Endpoint, Timeouts, ConnectionMode, EnableAutoReconnect, RetryConfig,
            HeartbeatInterval, EnableDataCollection, DataCollectionConfig,
            EnableAlarmHandling, enableRemoteControl, AlarmConfig,
            remoteControlConfig ?? RemoteControlConfig, CommunicationConfig, SecurityConfig);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Endpoint;
        yield return Timeouts;
        yield return ConnectionMode;
        yield return EnableAutoReconnect;
        yield return RetryConfig;
        yield return HeartbeatInterval;
        yield return EnableDataCollection;
        yield return DataCollectionConfig ?? new object();
        yield return EnableAlarmHandling;
        yield return EnableRemoteControl;
        yield return AlarmConfig ?? new object();
        yield return RemoteControlConfig ?? new object();
        yield return CommunicationConfig ?? new object();
        yield return SecurityConfig ?? new object();
    }
}

using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 远程控制配置
/// </summary>
public class RemoteControlConfiguration : ValueObject
{
    /// <summary>
    /// 命令执行超时（秒）
    /// </summary>
    public int CommandTimeout { get; }

    /// <summary>
    /// 最大并发命令数
    /// </summary>
    public int MaxConcurrentCommands { get; }

    /// <summary>
    /// 是否需要操作员确认
    /// </summary>
    public bool RequireOperatorConfirmation { get; }

    /// <summary>
    /// 允许的命令列表
    /// </summary>
    public IReadOnlyList<string> AllowedCommands { get; }

    /// <summary>
    /// 禁止的命令列表
    /// </summary>
    public IReadOnlyList<string> DeniedCommands { get; }

    /// <summary>
    /// 命令执行日志级别
    /// </summary>
    public LogLevel CommandLogLevel { get; }

    public RemoteControlConfiguration(
        int commandTimeout = 60,
        int maxConcurrentCommands = 5,
        bool requireOperatorConfirmation = false,
        IEnumerable<string>? allowedCommands = null,
        IEnumerable<string>? deniedCommands = null,
        LogLevel commandLogLevel = LogLevel.Information)
    {
        CommandTimeout = commandTimeout > 0 ? commandTimeout : throw new ArgumentException("Command timeout must be positive", nameof(commandTimeout));
        MaxConcurrentCommands = maxConcurrentCommands > 0 ? maxConcurrentCommands : throw new ArgumentException("Max concurrent commands must be positive", nameof(maxConcurrentCommands));
        RequireOperatorConfirmation = requireOperatorConfirmation;
        AllowedCommands = allowedCommands?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        DeniedCommands = deniedCommands?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        CommandLogLevel = commandLogLevel;
    }

    /// <summary>
    /// 检查命令是否被允许
    /// </summary>
    public bool IsCommandAllowed(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        // 如果在禁止列表中，直接拒绝
        if (DeniedCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
            return false;

        // 如果允许列表为空，默认允许所有命令
        if (!AllowedCommands.Any())
            return true;

        // 检查是否在允许列表中
        return AllowedCommands.Contains(command, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 创建默认远程控制配置
    /// </summary>
    public static RemoteControlConfiguration Default() => new();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CommandTimeout;
        yield return MaxConcurrentCommands;
        yield return RequireOperatorConfirmation;
        yield return CommandLogLevel;

        foreach (var cmd in AllowedCommands)
            yield return cmd;

        foreach (var cmd in DeniedCommands)
            yield return cmd;
    }
}

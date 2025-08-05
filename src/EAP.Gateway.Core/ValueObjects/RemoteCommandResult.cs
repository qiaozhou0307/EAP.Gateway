namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 远程命令执行结果
/// </summary>
public class RemoteCommandResult
{
    /// <summary>
    /// 命令ID
    /// </summary>
    public Guid CommandId { get; }

    /// <summary>
    /// 命令名称
    /// </summary>
    public string CommandName { get; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccessful { get; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// 执行时间
    /// </summary>
    public TimeSpan ExecutionTime { get; }

    /// <summary>
    /// 结果消息
    /// </summary>
    public string? ResultMessage { get; }

    /// <summary>
    /// 结果数据
    /// </summary>
    public IReadOnlyDictionary<string, object>? ResultData { get; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime CompletedAt { get; }

    private RemoteCommandResult(
        Guid commandId,
        string commandName,
        bool isSuccessful,
        TimeSpan executionTime,
        string? resultMessage = null,
        string? errorMessage = null,
        IReadOnlyDictionary<string, object>? resultData = null)
    {
        CommandId = commandId;
        CommandName = commandName;
        IsSuccessful = isSuccessful;
        ExecutionTime = executionTime;
        ResultMessage = resultMessage;
        ErrorMessage = errorMessage;
        ResultData = resultData;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static RemoteCommandResult Success(
        Guid commandId,
        string commandName,
        TimeSpan executionTime,
        string? resultMessage = null,
        IReadOnlyDictionary<string, object>? resultData = null)
    {
        return new RemoteCommandResult(commandId, commandName, true, executionTime, resultMessage, null, resultData);
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static RemoteCommandResult Failure(
        Guid commandId,
        string commandName,
        string errorMessage,
        TimeSpan executionTime,
        IReadOnlyDictionary<string, object>? resultData = null)
    {
        return new RemoteCommandResult(commandId, commandName, false, executionTime, null, errorMessage, resultData);
    }

    /// <summary>
    /// 创建超时结果
    /// </summary>
    public static RemoteCommandResult Timeout(
        Guid commandId,
        string commandName,
        TimeSpan executionTime)
    {
        return new RemoteCommandResult(commandId, commandName, false, executionTime, null, "Command execution timeout");
    }
}

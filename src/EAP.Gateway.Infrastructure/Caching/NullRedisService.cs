using EAP.Gateway.Core.Repositories; // ✅ 只引用Core层接口
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// 空Redis服务实现 - 当Redis不可用时的占位符
/// 实现Core.Repositories.IRedisService接口
/// </summary>
public class NullRedisService : Core.Repositories.IRedisService
{
    private readonly ILogger<NullRedisService> _logger;

    public bool IsConnected => false;

    public NullRedisService(ILogger<NullRedisService> logger)
    {
        _logger = logger;
    }

    // 实现Core层接口的所有方法，返回空或默认值
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("NullRedisService: GetAsync<{Type}> called for key {Key}", typeof(T).Name, key);
        return Task.FromResult<T?>(null);
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("NullRedisService: SetAsync<{Type}> called for key {Key}", typeof(T).Name, key);
        return Task.FromResult(false);
    }

    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRedisService: DeleteAsync called for key {Key}", key);
        return Task.FromResult(false);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRedisService: ExistsAsync called for key {Key}", key);
        return Task.FromResult(false);
    }

    public Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("NullRedisService: GetMultipleAsync<{Type}> called", typeof(T).Name);
        return Task.FromResult(new Dictionary<string, T?>());
    }

    public Task SetMultipleAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("NullRedisService: SetMultipleAsync<{Type}> called", typeof(T).Name);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRedisService: GetKeysAsync called for pattern {Pattern}", pattern);
        return Task.FromResult(Enumerable.Empty<string>());
    }

    // Infrastructure层扩展方法
    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRedisService: GetStringAsync called for key {Key}", key);
        return Task.FromResult<string?>(null);
    }

    public Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRedisService: SetStringAsync called for key {Key}", key);
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRedisService: RemoveAsync called for key {Key}", key);
        return Task.FromResult(false);
    }

    public Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRedisService: IncrementAsync called for key {Key}", key);
        return Task.FromResult(0L);
    }

    public Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRedisService: DecrementAsync called for key {Key}", key);
        return Task.FromResult(0L);
    }

    public Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRedisService: PingAsync called");
        return Task.FromResult(false);
    }
}

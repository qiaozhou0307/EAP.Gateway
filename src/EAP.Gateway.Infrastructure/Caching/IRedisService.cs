namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// Redis服务接口
/// </summary>
public interface IRedisService
{
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);
    Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);
}

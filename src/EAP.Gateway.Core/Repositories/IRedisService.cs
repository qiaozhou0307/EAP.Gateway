namespace EAP.Gateway.Core.Repositories;

/// <summary>
/// Redis缓存服务接口
/// 支持FR-DAM-003需求：实时数据缓存
/// </summary>
public interface IRedisService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class;
    Task SetMultipleAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    Task<IEnumerable<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default);
}

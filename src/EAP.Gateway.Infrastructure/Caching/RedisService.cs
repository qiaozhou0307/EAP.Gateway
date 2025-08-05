using System.Text.Json;
using System.Text.Json.Serialization;
using EAP.Gateway.Core.Repositories; // ✅ 只引用Core层接口
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// Redis缓存服务实现
/// 实现Core.Repositories.IRedisService接口
/// </summary>
public sealed class RedisService : Core.Repositories.IRedisService, IDisposable
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public bool IsConnected => _connectionMultiplexer.IsConnected;

    public RedisService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _database = _connectionMultiplexer.GetDatabase();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    #region Core层接口实现

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedValue))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Redis数据反序列化失败, Key: {Key}, Type: {Type}", key, typeof(T).Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Redis获取数据失败, Key: {Key}", key);
            return null;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis数据设置失败, Key: {Key}, Type: {Type}", key, typeof(T).Name);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis数据删除失败, Key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis键存在检查失败, Key: {Key}", key);
            return false;
        }
    }

    public async Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keys);

        var keysList = keys.ToList();
        if (!keysList.Any())
        {
            return new Dictionary<string, T?>();
        }

        var result = new Dictionary<string, T?>();

        try
        {
            var tasks = keysList.Select(async key =>
            {
                var value = await GetAsync<T>(key, cancellationToken);
                return new KeyValuePair<string, T?>(key, value);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var kvp in results)
            {
                result[kvp.Key] = kvp.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis批量获取失败, Keys: {Keys}", string.Join(", ", keysList));
        }

        return result;
    }

    public async Task SetMultipleAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyValuePairs);

        if (!keyValuePairs.Any())
        {
            return;
        }

        var tasks = keyValuePairs.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        try
        {
            var server = GetAvailableServer();
            var keys = new List<string>();

            await foreach (var key in server.KeysAsync(
                database: _database.Database,
                pattern: pattern,
                pageSize: 1000))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                keys.Add(key.ToString());
            }

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis模式匹配失败, Pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    #endregion

    #region Infrastructure层扩展方法实现

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            return await _distributedCache.GetStringAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Redis获取字符串失败, Key: {Key}", key);
            return null;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.SetAbsoluteExpiration(expiry.Value);
            }

            await _distributedCache.SetStringAsync(key, value, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis字符串设置失败, Key: {Key}", key);
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(key, cancellationToken); // 复用DeleteAsync实现
    }

    public async Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            return await _database.StringIncrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis递增操作失败, Key: {Key}, Value: {Value}", key, value);
            return 0;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            return await _database.StringDecrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis递减操作失败, Key: {Key}, Value: {Value}", key, value);
            return 0;
        }
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await _database.PingAsync();
            return latency != TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis Ping失败");
            return false;
        }
    }

    #endregion

    #region Private Methods

    private IServer GetAvailableServer()
    {
        var endpoints = _connectionMultiplexer.GetEndPoints();

        if (!endpoints.Any())
        {
            throw new InvalidOperationException("Redis连接中没有可用的端点");
        }

        // 查找连接的主服务器
        foreach (var endpoint in endpoints)
        {
            var server = _connectionMultiplexer.GetServer(endpoint);
            if (server.IsConnected && !server.IsReplica)
            {
                return server;
            }
        }

        // 如果没有主服务器，使用任何连接的服务器
        foreach (var endpoint in endpoints)
        {
            var server = _connectionMultiplexer.GetServer(endpoint);
            if (server.IsConnected)
            {
                return server;
            }
        }

        throw new InvalidOperationException("没有可用的Redis服务器连接");
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #endregion
}

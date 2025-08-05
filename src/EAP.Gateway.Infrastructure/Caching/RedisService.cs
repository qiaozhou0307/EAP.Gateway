// 文件路径: src/EAP.Gateway.Infrastructure/Caching/RedisService.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Core.Services;
using EAP.Gateway.Infrastructure.Communications.SecsGem;
using EAP.Gateway.Infrastructure.Configuration;
using EAP.Gateway.Infrastructure.Messaging.Kafka;
using EAP.Gateway.Infrastructure.Persistence;
using EAP.Gateway.Infrastructure.Persistence.Contexts;
using EAP.Gateway.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// Redis缓存服务实现（生产就绪版本）
/// 支持FR-DAM-003需求：实时数据缓存(Redis)
/// </summary>
public sealed class RedisService : IRedisService, IDisposable
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

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

    #region IRedisService Implementation

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
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Redis数据序列化失败, Key: {Key}, Type: {Type}", key, typeof(T).Name);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向Redis设置数据失败, Key: {Key}", key);
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
            _logger.LogError(ex, "从Redis删除数据失败, Key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var exists = await _database.KeyExistsAsync(key);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查Redis键是否存在失败, Key: {Key}", key);
            return false;
        }
    }

    public async Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keys);

        var keyList = keys.ToList();
        var result = new Dictionary<string, T?>();

        if (!keyList.Any())
        {
            return result;
        }

        // 简化的并发获取
        var tasks = keyList.Select(async key =>
        {
            var value = await GetAsync<T>(key, cancellationToken);
            return new { Key = key, Value = value };
        });

        try
        {
            var results = await Task.WhenAll(tasks);
            foreach (var item in results)
            {
                result[item.Key] = item.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取Redis数据失败");
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

    /// <summary>
    /// 实现GetKeysAsync方法 - 核心修复
    /// </summary>
    public async Task<IEnumerable<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        try
        {
            var server = GetAvailableServer();
            var keys = new List<string>();

            // 使用SCAN命令，性能更好且不会阻塞Redis
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

            _logger.LogDebug("Redis模式匹配完成, Pattern: {Pattern}, Found: {Count}", pattern, keys.Count);
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis模式匹配失败, Pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 获取可用的Redis服务器实例
    /// </summary>
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
            if (server.IsConnected && !server.IsReplica) // 修复：使用IsReplica
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
        // IConnectionMultiplexer由DI容器管理
        GC.SuppressFinalize(this);
    }

    #endregion
}


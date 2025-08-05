using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// 增强版Redis缓存服务实现（包含监控和性能优化）
/// 支持FR-DAM-003需求：实时数据缓存(Redis)
/// </summary>
public class EnhancedRedisService : IRedisService, IDisposable
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly ILogger<EnhancedRedisService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RedisOptions _options;
    private readonly RedisMetrics _metrics;

    public EnhancedRedisService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisOptions> options,
        RedisMetrics metrics,
        ILogger<EnhancedRedisService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _database = _connectionMultiplexer.GetDatabase();
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Redis缓存未命中, Key: {Key}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
            success = true;
            _logger.LogDebug("Redis缓存命中, Key: {Key}, Type: {Type}", key, typeof(T).Name);
            return result;
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
        finally
        {
            stopwatch.Stop();
            _metrics.RecordOperation("get", stopwatch.Elapsed.TotalMilliseconds, success);
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions();

            var effectiveExpiration = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
            options.SetAbsoluteExpiration(effectiveExpiration);

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            success = true;

            _logger.LogDebug("Redis数据设置成功, Key: {Key}, Type: {Type}, Expiration: {Expiration}",
                key, typeof(T).Name, effectiveExpiration);
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
        finally
        {
            stopwatch.Stop();
            _metrics.RecordOperation("set", stopwatch.Elapsed.TotalMilliseconds, success);
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            success = true;
            _logger.LogDebug("Redis数据删除成功, Key: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从Redis删除数据失败, Key: {Key}", key);
            return false;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordOperation("delete", stopwatch.Elapsed.TotalMilliseconds, success);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var exists = await _database.KeyExistsAsync(key);
            success = true;
            _logger.LogDebug("Redis键存在性检查, Key: {Key}, Exists: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查Redis键是否存在失败, Key: {Key}", key);
            return false;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordOperation("exists", stopwatch.Elapsed.TotalMilliseconds, success);
        }
    }

    public async Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keys);

        var keyList = keys.ToList();
        if (!keyList.Any())
        {
            return new Dictionary<string, T?>();
        }

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var result = new Dictionary<string, T?>();

            // 使用Redis pipeline进行批量操作优化
            var batch = _database.CreateBatch();
            var tasks = keyList.Select(key => new
            {
                Key = key,
                Task = batch.StringGetAsync(key)
            }).ToList();

            batch.Execute();

            await Task.WhenAll(tasks.Select(t => t.Task));

            foreach (var item in tasks)
            {
                var redisValue = await item.Task;
                if (redisValue.HasValue)
                {
                    try
                    {
                        var deserializedValue = JsonSerializer.Deserialize<T>(redisValue!, _jsonOptions);
                        result[item.Key] = deserializedValue;
                    }
                    catch (JsonException)
                    {
                        result[item.Key] = null;
                    }
                }
                else
                {
                    result[item.Key] = null;
                }
            }

            success = true;
            _logger.LogDebug("批量获取Redis数据完成, Keys: {KeyCount}, Found: {FoundCount}",
                keyList.Count, result.Values.Count(v => v != null));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取Redis数据失败, Keys: {Keys}", string.Join(",", keyList));
            return new Dictionary<string, T?>();
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordOperation("get_multiple", stopwatch.Elapsed.TotalMilliseconds, success);
        }
    }

    public async Task SetMultipleAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(keyValuePairs);

        if (!keyValuePairs.Any())
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            // 使用Redis事务进行批量设置
            var transaction = _database.CreateTransaction();
            var effectiveExpiration = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

            var tasks = new List<Task<bool>>();

            foreach (var kvp in keyValuePairs)
            {
                var serializedValue = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
                var task = transaction.StringSetAsync(kvp.Key, serializedValue, effectiveExpiration);
                tasks.Add(task);
            }

            var executed = await transaction.ExecuteAsync();
            if (executed)
            {
                await Task.WhenAll(tasks);
                success = true;
                _logger.LogDebug("批量设置Redis数据成功, Count: {Count}", keyValuePairs.Count);
            }
            else
            {
                _logger.LogWarning("Redis事务执行失败，批量设置数据失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量设置Redis数据失败, Count: {Count}", keyValuePairs.Count);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordOperation("set_multiple", stopwatch.Elapsed.TotalMilliseconds, success);
        }
    }

    /// <summary>
    /// 实现GetKeysAsync方法 - 使用SCAN命令优化性能
    /// </summary>
    public async Task<IEnumerable<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var server = GetHealthyServer();
            var keys = new List<string>();

            // 使用SCAN命令代替KEYS命令，避免阻塞Redis
            await foreach (var key in server.KeysAsync(
                database: _database.Database,
                pattern: pattern,
                pageSize: _options.KeyScanBatchSize))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Redis键扫描被取消, Pattern: {Pattern}", pattern);
                    break;
                }

                keys.Add(key.ToString());
            }

            success = true;
            _logger.LogDebug("Redis模式匹配完成, Pattern: {Pattern}, Found: {Count}", pattern, keys.Count);
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis模式匹配失败, Pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordOperation("scan_keys", stopwatch.Elapsed.TotalMilliseconds, success);
        }
    }

    /// <summary>
    /// 获取健康的Redis服务器实例
    /// </summary>
    private IServer GetHealthyServer()
    {
        var endpoints = _connectionMultiplexer.GetEndPoints();
        if (!endpoints.Any())
        {
            throw new InvalidOperationException("Redis连接中没有可用的端点");
        }

        // 优先选择连接且健康的服务器
        foreach (var endpoint in endpoints)
        {
            var server = _connectionMultiplexer.GetServer(endpoint);
            // 修复：使用IsReplica代替已过时的IsSlave
            if (server.IsConnected && !server.IsReplica)
            {
                return server;
            }
        }

        // 如果没有主服务器，选择任何连接的服务器
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

    public void Dispose()
    {
        _metrics?.Dispose();
        GC.SuppressFinalize(this);
    }
}

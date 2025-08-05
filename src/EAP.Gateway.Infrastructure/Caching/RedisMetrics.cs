using System.Diagnostics.Metrics;
using StackExchange.Redis;

namespace EAP.Gateway.Infrastructure.Caching;

/// <summary>
/// Redis性能指标
/// </summary>
public class RedisMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Counter<long> _operationCounter;
    private readonly Histogram<double> _operationDuration;
    private readonly UpDownCounter<long> _connectionCount;

    public RedisMetrics()
    {
        _meter = new Meter("EAP.Gateway.Redis", "1.0.0");

        _operationCounter = _meter.CreateCounter<long>(
            "redis_operations_total",
            "Total number of Redis operations");

        _operationDuration = _meter.CreateHistogram<double>(
            "redis_operation_duration_ms",
            "Duration of Redis operations in milliseconds");

        _connectionCount = _meter.CreateUpDownCounter<long>(
            "redis_connections_active",
            "Number of active Redis connections");
    }

    public void RecordOperation(string operation, double durationMs, bool success)
    {
        _operationCounter.Add(1, new KeyValuePair<string, object?>("operation", operation),
                                new KeyValuePair<string, object?>("success", success));
        _operationDuration.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
    }

    public void UpdateConnectionCount(long count)
    {
        _connectionCount.Add(count);
    }

    public void Dispose()
    {
        _meter?.Dispose();
        GC.SuppressFinalize(this);
    }
}

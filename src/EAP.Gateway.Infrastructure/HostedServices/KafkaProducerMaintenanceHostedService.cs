using EAP.Gateway.Core.Repositories;
using EAP.Gateway.Infrastructure.Messaging.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.HostedServices;

/// <summary>
/// Kafka生产者维护后台服务 - 定期清理和优化Kafka连接
/// </summary>
public class KafkaProducerMaintenanceHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KafkaProducerMaintenanceHostedService> _logger;

    public KafkaProducerMaintenanceHostedService(
        IServiceProvider serviceProvider,
        ILogger<KafkaProducerMaintenanceHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka生产者维护服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync();

                // 每小时执行一次维护
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka生产者维护过程中发生异常");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Kafka生产者维护服务已停止");
    }

    private async Task PerformMaintenanceAsync()
    {
        var kafkaProducer = _serviceProvider.GetService<IKafkaProducerService>();
        if (kafkaProducer == null)
        {
            _logger.LogDebug("Kafka生产者服务未注册，跳过维护");
            return;
        }

        try
        {
            // 执行维护操作（如果KafkaProducerService支持）
            if (kafkaProducer is IMaintenanceSupport maintenanceSupport)
            {
                await maintenanceSupport.PerformMaintenanceAsync();
                _logger.LogDebug("Kafka生产者维护操作完成");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行Kafka生产者维护时发生异常");
        }
    }
}

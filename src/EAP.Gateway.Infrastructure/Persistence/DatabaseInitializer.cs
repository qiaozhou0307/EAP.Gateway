using EAP.Gateway.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EAP.Gateway.Infrastructure.Persistence;

/// <summary>
/// 数据库初始化器接口
/// </summary>
public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 数据库初始化器实现
/// </summary>
public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly EapGatewayDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(EapGatewayDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始数据库迁移");
            await _context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("数据库迁移完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库迁移失败");
            throw;
        }
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始数据库种子数据");

            // 检查是否已有数据
            if (await _context.Equipment.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("数据库已包含数据，跳过种子数据");
                return;
            }

            // 这里可以添加种子数据逻辑
            _logger.LogInformation("数据库种子数据完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库种子数据失败");
            throw;
        }
    }
}

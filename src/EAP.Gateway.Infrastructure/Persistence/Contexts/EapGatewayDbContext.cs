using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Entities;
using EAP.Gateway.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EAP.Gateway.Infrastructure.Persistence.Contexts;

/// <summary>
/// EAP Gateway 数据库上下文
/// </summary>
public class EapGatewayDbContext : DbContext
{
    public EapGatewayDbContext(DbContextOptions<EapGatewayDbContext> options) : base(options)
    {
    }

    // DbSet 定义
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<AlarmEvent> AlarmEvents => Set<AlarmEvent>();
    public DbSet<TraceData> TraceData => Set<TraceData>();
    public DbSet<RemoteCommand> RemoteCommands => Set<RemoteCommand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 应用实体配置 - 修复：使用重命名后的配置类
        modelBuilder.ApplyConfiguration(new EquipmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AlarmEventConfiguration());
        // modelBuilder.ApplyConfiguration(new TraceDataConfiguration());
        // modelBuilder.ApplyConfiguration(new RemoteCommandConfiguration());

        // 全局配置
        ConfigureGlobalSettings(modelBuilder);
    }

    private static void ConfigureGlobalSettings(ModelBuilder modelBuilder)
    {
        // 设置默认字符串长度
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string) && property.GetMaxLength() == null)
                {
                    property.SetMaxLength(255);
                }
            }
        }

        // 设置默认小数精度
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,6)");
        }
    }
}

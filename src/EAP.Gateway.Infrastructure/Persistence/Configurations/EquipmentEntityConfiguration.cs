using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.ValueObjects;
using System.Text.Json;

namespace EAP.Gateway.Infrastructure.Persistence.Configurations;

/// <summary>
/// 设备实体配置 - 支持新增基础信息属性
/// </summary>
public class EquipmentEntityConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipment");

        // 主键配置
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => EquipmentId.Create(value))
            .HasMaxLength(50);

        // 基本属性
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        // 新增：设备基础信息属性
        builder.Property(e => e.Manufacturer)
            .HasMaxLength(100)
            .HasColumnName("Manufacturer");

        builder.Property(e => e.Model)
            .HasMaxLength(100)
            .HasColumnName("Model");

        builder.Property(e => e.SerialNumber)
            .HasMaxLength(100)
            .HasColumnName("SerialNumber");

        builder.Property(e => e.DataCollectionInterval)
            .HasColumnName("DataCollectionInterval");

        builder.Property(e => e.EnableAlarmCollection)
            .HasColumnName("EnableAlarmCollection")
            .HasDefaultValue(true);

        // 其他基本属性
        builder.Property(e => e.SubState)
            .HasMaxLength(100);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        // 枚举属性
        builder.Property(e => e.State)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.HealthStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        // 时间戳属性
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.LastHeartbeat);
        builder.Property(e => e.LastDataUpdate);

        // 配置 EquipmentConfiguration 值对象
        builder.OwnsOne(e => e.Configuration, config =>
        {
            // 配置 Endpoint 嵌套值对象
            config.OwnsOne(c => c.Endpoint, endpoint =>
            {
                endpoint.Property(ep => ep.IpAddress)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasColumnName("Endpoint_IpAddress");

                endpoint.Property(ep => ep.Port)
                    .IsRequired()
                    .HasColumnName("Endpoint_Port");
            });

            // 配置 Timeouts 嵌套值对象
            config.OwnsOne(c => c.Timeouts, timeouts =>
            {
                timeouts.Property(t => t.T3)
                    .HasColumnName("T3Timeout")
                    .HasDefaultValue(45000);

                timeouts.Property(t => t.T5)
                    .HasColumnName("T5Timeout")
                    .HasDefaultValue(10000);

                timeouts.Property(t => t.T6)
                    .HasColumnName("T6Timeout")
                    .HasDefaultValue(5000);

                timeouts.Property(t => t.T7)
                    .HasColumnName("T7Timeout")
                    .HasDefaultValue(10000);

                timeouts.Property(t => t.T8)
                    .HasColumnName("T8Timeout")
                    .HasDefaultValue(6000);
            });

            // 配置 RetryConfig 嵌套值对象
            config.OwnsOne(c => c.RetryConfig, retry =>
            {
                retry.Property(r => r.MaxRetries)
                    .HasColumnName("RetryCount")
                    .HasDefaultValue(3);

                retry.Property(r => r.InitialDelay)
                    .HasColumnName("RetryInterval")
                    .HasDefaultValue(1000);

                retry.Property(r => r.DelayMultiplier)
                    .HasColumnName("RetryDelayMultiplier")
                    .HasDefaultValue(2.0);

                retry.Property(r => r.MaxDelay)
                    .HasColumnName("RetryMaxDelay")
                    .HasDefaultValue(30000);

                retry.Property(r => r.EnableJitter)
                    .HasColumnName("RetryEnableJitter")
                    .HasDefaultValue(true);
            });

            // 扁平属性
            config.Property(c => c.ConnectionMode)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(ConnectionMode.Active);

            config.Property(c => c.EnableAutoReconnect)
                .HasDefaultValue(true);

            config.Property(c => c.HeartbeatInterval)
                .HasDefaultValue(30);

            config.Property(c => c.EnableDataCollection)
                .HasDefaultValue(true);

            config.Property(c => c.EnableAlarmHandling)
                .HasDefaultValue(true);

            config.Property(c => c.EnableRemoteControl)
                .HasDefaultValue(true);

            // 可选配置对象（使用 JSON 序列化）
            config.Property(c => c.DataCollectionConfig)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<DataCollectionConfiguration>(v, (JsonSerializerOptions?)null))
                .HasColumnName("DataCollectionConfigJson")
                .HasMaxLength(2000);

            config.Property(c => c.AlarmConfig)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<AlarmConfiguration>(v, (JsonSerializerOptions?)null))
                .HasColumnName("AlarmConfigJson")
                .HasMaxLength(2000);

            config.Property(c => c.RemoteControlConfig)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<RemoteControlConfiguration>(v, (JsonSerializerOptions?)null))
                .HasColumnName("RemoteControlConfigJson")
                .HasMaxLength(2000);
        });

        // 配置 ConnectionState 值对象
        builder.OwnsOne(e => e.ConnectionState, state =>
        {
            state.Property(s => s.IsConnected)
                .HasColumnName("IsConnected");

            state.Property(s => s.LastConnectedAt)
                .HasColumnName("LastConnectedAt");

            state.Property(s => s.LastDisconnectedAt)
                .HasColumnName("LastDisconnectedAt");

            state.Property(s => s.DisconnectReason)
                .HasMaxLength(500)
                .HasColumnName("DisconnectReason");

            state.Property(s => s.RetryCount)
                .HasColumnName("ConnectionRetryCount");

            state.Property(s => s.MaxRetries)
                .HasColumnName("ConnectionMaxRetries");

            state.Property(s => s.SessionId)
                .HasMaxLength(100)
                .HasColumnName("SessionId");

            state.Property(s => s.Quality)
                .HasConversion<string>()
                .HasColumnName("ConnectionQuality");

            state.Property(s => s.LastHeartbeatAt)
                .HasColumnName("LastHeartbeatAt");

            state.Property(s => s.ConnectionStartedAt)
                .HasColumnName("ConnectionStartedAt");
        });

        // 配置 Metrics 值对象
        builder.OwnsOne(e => e.Metrics, metrics =>
        {
            metrics.Property(m => m.TotalProcessed)
                .HasColumnName("TotalProcessed");

            metrics.Property(m => m.SuccessCount)
                .HasColumnName("SuccessCount");

            metrics.Property(m => m.FailureCount)
                .HasColumnName("FailureCount");

            metrics.Property(m => m.AverageProcessingTime)
                .HasColumnName("AverageProcessingTime");

            metrics.Property(m => m.LastResetAt)
                .HasColumnName("MetricsLastResetAt");
        });

        // 忽略运行时属性
        builder.Ignore(e => e.DomainEvents);
        builder.Ignore(e => e.ActiveAlarms);
        builder.Ignore(e => e.RecentTraceData);
        builder.Ignore(e => e.CommandHistory);

        // 索引
        builder.HasIndex(e => e.Name).HasDatabaseName("IX_Equipment_Name");
        builder.HasIndex(e => e.State).HasDatabaseName("IX_Equipment_State");
        builder.HasIndex(e => e.HealthStatus).HasDatabaseName("IX_Equipment_HealthStatus");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Equipment_CreatedAt");

        // 新增：为新属性创建索引
        builder.HasIndex(e => e.Manufacturer).HasDatabaseName("IX_Equipment_Manufacturer");
        builder.HasIndex(e => e.Model).HasDatabaseName("IX_Equipment_Model");
        builder.HasIndex(e => e.SerialNumber).HasDatabaseName("IX_Equipment_SerialNumber");

        // 复合索引
        builder.HasIndex(e => new { e.State, e.HealthStatus }).HasDatabaseName("IX_Equipment_State_HealthStatus");
        builder.HasIndex(e => new { e.Manufacturer, e.Model }).HasDatabaseName("IX_Equipment_Manufacturer_Model");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EAP.Gateway.Core.Entities;

namespace EAP.Gateway.Infrastructure.Persistence.Configurations;

/// <summary>
/// 报警事件实体配置
/// </summary>
public class AlarmEventConfiguration : IEntityTypeConfiguration<AlarmEvent>
{
    public void Configure(EntityTypeBuilder<AlarmEvent> builder)
    {
        builder.ToTable("AlarmEvents");

        // 主键
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
               .ValueGeneratedOnAdd();

        // 基本属性
        builder.Property(e => e.EquipmentId)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.AlarmId)
               .IsRequired();

        builder.Property(e => e.AlarmText)
               .HasMaxLength(500);

        builder.Property(e => e.AlarmCode)
               .HasMaxLength(100);

        // 枚举转换
        builder.Property(e => e.Severity)
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(e => e.State)
               .HasConversion<string>()
               .HasMaxLength(20);

        // 时间戳
        builder.Property(e => e.SetTime)
               .IsRequired();

        builder.Property(e => e.ClearTime);

        builder.Property(e => e.CreatedAt)
               .IsRequired();

        // 索引
        builder.HasIndex(e => e.EquipmentId);
        builder.HasIndex(e => e.AlarmId);
        builder.HasIndex(e => e.Severity);
        builder.HasIndex(e => e.State);
        builder.HasIndex(e => e.SetTime);
        builder.HasIndex(e => new { e.EquipmentId, e.State });
    }
}

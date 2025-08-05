using AutoMapper;
using EAP.Gateway.Application.DTOs;
using EAP.Gateway.Core.Aggregates.EquipmentAggregate;
using EAP.Gateway.Core.Models;

/// <summary>
/// 简化后的设备映射配置 - 只处理复杂映射
/// 简单映射建议使用 MappingExtensions
/// </summary>
public class EquipmentMappingProfile : Profile
{
    public EquipmentMappingProfile()
    {
        // 只保留复杂的批量映射，简单映射使用扩展方法
        CreateMap<Equipment, EquipmentStatusDto>()
            .ForMember(dest => dest.EquipmentId, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State.ToString()))
            .ForMember(dest => dest.ConnectionState, opt => opt.MapFrom(src => GetConnectionStateString(src.ConnectionState)))
            .ForMember(dest => dest.HealthStatus, opt => opt.MapFrom(src => src.HealthStatus.ToString()));

        // 用于批量操作的映射
        CreateMap<IEnumerable<EquipmentStatus>, IEnumerable<EquipmentStatusDto>>();
    }

    private static string GetConnectionStateString(EAP.Gateway.Core.ValueObjects.ConnectionState connectionState)
    {
        return connectionState.IsConnected ? "Connected" : "Disconnected";
    }
}

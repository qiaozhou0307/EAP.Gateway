using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 裂片机元数据值对象 - 安全存储设备信息
/// </summary>
public class DicingMachineMetadata : ValueObject
{
    /// <summary>
    /// 裂片机编号 (例如: "001", "002")
    /// </summary>
    public string MachineNumber { get; }

    /// <summary>
    /// 裂片机版本 (例如: "V2.1.0")
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// 制造商
    /// </summary>
    public string Manufacturer { get; }

    /// <summary>
    /// 设备型号
    /// </summary>
    public string Model { get; }

    /// <summary>
    /// 设备序列号
    /// </summary>
    public string SerialNumber { get; }

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime RegisteredAt { get; }

    /// <summary>
    /// 设备能力特性
    /// </summary>
    public DicingMachineCapabilities Capabilities { get; }

    /// <summary>
    /// 扩展属性 (JSON格式存储)
    /// </summary>
    public IReadOnlyDictionary<string, string> ExtendedProperties { get; }

    public DicingMachineMetadata(
        string machineNumber,
        string version,
        string manufacturer,
        string model,
        string serialNumber,
        DateTime registeredAt,
        DicingMachineCapabilities? capabilities = null,
        Dictionary<string, string>? extendedProperties = null)
    {
        MachineNumber = ValidateMachineNumber(machineNumber);
        Version = ValidateVersion(version);
        Manufacturer = manufacturer ?? "Unknown";
        Model = model ?? "Unknown";
        SerialNumber = serialNumber ?? "Unknown";
        RegisteredAt = registeredAt;
        Capabilities = capabilities ?? DicingMachineCapabilities.Default();
        ExtendedProperties = extendedProperties?.ToDictionary(k => k.Key, v => v.Value) ??
                           new Dictionary<string, string>();
    }

    /// <summary>
    /// 验证裂片机编号格式
    /// </summary>
    private static string ValidateMachineNumber(string machineNumber)
    {
        if (string.IsNullOrWhiteSpace(machineNumber))
            throw new ArgumentException("裂片机编号不能为空", nameof(machineNumber));

        // 标准化编号格式 (确保3位数字)
        if (System.Text.RegularExpressions.Regex.IsMatch(machineNumber, @"^\d{1,3}$"))
        {
            return machineNumber.PadLeft(3, '0');
        }

        // 如果包含前缀，提取数字部分
        var match = System.Text.RegularExpressions.Regex.Match(machineNumber, @"(\d{1,3})");
        if (match.Success)
        {
            return match.Groups[1].Value.PadLeft(3, '0');
        }

        throw new ArgumentException($"裂片机编号格式无效: {machineNumber}", nameof(machineNumber));
    }

    /// <summary>
    /// 验证版本格式
    /// </summary>
    private static string ValidateVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return "V1.0.0";

        // 检查是否已是标准格式
        if (System.Text.RegularExpressions.Regex.IsMatch(version, @"^V\d+\.\d+\.\d+$"))
            return version;

        throw new ArgumentException($"裂片机版本格式无效: {version}，应为 VX.Y.Z 格式", nameof(version));
    }

    /// <summary>
    /// 生成完整设备标识
    /// </summary>
    public string GetFullDeviceId() => $"DICER_{MachineNumber}";

    /// <summary>
    /// 生成显示名称
    /// </summary>
    public string GetDisplayName() => $"裂片机 #{MachineNumber} ({Version})";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MachineNumber;
        yield return Version;
        yield return Manufacturer;
        yield return Model;
        yield return SerialNumber;
        yield return RegisteredAt;
    }
}

/// <summary>
/// 裂片机能力特性
/// </summary>
public class DicingMachineCapabilities : ValueObject
{
    /// <summary>
    /// 最大晶圆尺寸 (英寸)
    /// </summary>
    public double MaxWaferSize { get; }

    /// <summary>
    /// 支持的切割类型
    /// </summary>
    public IReadOnlyList<string> SupportedCuttingTypes { get; }

    /// <summary>
    /// 最大切割速度 (mm/s)
    /// </summary>
    public double MaxCuttingSpeed { get; }

    /// <summary>
    /// 精度等级 (μm)
    /// </summary>
    public double AccuracyLevel { get; }

    /// <summary>
    /// 支持的SECS/GEM功能
    /// </summary>
    public IReadOnlyList<string> SupportedSecsGemFeatures { get; }

    public DicingMachineCapabilities(
        double maxWaferSize = 8.0,
        IEnumerable<string>? supportedCuttingTypes = null,
        double maxCuttingSpeed = 50.0,
        double accuracyLevel = 1.0,
        IEnumerable<string>? supportedSecsGemFeatures = null)
    {
        MaxWaferSize = maxWaferSize > 0 ? maxWaferSize : 8.0;
        SupportedCuttingTypes = supportedCuttingTypes?.ToList() ?? new List<string> { "Full Cut", "Half Cut" };
        MaxCuttingSpeed = maxCuttingSpeed > 0 ? maxCuttingSpeed : 50.0;
        AccuracyLevel = accuracyLevel > 0 ? accuracyLevel : 1.0;
        SupportedSecsGemFeatures = supportedSecsGemFeatures?.ToList() ??
            new List<string> { "S1F1", "S1F2", "S1F13", "S1F14", "S2F41", "S2F42", "S6F11", "S6F12" };
    }

    public static DicingMachineCapabilities Default() => new();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MaxWaferSize;
        yield return string.Join(",", SupportedCuttingTypes);
        yield return MaxCuttingSpeed;
        yield return AccuracyLevel;
        yield return string.Join(",", SupportedSecsGemFeatures);
    }
}

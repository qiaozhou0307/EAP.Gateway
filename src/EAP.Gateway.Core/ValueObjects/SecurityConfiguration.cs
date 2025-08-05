using EAP.Gateway.Core.Common;

namespace EAP.Gateway.Core.ValueObjects;

/// <summary>
/// 安全配置
/// </summary>
public class SecurityConfiguration : ValueObject
{
    /// <summary>
    /// 是否启用TLS
    /// </summary>
    public bool EnableTls { get; }

    /// <summary>
    /// TLS版本
    /// </summary>
    public string TlsVersion { get; }

    /// <summary>
    /// 证书路径
    /// </summary>
    public string? CertificatePath { get; }

    /// <summary>
    /// 是否验证证书
    /// </summary>
    public bool ValidateCertificate { get; }

    /// <summary>
    /// 允许的IP地址列表
    /// </summary>
    public IReadOnlyList<string> AllowedIpAddresses { get; }

    public SecurityConfiguration(
        bool enableTls = false,
        string tlsVersion = "1.2",
        string? certificatePath = null,
        bool validateCertificate = true,
        IEnumerable<string>? allowedIpAddresses = null)
    {
        EnableTls = enableTls;
        TlsVersion = tlsVersion ?? throw new ArgumentNullException(nameof(tlsVersion));
        CertificatePath = certificatePath;
        ValidateCertificate = validateCertificate;
        AllowedIpAddresses = allowedIpAddresses?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    /// <summary>
    /// 创建默认安全配置
    /// </summary>
    public static SecurityConfiguration Default() => new();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EnableTls;
        yield return TlsVersion;
        yield return CertificatePath ?? string.Empty;
        yield return ValidateCertificate;

        foreach (var ip in AllowedIpAddresses)
            yield return ip;
    }
}

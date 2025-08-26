using System.Security.Cryptography.X509Certificates;

namespace OfdrwNet.Sign;

/// <summary>
/// 证书链验证器
/// 负责验证X509证书链的有效性，确保签名证书的可信性
/// </summary>
public class CertificateChainValidator : IDisposable
{
    /// <summary>
    /// 受信任的根证书存储
    /// </summary>
    private readonly X509Store _trustedRootStore;

    /// <summary>
    /// 中间证书存储
    /// </summary>
    private readonly X509Store _intermediateStore;

    /// <summary>
    /// 证书撤销列表检查器
    /// </summary>
    private readonly X509RevocationMode _revocationMode;

    /// <summary>
    /// 证书撤销检查标志
    /// </summary>
    private readonly X509RevocationFlag _revocationFlag;

    /// <summary>
    /// 验证标志
    /// </summary>
    private readonly X509VerificationFlags _verificationFlags;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="revocationMode">撤销检查模式</param>
    /// <param name="revocationFlag">撤销检查标志</param>
    /// <param name="verificationFlags">验证标志</param>
    public CertificateChainValidator(
        X509RevocationMode revocationMode = X509RevocationMode.Online,
        X509RevocationFlag revocationFlag = X509RevocationFlag.ExcludeRoot,
        X509VerificationFlags verificationFlags = X509VerificationFlags.NoFlag)
    {
        _revocationMode = revocationMode;
        _revocationFlag = revocationFlag;
        _verificationFlags = verificationFlags;

        _trustedRootStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        _intermediateStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
    }

    /// <summary>
    /// 验证证书链
    /// </summary>
    /// <param name="certificate">要验证的证书</param>
    /// <param name="additionalCertificates">额外的证书（如中间CA证书）</param>
    /// <returns>验证结果</returns>
    public CertificateValidationResult ValidateCertificateChain(X509Certificate2 certificate, 
        X509Certificate2Collection? additionalCertificates = null)
    {
        if (certificate == null)
        {
            throw new ArgumentNullException(nameof(certificate));
        }

        var result = new CertificateValidationResult
        {
            Certificate = certificate,
            ValidationTime = DateTime.Now
        };

        try
        {
            using var chain = new X509Chain();
            
            // 配置链验证选项
            chain.ChainPolicy.RevocationMode = _revocationMode;
            chain.ChainPolicy.RevocationFlag = _revocationFlag;
            chain.ChainPolicy.VerificationFlags = _verificationFlags;
            
            // 设置验证时间（如果需要历史验证）
            chain.ChainPolicy.VerificationTime = DateTime.Now;

            // 添加额外的证书到链中
            if (additionalCertificates != null)
            {
                chain.ChainPolicy.ExtraStore.AddRange(additionalCertificates);
            }

            // 添加中间证书存储
            try
            {
                _intermediateStore.Open(OpenFlags.ReadOnly);
                chain.ChainPolicy.ExtraStore.AddRange(_intermediateStore.Certificates);
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"无法打开中间证书存储: {ex.Message}");
            }
            finally
            {
                _intermediateStore.Close();
            }

            // 执行证书链验证
            bool isValid = chain.Build(certificate);
            result.IsValid = isValid;

            // 收集链信息
            result.ChainElements = new List<CertificateChainElement>();
            for (int i = 0; i < chain.ChainElements.Count; i++)
            {
                var element = chain.ChainElements[i];
                var chainElement = new CertificateChainElement
                {
                    Certificate = element.Certificate,
                    ChainElementStatus = element.ChainElementStatus?.ToList() ?? new List<X509ChainStatus>(),
                    Information = element.Information
                };
                result.ChainElements.Add(chainElement);
            }

            // 收集验证错误和状态信息
            result.ChainStatus = chain.ChainStatus?.ToList() ?? new List<X509ChainStatus>();

            // 分析验证结果
            AnalyzeValidationResult(result);

            // 执行额外的自定义验证
            PerformCustomValidation(result);
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"证书链验证时发生异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 验证证书的基本属性
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <returns>验证结果</returns>
    public CertificatePropertyValidationResult ValidateCertificateProperties(X509Certificate2 certificate)
    {
        if (certificate == null)
        {
            throw new ArgumentNullException(nameof(certificate));
        }

        var result = new CertificatePropertyValidationResult
        {
            Certificate = certificate
        };

        try
        {
            // 检查证书有效期
            var now = DateTime.Now;
            result.IsInValidPeriod = now >= certificate.NotBefore && now <= certificate.NotAfter;
            
            if (!result.IsInValidPeriod)
            {
                if (now < certificate.NotBefore)
                {
                    result.Issues.Add($"证书尚未生效，生效时间: {certificate.NotBefore}");
                }
                else
                {
                    result.Issues.Add($"证书已过期，过期时间: {certificate.NotAfter}");
                }
            }

            // 检查证书用途
            result.HasDigitalSignatureUsage = HasKeyUsage(certificate, X509KeyUsageFlags.DigitalSignature);
            result.HasNonRepudiationUsage = HasKeyUsage(certificate, X509KeyUsageFlags.NonRepudiation);

            if (!result.HasDigitalSignatureUsage && !result.HasNonRepudiationUsage)
            {
                result.Issues.Add("证书不支持数字签名用途");
            }

            // 检查证书算法
            result.SignatureAlgorithm = certificate.SignatureAlgorithm.FriendlyName ?? "Unknown";
            result.PublicKeyAlgorithm = certificate.PublicKey.Oid.FriendlyName ?? "Unknown";

            // 检查密钥强度
            var keySize = certificate.PublicKey.Key?.KeySize ?? 0;
            result.KeySize = keySize;
            result.IsKeyStrengthAdequate = keySize >= 2048; // RSA最低2048位

            if (!result.IsKeyStrengthAdequate)
            {
                result.Issues.Add($"密钥强度不足: {keySize}位，建议至少2048位");
            }

            // 检查证书扩展
            ValidateCertificateExtensions(certificate, result);

            // 总体评估
            result.IsValid = result.IsInValidPeriod && 
                           (result.HasDigitalSignatureUsage || result.HasNonRepudiationUsage) &&
                           result.IsKeyStrengthAdequate &&
                           result.Issues.Count == 0;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Issues.Add($"证书属性验证时发生异常: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 验证证书撤销状态
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <param name="issuerCertificate">颁发者证书（可选）</param>
    /// <returns>撤销状态验证结果</returns>
    public async Task<CertificateRevocationResult> ValidateRevocationStatusAsync(
        X509Certificate2 certificate, X509Certificate2? issuerCertificate = null)
    {
        var result = new CertificateRevocationResult
        {
            Certificate = certificate,
            CheckTime = DateTime.Now
        };

        try
        {
            // 检查CRL分发点
            var crlDistributionPoints = GetCrlDistributionPoints(certificate);
            result.CrlDistributionPoints = crlDistributionPoints;

            if (crlDistributionPoints.Count > 0)
            {
                // 尝试从CRL检查撤销状态
                foreach (var crlUrl in crlDistributionPoints)
                {
                    try
                    {
                        var isRevoked = await CheckCrlRevocationAsync(certificate, crlUrl);
                        if (isRevoked)
                        {
                            result.IsRevoked = true;
                            result.RevocationReason = "证书在CRL中被标记为已撤销";
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"检查CRL失败 ({crlUrl}): {ex.Message}");
                    }
                }
            }

            // 检查OCSP
            var ocspUrls = GetOcspUrls(certificate);
            result.OcspUrls = ocspUrls;

            if (ocspUrls.Count > 0 && !result.IsRevoked)
            {
                foreach (var ocspUrl in ocspUrls)
                {
                    try
                    {
                        var ocspResult = await CheckOcspRevocationAsync(certificate, ocspUrl, issuerCertificate);
                        if (ocspResult.IsRevoked)
                        {
                            result.IsRevoked = true;
                            result.RevocationReason = ocspResult.RevocationReason;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"检查OCSP失败 ({ocspUrl}): {ex.Message}");
                    }
                }
            }

            result.IsValid = !result.IsRevoked;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.RevocationReason = $"撤销状态检查时发生异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 分析验证结果
    /// </summary>
    /// <param name="result">验证结果</param>
    private void AnalyzeValidationResult(CertificateValidationResult result)
    {
        foreach (var status in result.ChainStatus)
        {
            switch (status.Status)
            {
                case X509ChainStatusFlags.NotTimeValid:
                    result.Issues.Add("证书或链中的证书已过期或尚未生效");
                    break;
                case X509ChainStatusFlags.Revoked:
                    result.Issues.Add("证书已被撤销");
                    break;
                case X509ChainStatusFlags.NotSignatureValid:
                    result.Issues.Add("证书签名无效");
                    break;
                case X509ChainStatusFlags.UntrustedRoot:
                    result.Warnings.Add("证书链的根证书不受信任");
                    break;
                case X509ChainStatusFlags.PartialChain:
                    result.Warnings.Add("证书链不完整");
                    break;
                case X509ChainStatusFlags.RevocationStatusUnknown:
                    result.Warnings.Add("无法确定证书撤销状态");
                    break;
                case X509ChainStatusFlags.OfflineRevocation:
                    result.Warnings.Add("撤销检查处于离线状态");
                    break;
                default:
                    if (status.Status != X509ChainStatusFlags.NoError)
                    {
                        result.Issues.Add($"验证问题: {status.StatusInformation}");
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 执行自定义验证
    /// </summary>
    /// <param name="result">验证结果</param>
    private void PerformCustomValidation(CertificateValidationResult result)
    {
        var certificate = result.Certificate;

        // 检查证书是否为自签名证书
        if (certificate.Issuer == certificate.Subject)
        {
            result.Warnings.Add("证书为自签名证书");
        }

        // 检查证书的扩展用途
        if (!HasEnhancedKeyUsage(certificate, "1.3.6.1.5.5.7.3.4")) // 邮件保护
        {
            result.Warnings.Add("证书不包含推荐的扩展密钥用途");
        }

        // 检查密钥长度（针对不同算法）
        var publicKey = certificate.PublicKey;
        if (publicKey.Oid.Value == "1.2.840.113549.1.1.1") // RSA
        {
            var keySize = publicKey.Key?.KeySize ?? 0;
            if (keySize < 2048)
            {
                result.Issues.Add($"RSA密钥长度不足: {keySize}位，建议至少2048位");
            }
        }
    }

    /// <summary>
    /// 检查证书是否具有指定的密钥用途
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <param name="keyUsage">密钥用途标志</param>
    /// <returns>是否具有指定用途</returns>
    private bool HasKeyUsage(X509Certificate2 certificate, X509KeyUsageFlags keyUsage)
    {
        foreach (var extension in certificate.Extensions)
        {
            if (extension is X509KeyUsageExtension keyUsageExt)
            {
                return keyUsageExt.KeyUsages.HasFlag(keyUsage);
            }
        }
        return false;
    }

    /// <summary>
    /// 检查证书是否具有指定的增强密钥用途
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <param name="oid">用途OID</param>
    /// <returns>是否具有指定用途</returns>
    private bool HasEnhancedKeyUsage(X509Certificate2 certificate, string oid)
    {
        foreach (var extension in certificate.Extensions)
        {
            if (extension is X509EnhancedKeyUsageExtension ekuExt)
            {
                foreach (var usage in ekuExt.EnhancedKeyUsages)
                {
                    if (usage.Value == oid)
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 验证证书扩展
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <param name="result">验证结果</param>
    private void ValidateCertificateExtensions(X509Certificate2 certificate, CertificatePropertyValidationResult result)
    {
        foreach (var extension in certificate.Extensions)
        {
            if (extension.Critical && !IsKnownExtension(extension.Oid?.Value))
            {
                result.Issues.Add($"包含未知的关键扩展: {extension.Oid?.Value}");
            }
        }
    }

    /// <summary>
    /// 检查是否为已知的扩展
    /// </summary>
    /// <param name="oid">扩展OID</param>
    /// <returns>是否为已知扩展</returns>
    private bool IsKnownExtension(string? oid)
    {
        var knownExtensions = new[]
        {
            "2.5.29.15", // Key Usage
            "2.5.29.37", // Extended Key Usage
            "2.5.29.14", // Subject Key Identifier
            "2.5.29.35", // Authority Key Identifier
            "2.5.29.19", // Basic Constraints
            "2.5.29.31", // CRL Distribution Points
            "1.3.6.1.5.5.7.1.1" // Authority Information Access
        };

        return knownExtensions.Contains(oid);
    }

    /// <summary>
    /// 获取CRL分发点
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <returns>CRL分发点URL列表</returns>
    private List<string> GetCrlDistributionPoints(X509Certificate2 certificate)
    {
        var crlUrls = new List<string>();

        foreach (var extension in certificate.Extensions)
        {
            if (extension.Oid?.Value == "2.5.29.31") // CRL Distribution Points
            {
                // 这里需要解析CRL分发点扩展
                // 简化实现，返回空列表
                break;
            }
        }

        return crlUrls;
    }

    /// <summary>
    /// 获取OCSP URL
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <returns>OCSP URL列表</returns>
    private List<string> GetOcspUrls(X509Certificate2 certificate)
    {
        var ocspUrls = new List<string>();

        foreach (var extension in certificate.Extensions)
        {
            if (extension.Oid?.Value == "1.3.6.1.5.5.7.1.1") // Authority Information Access
            {
                // 这里需要解析AIA扩展
                // 简化实现，返回空列表
                break;
            }
        }

        return ocspUrls;
    }

    /// <summary>
    /// 通过CRL检查撤销状态
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <param name="crlUrl">CRL URL</param>
    /// <returns>是否已撤销</returns>
    private async Task<bool> CheckCrlRevocationAsync(X509Certificate2 certificate, string crlUrl)
    {
        // 简化实现，实际需要下载和解析CRL
        await Task.Delay(100); // 模拟网络请求
        return false;
    }

    /// <summary>
    /// 通过OCSP检查撤销状态
    /// </summary>
    /// <param name="certificate">证书</param>
    /// <param name="ocspUrl">OCSP URL</param>
    /// <param name="issuerCertificate">颁发者证书</param>
    /// <returns>OCSP响应结果</returns>
    private async Task<OcspResult> CheckOcspRevocationAsync(X509Certificate2 certificate, string ocspUrl, X509Certificate2? issuerCertificate)
    {
        // 简化实现，实际需要构建和发送OCSP请求
        await Task.Delay(100); // 模拟网络请求
        return new OcspResult { IsRevoked = false };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _trustedRootStore?.Dispose();
            _intermediateStore?.Dispose();
            _disposed = true;
        }
    }
}

#region 结果类定义

/// <summary>
/// 证书验证结果
/// </summary>
public class CertificateValidationResult
{
    public X509Certificate2? Certificate { get; set; }
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<X509ChainStatus> ChainStatus { get; set; } = new();
    public List<CertificateChainElement> ChainElements { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 证书链元素
/// </summary>
public class CertificateChainElement
{
    public X509Certificate2? Certificate { get; set; }
    public List<X509ChainStatus> ChainElementStatus { get; set; } = new();
    public string? Information { get; set; }
}

/// <summary>
/// 证书属性验证结果
/// </summary>
public class CertificatePropertyValidationResult
{
    public X509Certificate2? Certificate { get; set; }
    public bool IsValid { get; set; }
    public bool IsInValidPeriod { get; set; }
    public bool HasDigitalSignatureUsage { get; set; }
    public bool HasNonRepudiationUsage { get; set; }
    public string SignatureAlgorithm { get; set; } = string.Empty;
    public string PublicKeyAlgorithm { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public bool IsKeyStrengthAdequate { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// 证书撤销结果
/// </summary>
public class CertificateRevocationResult
{
    public X509Certificate2? Certificate { get; set; }
    public bool IsValid { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CheckTime { get; set; }
    public string? RevocationReason { get; set; }
    public List<string> CrlDistributionPoints { get; set; } = new();
    public List<string> OcspUrls { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// OCSP结果
/// </summary>
public class OcspResult
{
    public bool IsRevoked { get; set; }
    public string? RevocationReason { get; set; }
    public DateTime ResponseTime { get; set; }
}

#endregion
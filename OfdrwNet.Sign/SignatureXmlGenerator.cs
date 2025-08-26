using OfdrwNet.Core.BasicType;
using System.Security.Cryptography.X509Certificates;

namespace OfdrwNet.Sign;

/// <summary>
/// OFD签名XML结构生成器
/// 负责生成符合OFD标准的签名相关XML文件
/// </summary>
public class SignatureXmlGenerator
{
    /// <summary>
    /// 签名时间格式
    /// </summary>
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

    /// <summary>
    /// 生成签名列表文件 Signatures.xml
    /// </summary>
    /// <param name="signatures">签名列表</param>
    /// <returns>Signatures.xml内容</returns>
    public static string GenerateSignaturesXml(List<SignatureInfo> signatures)
    {
        var xml = "<?xml version='1.0' encoding='UTF-8'?>\n";
        xml += "<ofd:Signatures xmlns:ofd='http://www.ofdspec.org/2016'>\n";

        foreach (var signature in signatures.OrderBy(s => s.SignID))
        {
            xml += $"  <ofd:Signature ID='{signature.SignID}' Type='{signature.Type}' BaseLoc='Sign_{signature.SignID}/'>\n";
            
            if (!string.IsNullOrEmpty(signature.SignedDate))
            {
                xml += $"    <ofd:SignedDate>{signature.SignedDate}</ofd:SignedDate>\n";
            }
            
            if (!string.IsNullOrEmpty(signature.Provider))
            {
                xml += $"    <ofd:Provider>\n";
                xml += $"      <ofd:ProviderName>{signature.Provider}</ofd:ProviderName>\n";
                xml += $"      <ofd:Version>{signature.ProviderVersion}</ofd:Version>\n";
                xml += $"    </ofd:Provider>\n";
            }

            xml += "  </ofd:Signature>\n";
        }

        xml += "</ofd:Signatures>";
        return xml;
    }

    /// <summary>
    /// 生成单个签名文件 Signature.xml
    /// </summary>
    /// <param name="signatureInfo">签名信息</param>
    /// <param name="fileDigests">文件摘要</param>
    /// <param name="certificate">签名证书</param>
    /// <returns>Signature.xml内容</returns>
    public static string GenerateSignatureXml(SignatureInfo signatureInfo, 
        Dictionary<string, byte[]> fileDigests, X509Certificate2? certificate = null)
    {
        var xml = "<?xml version='1.0' encoding='UTF-8'?>\n";
        xml += "<ofd:Signature xmlns:ofd='http://www.ofdspec.org/2016'>\n";

        // 签名基本信息
        xml += $"  <ofd:SignedInfo>\n";
        xml += $"    <ofd:Provider>\n";
        xml += $"      <ofd:ProviderName>{signatureInfo.Provider}</ofd:ProviderName>\n";
        xml += $"      <ofd:Version>{signatureInfo.ProviderVersion}</ofd:Version>\n";
        xml += $"      <ofd:Company>{signatureInfo.Company}</ofd:Company>\n";
        xml += $"    </ofd:Provider>\n";
        xml += $"    <ofd:SignatureMethod>{signatureInfo.Algorithm}</ofd:SignatureMethod>\n";
        xml += $"    <ofd:SignatureDateTime>{signatureInfo.SignedDate}</ofd:SignatureDateTime>\n";

        // 签名文件列表
        if (fileDigests.Count > 0)
        {
            xml += $"    <ofd:References>\n";
            
            foreach (var fileDigest in fileDigests.OrderBy(kvp => kvp.Key))
            {
                var filePath = fileDigest.Key;
                var digest = fileDigest.Value;
                var digestHex = Convert.ToHexString(digest);
                
                xml += $"      <ofd:Reference FileRef='{filePath}' CheckMethod='{signatureInfo.DigestAlgorithm}'>\n";
                xml += $"        <ofd:DigestValue>{digestHex}</ofd:DigestValue>\n";
                xml += $"      </ofd:Reference>\n";
            }
            
            xml += $"    </ofd:References>\n";
        }

        xml += $"  </ofd:SignedInfo>\n";

        // 签名值
        if (signatureInfo.SignatureValue != null && signatureInfo.SignatureValue.Length > 0)
        {
            var signatureHex = Convert.ToHexString(signatureInfo.SignatureValue);
            xml += $"  <ofd:SignedValue>{signatureHex}</ofd:SignedValue>\n";
        }

        // 证书信息
        if (certificate != null)
        {
            xml += GenerateCertificateXml(certificate);
        }

        xml += "</ofd:Signature>";
        return xml;
    }

    /// <summary>
    /// 生成证书信息XML
    /// </summary>
    /// <param name="certificate">X509证书</param>
    /// <returns>证书XML片段</returns>
    private static string GenerateCertificateXml(X509Certificate2 certificate)
    {
        var xml = "  <ofd:SignatureCert>\n";
        xml += $"    <ofd:CertDigestMethod>SHA256</ofd:CertDigestMethod>\n";
        
        // 证书摘要
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var certDigest = sha256.ComputeHash(certificate.RawData);
        var certDigestHex = Convert.ToHexString(certDigest);
        xml += $"    <ofd:CertDigestValue>{certDigestHex}</ofd:CertDigestValue>\n";

        // 证书详细信息
        xml += $"    <ofd:Certificate>\n";
        xml += $"      <ofd:Version>{certificate.Version}</ofd:Version>\n";
        xml += $"      <ofd:SerialNumber>{certificate.SerialNumber}</ofd:SerialNumber>\n";
        xml += $"      <ofd:Issuer>{EscapeXml(certificate.Issuer)}</ofd:Issuer>\n";
        xml += $"      <ofd:Subject>{EscapeXml(certificate.Subject)}</ofd:Subject>\n";
        xml += $"      <ofd:ValidFrom>{certificate.NotBefore:yyyy-MM-ddTHH:mm:ss}</ofd:ValidFrom>\n";
        xml += $"      <ofd:ValidTo>{certificate.NotAfter:yyyy-MM-ddTHH:mm:ss}</ofd:ValidTo>\n";
        
        // 证书二进制数据
        var certDataBase64 = Convert.ToBase64String(certificate.RawData);
        xml += $"      <ofd:CertData>{certDataBase64}</ofd:CertData>\n";
        xml += $"    </ofd:Certificate>\n";

        xml += $"  </ofd:SignatureCert>\n";
        return xml;
    }

    /// <summary>
    /// 生成签名属性XML
    /// </summary>
    /// <param name="signatureInfo">签名信息</param>
    /// <returns>签名属性XML片段</returns>
    public static string GenerateSignedPropertiesXml(SignatureInfo signatureInfo)
    {
        var xml = "  <ofd:SignedProperties>\n";

        // 签名时间
        xml += $"    <ofd:SigningTime>{signatureInfo.SignedDate}</ofd:SigningTime>\n";

        // 签名策略
        if (!string.IsNullOrEmpty(signatureInfo.SignaturePolicy))
        {
            xml += $"    <ofd:SignaturePolicyIdentifier>\n";
            xml += $"      <ofd:SignaturePolicyId>{signatureInfo.SignaturePolicy}</ofd:SignaturePolicyId>\n";
            xml += $"    </ofd:SignaturePolicyIdentifier>\n";
        }

        // 签名者角色
        if (!string.IsNullOrEmpty(signatureInfo.SignerRole))
        {
            xml += $"    <ofd:SignerRole>\n";
            xml += $"      <ofd:ClaimedRoles>\n";
            xml += $"        <ofd:ClaimedRole>{signatureInfo.SignerRole}</ofd:ClaimedRole>\n";
            xml += $"      </ofd:ClaimedRoles>\n";
            xml += $"    </ofd:SignerRole>\n";
        }

        // 签名位置
        if (!string.IsNullOrEmpty(signatureInfo.SignatureLocation))
        {
            xml += $"    <ofd:SignatureLocation>{signatureInfo.SignatureLocation}</ofd:SignatureLocation>\n";
        }

        // 签名原因
        if (!string.IsNullOrEmpty(signatureInfo.SignatureReason))
        {
            xml += $"    <ofd:SignatureReason>{EscapeXml(signatureInfo.SignatureReason)}</ofd:SignatureReason>\n";
        }

        xml += $"  </ofd:SignedProperties>\n";
        return xml;
    }

    /// <summary>
    /// 生成签名外观XML（如果有可视化签名）
    /// </summary>
    /// <param name="appearance">签名外观信息</param>
    /// <returns>签名外观XML</returns>
    public static string GenerateSignatureAppearanceXml(SignatureAppearance appearance)
    {
        var xml = "  <ofd:SignatureAppearance>\n";

        // 签名位置
        if (appearance.PageId != null && appearance.Boundary != null)
        {
            xml += $"    <ofd:PageRef>{appearance.PageId}</ofd:PageRef>\n";
            xml += $"    <ofd:Boundary>{appearance.Boundary.X} {appearance.Boundary.Y} {appearance.Boundary.Width} {appearance.Boundary.Height}</ofd:Boundary>\n";
        }

        // 签名图片
        if (!string.IsNullOrEmpty(appearance.ImagePath))
        {
            xml += $"    <ofd:SignatureImage>{appearance.ImagePath}</ofd:SignatureImage>\n";
        }

        // 签名文本
        if (!string.IsNullOrEmpty(appearance.SignatureText))
        {
            xml += $"    <ofd:SignatureText>{EscapeXml(appearance.SignatureText)}</ofd:SignatureText>\n";
        }

        xml += $"  </ofd:SignatureAppearance>\n";
        return xml;
    }

    /// <summary>
    /// 生成签名验证信息XML
    /// </summary>
    /// <param name="verificationInfo">验证信息</param>
    /// <returns>验证信息XML</returns>
    public static string GenerateVerificationInfoXml(SignatureVerificationInfo verificationInfo)
    {
        var xml = "  <ofd:VerificationInfo>\n";

        xml += $"    <ofd:VerificationTime>{verificationInfo.VerificationTime:yyyy-MM-ddTHH:mm:ss}</ofd:VerificationTime>\n";
        xml += $"    <ofd:VerificationResult>{verificationInfo.IsValid}</ofd:VerificationResult>\n";

        if (!string.IsNullOrEmpty(verificationInfo.VerificationMessage))
        {
            xml += $"    <ofd:VerificationMessage>{EscapeXml(verificationInfo.VerificationMessage)}</ofd:VerificationMessage>\n";
        }

        if (!string.IsNullOrEmpty(verificationInfo.VerifierInfo))
        {
            xml += $"    <ofd:VerifierInfo>{EscapeXml(verificationInfo.VerifierInfo)}</ofd:VerifierInfo>\n";
        }

        xml += $"  </ofd:VerificationInfo>\n";
        return xml;
    }

    /// <summary>
    /// 生成完整的签名XML文件
    /// </summary>
    /// <param name="signatureInfo">签名信息</param>
    /// <param name="fileDigests">文件摘要</param>
    /// <param name="certificate">签名证书</param>
    /// <param name="appearance">签名外观（可选）</param>
    /// <param name="verificationInfo">验证信息（可选）</param>
    /// <returns>完整的签名XML</returns>
    public static string GenerateCompleteSignatureXml(
        SignatureInfo signatureInfo,
        Dictionary<string, byte[]> fileDigests,
        X509Certificate2? certificate = null,
        SignatureAppearance? appearance = null,
        SignatureVerificationInfo? verificationInfo = null)
    {
        var xml = "<?xml version='1.0' encoding='UTF-8'?>\n";
        xml += "<ofd:Signature xmlns:ofd='http://www.ofdspec.org/2016'>\n";

        // 签名基本信息
        xml += GenerateSignedInfoXml(signatureInfo, fileDigests);

        // 签名值
        if (signatureInfo.SignatureValue != null && signatureInfo.SignatureValue.Length > 0)
        {
            var signatureHex = Convert.ToHexString(signatureInfo.SignatureValue);
            xml += $"  <ofd:SignedValue>{signatureHex}</ofd:SignedValue>\n";
        }

        // 证书信息
        if (certificate != null)
        {
            xml += GenerateCertificateXml(certificate);
        }

        // 签名属性
        xml += GenerateSignedPropertiesXml(signatureInfo);

        // 签名外观
        if (appearance != null)
        {
            xml += GenerateSignatureAppearanceXml(appearance);
        }

        // 验证信息
        if (verificationInfo != null)
        {
            xml += GenerateVerificationInfoXml(verificationInfo);
        }

        xml += "</ofd:Signature>";
        return xml;
    }

    /// <summary>
    /// 生成SignedInfo部分的XML
    /// </summary>
    /// <param name="signatureInfo">签名信息</param>
    /// <param name="fileDigests">文件摘要</param>
    /// <returns>SignedInfo XML片段</returns>
    private static string GenerateSignedInfoXml(SignatureInfo signatureInfo, Dictionary<string, byte[]> fileDigests)
    {
        var xml = "  <ofd:SignedInfo>\n";
        xml += $"    <ofd:Provider>\n";
        xml += $"      <ofd:ProviderName>{signatureInfo.Provider}</ofd:ProviderName>\n";
        xml += $"      <ofd:Version>{signatureInfo.ProviderVersion}</ofd:Version>\n";
        xml += $"      <ofd:Company>{signatureInfo.Company}</ofd:Company>\n";
        xml += $"    </ofd:Provider>\n";
        xml += $"    <ofd:SignatureMethod>{signatureInfo.Algorithm}</ofd:SignatureMethod>\n";
        xml += $"    <ofd:SignatureDateTime>{signatureInfo.SignedDate}</ofd:SignatureDateTime>\n";

        // 签名文件列表
        if (fileDigests.Count > 0)
        {
            xml += $"    <ofd:References>\n";
            
            foreach (var fileDigest in fileDigests.OrderBy(kvp => kvp.Key))
            {
                var filePath = fileDigest.Key;
                var digest = fileDigest.Value;
                var digestHex = Convert.ToHexString(digest);
                
                xml += $"      <ofd:Reference FileRef='{filePath}' CheckMethod='{signatureInfo.DigestAlgorithm}'>\n";
                xml += $"        <ofd:DigestValue>{digestHex}</ofd:DigestValue>\n";
                xml += $"      </ofd:Reference>\n";
            }
            
            xml += $"    </ofd:References>\n";
        }

        xml += $"  </ofd:SignedInfo>\n";
        return xml;
    }

    /// <summary>
    /// XML字符转义
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>转义后的文本</returns>
    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&apos;");
    }
}

/// <summary>
/// 签名信息类
/// </summary>
public class SignatureInfo
{
    /// <summary>
    /// 签名ID
    /// </summary>
    public string SignID { get; set; } = string.Empty;

    /// <summary>
    /// 签名类型
    /// </summary>
    public string Type { get; set; } = "Seal";

    /// <summary>
    /// 签名日期
    /// </summary>
    public string SignedDate { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

    /// <summary>
    /// 签名提供者
    /// </summary>
    public string Provider { get; set; } = "OfdrwNet";

    /// <summary>
    /// 提供者版本
    /// </summary>
    public string ProviderVersion { get; set; } = "2.0.0";

    /// <summary>
    /// 公司名称
    /// </summary>
    public string Company { get; set; } = "OfdrwNet";

    /// <summary>
    /// 签名算法
    /// </summary>
    public string Algorithm { get; set; } = "SHA256withRSA";

    /// <summary>
    /// 摘要算法
    /// </summary>
    public string DigestAlgorithm { get; set; } = "SHA256";

    /// <summary>
    /// 签名值
    /// </summary>
    public byte[]? SignatureValue { get; set; }

    /// <summary>
    /// 签名策略
    /// </summary>
    public string? SignaturePolicy { get; set; }

    /// <summary>
    /// 签名者角色
    /// </summary>
    public string? SignerRole { get; set; }

    /// <summary>
    /// 签名位置
    /// </summary>
    public string? SignatureLocation { get; set; }

    /// <summary>
    /// 签名原因
    /// </summary>
    public string? SignatureReason { get; set; }
}

/// <summary>
/// 签名外观信息类
/// </summary>
public class SignatureAppearance
{
    /// <summary>
    /// 签名所在页面ID
    /// </summary>
    public StId? PageId { get; set; }

    /// <summary>
    /// 签名边界框
    /// </summary>
    public StBox? Boundary { get; set; }

    /// <summary>
    /// 签名图片路径
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// 签名文本
    /// </summary>
    public string? SignatureText { get; set; }
}

/// <summary>
/// 签名验证信息类
/// </summary>
public class SignatureVerificationInfo
{
    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime VerificationTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 验证结果
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 验证消息
    /// </summary>
    public string? VerificationMessage { get; set; }

    /// <summary>
    /// 验证器信息
    /// </summary>
    public string? VerifierInfo { get; set; }
}
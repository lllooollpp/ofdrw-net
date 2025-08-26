using System.Security.Cryptography;

namespace OfdrwNet.Sign;

/// <summary>
/// 待摘要文件信息
/// 对应 Java 版本的 org.ofdrw.sign.ToDigestFileInfo
/// </summary>
public class ToDigestFileInfo
{
    /// <summary>
    /// 文件绝对路径
    /// </summary>
    public string AbsolutePath { get; set; }

    /// <summary>
    /// 文件虚拟路径
    /// </summary>
    public string VirtualPath { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="absolutePath">文件绝对路径</param>
    /// <param name="virtualPath">文件虚拟路径</param>
    public ToDigestFileInfo(string absolutePath, string virtualPath)
    {
        AbsolutePath = absolutePath ?? throw new ArgumentNullException(nameof(absolutePath));
        VirtualPath = virtualPath ?? throw new ArgumentNullException(nameof(virtualPath));
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>描述字符串</returns>
    public override string ToString()
    {
        return $"ToDigestFileInfo: {VirtualPath} -> {AbsolutePath}";
    }
}

/// <summary>
/// 扩展签名容器接口
/// 对应 Java 版本的 org.ofdrw.sign.ExtendSignatureContainer
/// 用于实现具体的数字签名算法
/// </summary>
public interface IExtendSignatureContainer : IDisposable
{
    /// <summary>
    /// 签名算法名称
    /// </summary>
    string Algorithm { get; }

    /// <summary>
    /// 签名提供者名称
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// 获取签名者证书
    /// </summary>
    /// <returns>签名者证书字节数组</returns>
    byte[] GetSignerCertificate();

    /// <summary>
    /// 计算文件摘要
    /// </summary>
    /// <param name="fileInfo">待摘要文件信息</param>
    /// <returns>文件摘要</returns>
    Task<byte[]> ComputeDigestAsync(ToDigestFileInfo fileInfo);

    /// <summary>
    /// 执行数字签名
    /// </summary>
    /// <param name="tbsContent">待签名内容</param>
    /// <returns>签名值</returns>
    Task<byte[]> SignAsync(byte[] tbsContent);

    /// <summary>
    /// 验证数字签名
    /// </summary>
    /// <param name="tbsContent">原始内容</param>
    /// <param name="signatureValue">签名值</param>
    /// <returns>验证结果</returns>
    Task<bool> VerifyAsync(byte[] tbsContent, byte[] signatureValue);
}

/// <summary>
/// 默认数字签名容器实现
/// 基于.NET标准密码学API的简单实现
/// 注意：这只是一个示例实现，实际生产环境中需要使用符合国密标准的实现
/// </summary>
public class DefaultSignatureContainer : IExtendSignatureContainer
{
    private readonly RSA _rsa;
    private readonly HashAlgorithmName _hashAlgorithm;
    private bool _disposed = false;

    /// <summary>
    /// 签名算法名称
    /// </summary>
    public string Algorithm => "SHA256withRSA";

    /// <summary>
    /// 签名提供者名称
    /// </summary>
    public string Provider => "OfdrwNet.DefaultProvider";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="rsa">RSA实例</param>
    /// <param name="hashAlgorithm">哈希算法</param>
    public DefaultSignatureContainer(RSA rsa, HashAlgorithmName hashAlgorithm = default)
    {
        _rsa = rsa ?? throw new ArgumentNullException(nameof(rsa));
        _hashAlgorithm = hashAlgorithm == default ? HashAlgorithmName.SHA256 : hashAlgorithm;
    }

    /// <summary>
    /// 获取签名者证书
    /// </summary>
    /// <returns>签名者证书字节数组</returns>
    public byte[] GetSignerCertificate()
    {
        // 这里应该返回实际的X.509证书
        // 当前返回RSA公钥作为占位符
        return _rsa.ExportRSAPublicKey();
    }

    /// <summary>
    /// 计算文件摘要
    /// </summary>
    /// <param name="fileInfo">待摘要文件信息</param>
    /// <returns>文件摘要</returns>
    public async Task<byte[]> ComputeDigestAsync(ToDigestFileInfo fileInfo)
    {
        using var fileStream = File.OpenRead(fileInfo.AbsolutePath);
        using var hashAlgorithm = SHA256.Create();
        return await Task.Run(() => hashAlgorithm.ComputeHash(fileStream));
    }

    /// <summary>
    /// 执行数字签名
    /// </summary>
    /// <param name="tbsContent">待签名内容</param>
    /// <returns>签名值</returns>
    public async Task<byte[]> SignAsync(byte[] tbsContent)
    {
        return await Task.Run(() => _rsa.SignData(tbsContent, _hashAlgorithm, RSASignaturePadding.Pkcs1));
    }

    /// <summary>
    /// 验证数字签名
    /// </summary>
    /// <param name="tbsContent">原始内容</param>
    /// <param name="signatureValue">签名值</param>
    /// <returns>验证结果</returns>
    public async Task<bool> VerifyAsync(byte[] tbsContent, byte[] signatureValue)
    {
        return await Task.Run(() => _rsa.VerifyData(tbsContent, signatureValue, _hashAlgorithm, RSASignaturePadding.Pkcs1));
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _rsa?.Dispose();
            _disposed = true;
        }
    }
}
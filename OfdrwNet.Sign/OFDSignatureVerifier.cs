using OfdrwNet.Reader;

namespace OfdrwNet.Sign;

/// <summary>
/// 签名验证结果
/// </summary>
public class SignatureVerifyResult
{
    /// <summary>
    /// 验证是否成功
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 签名ID
    /// </summary>
    public string SignID { get; set; } = string.Empty;

    /// <summary>
    /// 签名时间
    /// </summary>
    public DateTime? SignTime { get; set; }

    /// <summary>
    /// 签名算法
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// 签名提供者
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 验证的文件数量
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// 签名者证书信息
    /// </summary>
    public byte[]? SignerCertificate { get; set; }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    public override string ToString()
    {
        var status = IsValid ? "有效" : "无效";
        var time = SignTime?.ToString(OFDSigner.DateTimeFormat) ?? "未知";
        return $"签名验证结果: {status}, 签名ID: {SignID}, 签名时间: {time}, 算法: {Algorithm}";
    }
}

/// <summary>
/// OFD签名验证器
/// 对应 Java 版本的 org.ofdrw.sign.verify 包的功能
/// </summary>
public class OFDSignatureVerifier : IDisposable
{
    /// <summary>
    /// OFD读取器
    /// </summary>
    private readonly OfdReader _reader;

    /// <summary>
    /// 签名容器
    /// </summary>
    private IExtendSignatureContainer? _signatureContainer;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="reader">OFD读取器</param>
    public OFDSignatureVerifier(OfdReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    /// <summary>
    /// 设置签名容器
    /// </summary>
    /// <param name="container">签名容器</param>
    /// <returns>this</returns>
    public OFDSignatureVerifier SetSignatureContainer(IExtendSignatureContainer container)
    {
        _signatureContainer = container ?? throw new ArgumentNullException(nameof(container));
        return this;
    }

    /// <summary>
    /// 验证所有签名
    /// </summary>
    /// <returns>验证结果列表</returns>
    public async Task<List<SignatureVerifyResult>> VerifyAllSignaturesAsync()
    {
        var results = new List<SignatureVerifyResult>();

        try
        {
            Console.WriteLine("开始验证OFD文档的所有数字签名...");

            // 查找签名列表文件
            var signaturesPath = await FindSignaturesFileAsync();
            if (string.IsNullOrEmpty(signaturesPath))
            {
                Console.WriteLine("文档中没有找到数字签名");
                return results;
            }

            // 解析签名列表
            var signatureList = await ParseSignatureListAsync(signaturesPath);

            // 验证每个签名
            foreach (var signatureInfo in signatureList)
            {
                var result = await VerifySignatureAsync(signatureInfo);
                results.Add(result);
            }

            Console.WriteLine($"签名验证完成，共验证{results.Count}个签名");
        }
        catch (Exception ex)
        {
            var errorResult = new SignatureVerifyResult
            {
                IsValid = false,
                ErrorMessage = $"签名验证过程发生错误: {ex.Message}",
                SignID = "Unknown"
            };
            results.Add(errorResult);
        }

        return results;
    }

    /// <summary>
    /// 验证指定签名
    /// </summary>
    /// <param name="signID">签名ID</param>
    /// <returns>验证结果</returns>
    public async Task<SignatureVerifyResult> VerifySignatureAsync(string signID)
    {
        try
        {
            Console.WriteLine($"验证签名: {signID}");

            if (_signatureContainer == null)
            {
                return new SignatureVerifyResult
                {
                    IsValid = false,
                    SignID = signID,
                    ErrorMessage = "签名容器未设置，请先调用SetSignatureContainer方法"
                };
            }

            // 查找签名文件
            var signaturePath = await FindSignatureFileAsync(signID);
            if (string.IsNullOrEmpty(signaturePath))
            {
                return new SignatureVerifyResult
                {
                    IsValid = false,
                    SignID = signID,
                    ErrorMessage = $"未找到签名文件: {signID}"
                };
            }

            // 解析签名文件
            var signatureData = await ParseSignatureFileAsync(signaturePath);

            // 重新计算文件摘要
            var currentDigests = await RecomputeFileDigestsAsync(signatureData.ProtectedFiles);

            // 比较摘要
            var digestsMatch = CompareDigests(signatureData.OriginalDigests, currentDigests);
            if (!digestsMatch)
            {
                return new SignatureVerifyResult
                {
                    IsValid = false,
                    SignID = signID,
                    ErrorMessage = "文件摘要不匹配，文档可能已被修改",
                    Algorithm = signatureData.Algorithm,
                    Provider = signatureData.Provider,
                    SignTime = signatureData.SignTime,
                    FileCount = signatureData.ProtectedFiles.Count
                };
            }

            // 验证签名值
            var tbsContent = await ReconstructTbsContentAsync(signatureData);
            var isSignatureValid = await _signatureContainer.VerifyAsync(tbsContent, signatureData.SignatureValue);

            return new SignatureVerifyResult
            {
                IsValid = isSignatureValid,
                SignID = signID,
                Algorithm = signatureData.Algorithm,
                Provider = signatureData.Provider,
                SignTime = signatureData.SignTime,
                FileCount = signatureData.ProtectedFiles.Count,
                SignerCertificate = signatureData.Certificate,
                ErrorMessage = isSignatureValid ? null : "签名值验证失败"
            };
        }
        catch (Exception ex)
        {
            return new SignatureVerifyResult
            {
                IsValid = false,
                SignID = signID,
                ErrorMessage = $"验证签名时发生错误: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 查找签名列表文件
    /// </summary>
    private async Task<string?> FindSignaturesFileAsync()
    {
        // 在OFD根目录查找Signatures.xml文件
        var workDir = _reader.GetWorkDir();
        var signaturesFile = Path.Combine(workDir, "Signatures.xml");
        
        if (File.Exists(signaturesFile))
        {
            return signaturesFile;
        }

        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// 解析签名列表文件
    /// </summary>
    private async Task<List<string>> ParseSignatureListAsync(string signaturesPath)
    {
        var signatureList = new List<string>();

        // 简化实现：假设签名ID为Sign_0, Sign_1等格式
        // 实际应该解析XML文件
        try
        {
            var content = await File.ReadAllTextAsync(signaturesPath);
            // 这里应该解析XML，提取签名ID列表
            // 当前返回模拟数据
            signatureList.Add("Sign_0");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析签名列表文件失败: {ex.Message}");
        }

        return signatureList;
    }

    /// <summary>
    /// 查找签名文件
    /// </summary>
    private async Task<string?> FindSignatureFileAsync(string signID)
    {
        var workDir = _reader.GetWorkDir();
        var signatureFile = Path.Combine(workDir, signID, "Signature.xml");
        
        if (File.Exists(signatureFile))
        {
            return signatureFile;
        }

        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// 签名数据类
    /// </summary>
    private class SignatureData
    {
        public string Algorithm { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public DateTime? SignTime { get; set; }
        public List<string> ProtectedFiles { get; set; } = new();
        public Dictionary<string, byte[]> OriginalDigests { get; set; } = new();
        public byte[] SignatureValue { get; set; } = Array.Empty<byte>();
        public byte[]? Certificate { get; set; }
    }

    /// <summary>
    /// 解析签名文件
    /// </summary>
    private async Task<SignatureData> ParseSignatureFileAsync(string signaturePath)
    {
        // 这里应该解析XML文件，提取签名信息
        // 当前返回模拟数据
        var data = new SignatureData
        {
            Algorithm = "SHA256withRSA",
            Provider = "OfdrwNet.DefaultProvider",
            SignTime = DateTime.Now,
            ProtectedFiles = new List<string> { "/OFD.xml", "/Doc_0/Document.xml" },
            SignatureValue = new byte[256] // 模拟签名值
        };

        await Task.CompletedTask;
        return data;
    }

    /// <summary>
    /// 重新计算文件摘要
    /// </summary>
    private async Task<Dictionary<string, byte[]>> RecomputeFileDigestsAsync(List<string> protectedFiles)
    {
        var digests = new Dictionary<string, byte[]>();

        if (_signatureContainer == null)
        {
            return digests;
        }

        var workDir = _reader.GetWorkDir();

        foreach (var virtualPath in protectedFiles)
        {
            var filePath = Path.Combine(workDir, virtualPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath))
            {
                var fileInfo = new ToDigestFileInfo(filePath, virtualPath);
                var digest = await _signatureContainer.ComputeDigestAsync(fileInfo);
                digests[virtualPath] = digest;
            }
        }

        return digests;
    }

    /// <summary>
    /// 比较摘要
    /// </summary>
    private bool CompareDigests(Dictionary<string, byte[]> original, Dictionary<string, byte[]> current)
    {
        if (original.Count != current.Count)
        {
            return false;
        }

        foreach (var kvp in original)
        {
            if (!current.TryGetValue(kvp.Key, out var currentDigest))
            {
                return false;
            }

            if (!kvp.Value.SequenceEqual(currentDigest))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 重构待签名内容
    /// </summary>
    private async Task<byte[]> ReconstructTbsContentAsync(SignatureData signatureData)
    {
        // 重构原始的待签名内容
        var signedInfo = new
        {
            Algorithm = signatureData.Algorithm,
            Provider = signatureData.Provider,
            SignTime = signatureData.SignTime?.ToString(OFDSigner.DateTimeFormat),
            FileDigests = signatureData.OriginalDigests
        };

        var json = System.Text.Json.JsonSerializer.Serialize(signedInfo);
        var tbsContent = System.Text.Encoding.UTF8.GetBytes(json);

        await Task.CompletedTask;
        return tbsContent;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _signatureContainer?.Dispose();
            _disposed = true;
        }
    }
}
using OfdrwNet.Core.BasicType;
using OfdrwNet.Reader;
using OfdrwNet.Packaging.Container;
using System.Security.Cryptography;

namespace OfdrwNet.Sign;

/// <summary>
/// OFD文档数字签名引擎
/// 对应 Java 版本的 org.ofdrw.sign.OFDSigner
/// 签名和验证操作均针对于OFD文档中的第一个文档
/// 
/// 注意：此实现提供基础框架，实际生产环境中需要集成符合国密标准的密码学库
/// </summary>
public class OFDSigner : IDisposable
{
    /// <summary>
    /// 时间日期格式
    /// </summary>
    public static readonly string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// OFDRW 签名提供者信息
    /// </summary>
    public static class OfdrwProvider
    {
        public static string Name => "ofdrw-sign";
        public static string Company => "ofdrw";
        public static string Version => "2.0.0";
    }

    /// <summary>
    /// OFD读取器
    /// </summary>
    private readonly OfdReader _reader;

    /// <summary>
    /// 输出流
    /// </summary>
    private readonly Stream _outputStream;

    /// <summary>
    /// 签名ID提供者
    /// </summary>
    private readonly ISignIDProvider _signIDProvider;

    /// <summary>
    /// 数字签名模式
    /// </summary>
    private SignMode _signMode;

    /// <summary>
    /// 签名扩展属性
    /// </summary>
    private readonly Dictionary<string, object> _parameters;

    /// <summary>
    /// 签名容器
    /// </summary>
    private IExtendSignatureContainer? _signatureContainer;

    /// <summary>
    /// 是否已执行签名
    /// </summary>
    private bool _hasSigned = false;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="reader">OFD读取器</param>
    /// <param name="outputStream">输出流</param>
    /// <param name="signIDProvider">签名ID提供者</param>
    public OFDSigner(OfdReader reader, Stream outputStream, ISignIDProvider? signIDProvider = null)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
        _signIDProvider = signIDProvider ?? new NumberFormatAtomicSignID(false);
        _signMode = SignMode.ContinueSign;
        _parameters = new Dictionary<string, object>();
    }

    /// <summary>
    /// 设置签名模式
    /// </summary>
    /// <param name="mode">签名模式</param>
    /// <returns>this</returns>
    public OFDSigner SetSignMode(SignMode mode)
    {
        _signMode = mode;
        return this;
    }

    /// <summary>
    /// 设置签名容器
    /// </summary>
    /// <param name="container">签名容器</param>
    /// <returns>this</returns>
    public OFDSigner SetSignatureContainer(IExtendSignatureContainer container)
    {
        _signatureContainer = container ?? throw new ArgumentNullException(nameof(container));
        return this;
    }

    /// <summary>
    /// 添加签名参数
    /// </summary>
    /// <param name="key">参数名</param>
    /// <param name="value">参数值</param>
    /// <returns>this</returns>
    public OFDSigner AddParameter(string key, object value)
    {
        _parameters[key] = value;
        return this;
    }

    /// <summary>
    /// 执行数字签名
    /// </summary>
    /// <returns>签名任务</returns>
    /// <exception cref="SignatureException">签名异常</exception>
    public async Task SignAsync()
    {
        if (_hasSigned)
        {
            throw new SignatureException("已经执行过签名，不能重复签名");
        }

        if (_signatureContainer == null)
        {
            throw new SignatureException("签名容器未设置，请先调用SetSignatureContainer方法");
        }

        try
        {
            Console.WriteLine("开始执行OFD数字签名...");

            // 1. 预检查
            await PreCheckAsync();

            // 2. 构建待摘要文件列表
            var digestFiles = await BuildDigestFileListAsync();

            // 3. 计算文件摘要
            var fileDigests = await ComputeFileDigestsAsync(digestFiles);

            // 4. 构建待签名内容
            var tbsContent = await BuildTbsContentAsync(fileDigests);

            // 5. 执行签名
            var signatureValue = await _signatureContainer.SignAsync(tbsContent);

            // 6. 构建签名结构
            await BuildSignatureStructureAsync(fileDigests, signatureValue);

            // 7. 输出签名后的OFD文件
            await OutputSignedOfdAsync();

            _hasSigned = true;
            Console.WriteLine("OFD数字签名完成");
        }
        catch (Exception ex)
        {
            throw new SignatureException($"数字签名执行失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 预检查
    /// </summary>
    private async Task PreCheckAsync()
    {
        // 检查OFD文件结构
        var pageCount = _reader.GetNumberOfPages();
        if (pageCount <= 0)
        {
            throw new SignatureException("OFD文件没有页面，无法进行签名");
        }

        Console.WriteLine($"预检查通过，文档共{pageCount}页");
        await Task.CompletedTask;
    }

    /// <summary>
    /// 构建待摘要文件列表
    /// </summary>
    private async Task<List<ToDigestFileInfo>> BuildDigestFileListAsync()
    {
        var digestFiles = new List<ToDigestFileInfo>();

        // 根据签名模式决定保护哪些文件
        switch (_signMode)
        {
            case SignMode.ContinueSign:
                // 继续签名模式：保护除签名列表文件外的所有文件
                Console.WriteLine("使用继续签名模式");
                break;

            case SignMode.WholeProtect:
                // 完整保护模式：保护所有文件
                Console.WriteLine("使用完整保护模式");
                break;
        }

        // 这里需要遍历OFD容器中的所有文件
        // 简化实现：只添加主要文件
        var workDir = _reader.GetWorkDir();
        var ofdFiles = Directory.GetFiles(workDir, "*.xml", SearchOption.AllDirectories);

        foreach (var file in ofdFiles)
        {
            var relativePath = Path.GetRelativePath(workDir, file);
            var virtualPath = "/" + relativePath.Replace('\\', '/');
            
            // 根据签名模式过滤文件
            if (_signMode == SignMode.ContinueSign && virtualPath.Contains("Signatures.xml"))
            {
                continue; // 继续签名模式不保护签名列表文件
            }

            digestFiles.Add(new ToDigestFileInfo(file, virtualPath));
        }

        Console.WriteLine($"构建待摘要文件列表完成，共{digestFiles.Count}个文件");
        await Task.CompletedTask;
        return digestFiles;
    }

    /// <summary>
    /// 计算文件摘要
    /// </summary>
    private async Task<Dictionary<string, byte[]>> ComputeFileDigestsAsync(List<ToDigestFileInfo> digestFiles)
    {
        var digests = new Dictionary<string, byte[]>();

        foreach (var fileInfo in digestFiles)
        {
            var digest = await _signatureContainer!.ComputeDigestAsync(fileInfo);
            digests[fileInfo.VirtualPath] = digest;
        }

        Console.WriteLine($"文件摘要计算完成，共{digests.Count}个文件摘要");
        return digests;
    }

    /// <summary>
    /// 构建待签名内容
    /// </summary>
    private async Task<byte[]> BuildTbsContentAsync(Dictionary<string, byte[]> fileDigests)
    {
        // 构建SignedInfo结构
        var signedInfo = new
        {
            Algorithm = _signatureContainer!.Algorithm,
            Provider = _signatureContainer.Provider,
            SignTime = DateTime.Now.ToString(DateTimeFormat),
            FileDigests = fileDigests
        };

        // 序列化为JSON（简化实现，实际应按OFD标准构建XML）
        var json = System.Text.Json.JsonSerializer.Serialize(signedInfo);
        var tbsContent = System.Text.Encoding.UTF8.GetBytes(json);

        Console.WriteLine($"构建待签名内容完成，长度: {tbsContent.Length}字节");
        await Task.CompletedTask;
        return tbsContent;
    }

    /// <summary>
    /// 构建签名结构
    /// </summary>
    private async Task BuildSignatureStructureAsync(Dictionary<string, byte[]> fileDigests, byte[] signatureValue)
    {
        var signID = _signIDProvider.Get();
        _signIDProvider.Increment();

        Console.WriteLine($"构建签名结构: {signID}");

        // 创建签名信息
        var signatureInfo = new SignatureInfo
        {
            SignID = signID,
            Type = "Seal",
            Provider = OfdrwProvider.Name,
            ProviderVersion = OfdrwProvider.Version,
            Company = OfdrwProvider.Company,
            Algorithm = _signatureContainer!.Algorithm,
            DigestAlgorithm = "SHA256",
            SignatureValue = signatureValue,
            SignedDate = DateTime.Now.ToString(DateTimeFormat)
        };

        // 从参数中获取额外信息
        if (_parameters.TryGetValue("SignerRole", out var signerRole))
        {
            signatureInfo.SignerRole = signerRole.ToString();
        }
        if (_parameters.TryGetValue("SignatureLocation", out var location))
        {
            signatureInfo.SignatureLocation = location.ToString();
        }
        if (_parameters.TryGetValue("SignatureReason", out var reason))
        {
            signatureInfo.SignatureReason = reason.ToString();
        }

        // 生成签名XML文件
        await GenerateSignatureFilesAsync(signatureInfo, fileDigests);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 生成签名相关的XML文件
    /// </summary>
    /// <param name="signatureInfo">签名信息</param>
    /// <param name="fileDigests">文件摘要</param>
    private async Task GenerateSignatureFilesAsync(SignatureInfo signatureInfo, Dictionary<string, byte[]> fileDigests)
    {
        var workDir = _reader.GetWorkDir();
        var signaturesDir = Path.Combine(workDir, "Signatures");
        
        // 确保签名目录存在
        if (!Directory.Exists(signaturesDir))
        {
            Directory.CreateDirectory(signaturesDir);
        }

        var signDir = Path.Combine(signaturesDir, $"Sign_{signatureInfo.SignID}");
        if (!Directory.Exists(signDir))
        {
            Directory.CreateDirectory(signDir);
        }

        // 生成或更新 Signatures.xml
        await GenerateSignaturesListAsync(signaturesDir, signatureInfo);

        // 生成单个签名文件 Signature.xml
        var signatureXml = SignatureXmlGenerator.GenerateCompleteSignatureXml(
            signatureInfo, fileDigests);
        var signatureFilePath = Path.Combine(signDir, "Signature.xml");
        await File.WriteAllTextAsync(signatureFilePath, signatureXml);

        // 保存签名值到单独文件（可选）
        if (signatureInfo.SignatureValue != null)
        {
            var signedValuePath = Path.Combine(signDir, "SignedValue.dat");
            await File.WriteAllBytesAsync(signedValuePath, signatureInfo.SignatureValue);
        }

        Console.WriteLine($"签名文件生成完成: {signDir}");
    }

    /// <summary>
    /// 生成或更新签名列表文件
    /// </summary>
    /// <param name="signaturesDir">签名目录</param>
    /// <param name="newSignature">新签名信息</param>
    private async Task GenerateSignaturesListAsync(string signaturesDir, SignatureInfo newSignature)
    {
        var signaturesFilePath = Path.Combine(signaturesDir, "Signatures.xml");
        var signatures = new List<SignatureInfo>();

        // 如果已存在签名列表文件，先读取现有签名
        if (File.Exists(signaturesFilePath))
        {
            try
            {
                // 这里应该解析现有的Signatures.xml文件
                // 简化实现：假设是新文件
            }
            catch
            {
                // 解析失败，创建新的列表
            }
        }

        // 添加新签名
        signatures.Add(newSignature);

        // 生成签名列表XML
        var signaturesXml = SignatureXmlGenerator.GenerateSignaturesXml(signatures);
        await File.WriteAllTextAsync(signaturesFilePath, signaturesXml);
    }
    private async Task OutputSignedOfdAsync()
    {
        // 将签名后的OFD文件写入输出流
        // 这里需要重新打包整个OFD容器

        var workDir = _reader.GetWorkDir();
        
        // 简化实现：直接复制原文件
        // 实际应该加入签名相关文件后重新打包
        var tempZip = Path.GetTempFileName();
        System.IO.Compression.ZipFile.CreateFromDirectory(workDir, tempZip);
        
        using var fileStream = File.OpenRead(tempZip);
        await fileStream.CopyToAsync(_outputStream);
        
        File.Delete(tempZip);
        
        Console.WriteLine("签名后的OFD文件输出完成");
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
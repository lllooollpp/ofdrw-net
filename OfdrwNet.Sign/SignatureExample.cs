using System.Security.Cryptography;
using OfdrwNet.Reader;
using Microsoft.Extensions.Logging; // 新增
using System.IO; // 新增
using System.Linq; // 新增

namespace OfdrwNet.Sign;

/// <summary>
/// OFD数字签名示例
/// 展示如何使用OfdrwNet.Sign模块进行数字签名和验证
/// 
/// 注意：这是示例实现，生产环境中需要使用符合国密标准的密码学库
/// </summary>
public static class SignatureExample
{
    /// <summary>
    /// 数字签名示例
    /// </summary>
    /// <param name="logger">日志接口</param>
    /// <param name="inputOfdPath">输入OFD文件路径</param>
    /// <param name="outputOfdPath">签名后输出文件路径</param>
    public static async Task SignDocumentExample(ILogger logger, string inputOfdPath, string outputOfdPath)
    {
        logger.LogInformation("=== OFD数字签名示例 ===");

        if (!File.Exists(inputOfdPath))
        {
            logger.LogWarning("输入文件不存在: {File}", inputOfdPath);
            return;
        }

        try
        {
            // 1. 创建RSA密钥对（示例用，生产环境应使用符合标准的证书）
            using var rsa = RSA.Create(2048);
            logger.LogDebug("生成RSA密钥对完成");

            // 2. 创建签名容器
            using var signatureContainer = new DefaultSignatureContainer(rsa);
            logger.LogDebug("创建签名容器完成");

            // 3. 打开OFD文档
            using var reader = new OfdReader(inputOfdPath);
            logger.LogInformation("打开OFD文档: {File}", Path.GetFileName(inputOfdPath));

            // 4. 创建输出流
            using var outputStream = File.Create(outputOfdPath);

            // 5. 创建签名器
            using var signer = new OFDSigner(reader, outputStream)
                .SetSignMode(SignMode.ContinueSign)
                .SetSignatureContainer(signatureContainer)
                .AddParameter("Reason", "文档数字签名")
                .AddParameter("Location", "OfdrwNet");

            logger.LogDebug("配置签名器完成");

            // 6. 执行签名
            await signer.SignAsync();
            
            logger.LogInformation("数字签名完成 输出文件: {Out}", outputOfdPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数字签名失败");
        }
    }

    /// <summary>
    /// 签名验证示例
    /// </summary>
    /// <param name="logger">日志接口</param>
    /// <param name="signedOfdPath">已签名的OFD文件路径</param>
    public static async Task VerifySignatureExample(ILogger logger, string signedOfdPath)
    {
        logger.LogInformation("=== OFD签名验证示例 ===");

        if (!File.Exists(signedOfdPath))
        {
            logger.LogWarning("文件不存在: {File}", signedOfdPath);
            return;
        }

        try
        {
            // 1. 打开已签名的OFD文档
            using var reader = new OfdReader(signedOfdPath);
            logger.LogInformation("打开已签名文档: {File}", Path.GetFileName(signedOfdPath));

            // 2. 创建验证用的签名容器（需要与签名时使用的公钥匹配）
            using var rsa = RSA.Create(2048);
            using var signatureContainer = new DefaultSignatureContainer(rsa);

            // 3. 创建验证器
            using var verifier = new OFDSignatureVerifier(reader)
                .SetSignatureContainer(signatureContainer);

            logger.LogDebug("配置验证器完成");

            // 4. 验证所有签名
            var results = await verifier.VerifyAllSignaturesAsync();

            logger.LogInformation("验证完成 签名数={Count}", results.Count);
            foreach (var r in results)
            {
                if (string.IsNullOrEmpty(r.ErrorMessage)) logger.LogInformation("签名: {Result}", r);
                else logger.LogWarning("签名: {Result} 错误={Err}", r, r.ErrorMessage);
            }

            // 6. 统计验证结果
            var validCount = results.Count(r => r.IsValid);
            var invalidCount = results.Count - validCount;
            
            logger.LogInformation("验证结果汇总 有效={Valid} 无效={Invalid}", validCount, invalidCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "签名验证失败");
        }
    }

    /// <summary>
    /// 完整保护模式签名示例
    /// </summary>
    /// <param name="logger">日志接口</param>
    /// <param name="inputOfdPath">输入OFD文件路径</param>
    /// <param name="outputOfdPath">签名后输出文件路径</param>
    public static async Task WholeProtectSignExample(ILogger logger, string inputOfdPath, string outputOfdPath)
    {
        logger.LogInformation("=== OFD完整保护签名示例 ===");

        if (!File.Exists(inputOfdPath))
        {
            logger.LogWarning("输入文件不存在: {File}", inputOfdPath);
            return;
        }

        try
        {
            using var rsa = RSA.Create(2048);
            using var signatureContainer = new DefaultSignatureContainer(rsa);
            using var reader = new OfdReader(inputOfdPath);
            using var outputStream = File.Create(outputOfdPath);

            using var signer = new OFDSigner(reader, outputStream)
                .SetSignMode(SignMode.WholeProtect) // 使用完整保护模式
                .SetSignatureContainer(signatureContainer)
                .AddParameter("Reason", "文档完整性保护")
                .AddParameter("Location", "OfdrwNet")
                .AddParameter("ContactInfo", "admin@ofdrw.net");

            logger.LogInformation("使用完整保护模式进行签名...");
            await signer.SignAsync();
            
            logger.LogInformation("完整保护签名完成 输出={Out}", outputOfdPath);
            logger.LogWarning("完整保护模式签名后文档不可追加新签名");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "完整保护签名失败");
        }
    }

    /// <summary>
    /// 运行所有签名示例
    /// </summary>
    /// <param name="logger">日志接口</param>
    /// <param name="workingDir">工作目录</param>
    /// <param name="sampleOfdFile">示例OFD文件</param>
    public static async Task RunAllExamples(ILogger logger, string workingDir, string? sampleOfdFile = null)
    {
        logger.LogInformation("开始OFD数字签名示例演示...");

        if (!Directory.Exists(workingDir))
        {
            Directory.CreateDirectory(workingDir);
        }

        // 如果没有提供示例文件，跳过演示
        if (string.IsNullOrEmpty(sampleOfdFile) || !File.Exists(sampleOfdFile))
        {
            logger.LogWarning("没有有效的OFD示例文件，跳过数字签名演示");
            logger.LogInformation("功能说明: 继续签名 / 完整保护 / 验证 / 可扩展容器 / .NET8 Crypto");
            logger.LogWarning("生产需使用符合国密或合规密码库");
            return;
        }

        try
        {
            // 1. 继续签名模式示例
            var continueSignedFile = Path.Combine(workingDir, "continue_signed.ofd");
            await SignDocumentExample(logger, sampleOfdFile, continueSignedFile);
            logger.LogInformation("");

            // 2. 签名验证示例
            if (File.Exists(continueSignedFile))
            {
                await VerifySignatureExample(logger, continueSignedFile);
                logger.LogInformation("");
            }

            // 3. 完整保护签名示例
            var wholeProtectFile = Path.Combine(workingDir, "whole_protect_signed.ofd");
            await WholeProtectSignExample(logger, sampleOfdFile, wholeProtectFile);
            logger.LogInformation("");

            logger.LogInformation("数字签名示例演示完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "示例演示过程中出现错误");
        }
    }
}
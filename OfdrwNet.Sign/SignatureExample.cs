using System.Security.Cryptography;
using OfdrwNet.Reader;

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
    /// <param name="inputOfdPath">输入OFD文件路径</param>
    /// <param name="outputOfdPath">签名后输出文件路径</param>
    public static async Task SignDocumentExample(string inputOfdPath, string outputOfdPath)
    {
        Console.WriteLine("=== OFD数字签名示例 ===");

        if (!File.Exists(inputOfdPath))
        {
            Console.WriteLine($"输入文件不存在: {inputOfdPath}");
            return;
        }

        try
        {
            // 1. 创建RSA密钥对（示例用，生产环境应使用符合标准的证书）
            using var rsa = RSA.Create(2048);
            Console.WriteLine("生成RSA密钥对完成");

            // 2. 创建签名容器
            using var signatureContainer = new DefaultSignatureContainer(rsa);
            Console.WriteLine("创建签名容器完成");

            // 3. 打开OFD文档
            using var reader = new OfdReader(inputOfdPath);
            Console.WriteLine($"打开OFD文档: {Path.GetFileName(inputOfdPath)}");

            // 4. 创建输出流
            using var outputStream = File.Create(outputOfdPath);

            // 5. 创建签名器
            using var signer = new OFDSigner(reader, outputStream)
                .SetSignMode(SignMode.ContinueSign)
                .SetSignatureContainer(signatureContainer)
                .AddParameter("Reason", "文档数字签名")
                .AddParameter("Location", "OfdrwNet");

            Console.WriteLine("配置签名器完成");

            // 6. 执行签名
            await signer.SignAsync();
            
            Console.WriteLine($"数字签名完成！输出文件: {outputOfdPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"数字签名失败: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"内部异常: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// 签名验证示例
    /// </summary>
    /// <param name="signedOfdPath">已签名的OFD文件路径</param>
    public static async Task VerifySignatureExample(string signedOfdPath)
    {
        Console.WriteLine("=== OFD签名验证示例 ===");

        if (!File.Exists(signedOfdPath))
        {
            Console.WriteLine($"文件不存在: {signedOfdPath}");
            return;
        }

        try
        {
            // 1. 打开已签名的OFD文档
            using var reader = new OfdReader(signedOfdPath);
            Console.WriteLine($"打开已签名文档: {Path.GetFileName(signedOfdPath)}");

            // 2. 创建验证用的签名容器（需要与签名时使用的公钥匹配）
            using var rsa = RSA.Create(2048);
            using var signatureContainer = new DefaultSignatureContainer(rsa);

            // 3. 创建验证器
            using var verifier = new OFDSignatureVerifier(reader)
                .SetSignatureContainer(signatureContainer);

            Console.WriteLine("配置验证器完成");

            // 4. 验证所有签名
            var results = await verifier.VerifyAllSignaturesAsync();

            // 5. 输出验证结果
            Console.WriteLine($"验证完成，共发现{results.Count}个签名:");
            foreach (var result in results)
            {
                Console.WriteLine($"  {result}");
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine($"    错误: {result.ErrorMessage}");
                }
            }

            // 6. 统计验证结果
            var validCount = results.Count(r => r.IsValid);
            var invalidCount = results.Count - validCount;
            
            Console.WriteLine($"验证结果汇总: {validCount}个有效签名，{invalidCount}个无效签名");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"签名验证失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 完整保护模式签名示例
    /// </summary>
    /// <param name="inputOfdPath">输入OFD文件路径</param>
    /// <param name="outputOfdPath">签名后输出文件路径</param>
    public static async Task WholeProtectSignExample(string inputOfdPath, string outputOfdPath)
    {
        Console.WriteLine("=== OFD完整保护签名示例 ===");

        if (!File.Exists(inputOfdPath))
        {
            Console.WriteLine($"输入文件不存在: {inputOfdPath}");
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

            Console.WriteLine("使用完整保护模式进行签名...");
            await signer.SignAsync();
            
            Console.WriteLine($"完整保护签名完成！输出文件: {outputOfdPath}");
            Console.WriteLine("注意：完整保护模式签名后，文档不能再添加新的签名");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"完整保护签名失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 运行所有签名示例
    /// </summary>
    /// <param name="workingDir">工作目录</param>
    /// <param name="sampleOfdFile">示例OFD文件</param>
    public static async Task RunAllExamples(string workingDir, string? sampleOfdFile = null)
    {
        Console.WriteLine("开始OFD数字签名示例演示...");
        Console.WriteLine();

        if (!Directory.Exists(workingDir))
        {
            Directory.CreateDirectory(workingDir);
        }

        // 如果没有提供示例文件，跳过演示
        if (string.IsNullOrEmpty(sampleOfdFile) || !File.Exists(sampleOfdFile))
        {
            Console.WriteLine("没有有效的OFD示例文件，跳过数字签名演示");
            Console.WriteLine();
            Console.WriteLine("数字签名功能说明：");
            Console.WriteLine("✓ 支持继续签名模式和完整保护模式");
            Console.WriteLine("✓ 提供数字签名容器接口，可扩展支持不同算法");
            Console.WriteLine("✓ 支持签名验证功能");
            Console.WriteLine("✓ 基于.NET 8现代密码学API");
            Console.WriteLine();
            Console.WriteLine("注意：生产环境中需要集成符合国密标准的密码学库");
            Console.WriteLine("当前实现仅为演示框架，实际签名需要使用认证的密码设备");
            return;
        }

        try
        {
            // 1. 继续签名模式示例
            var continueSignedFile = Path.Combine(workingDir, "continue_signed.ofd");
            await SignDocumentExample(sampleOfdFile, continueSignedFile);
            Console.WriteLine();

            // 2. 签名验证示例
            if (File.Exists(continueSignedFile))
            {
                await VerifySignatureExample(continueSignedFile);
                Console.WriteLine();
            }

            // 3. 完整保护签名示例
            var wholeProtectFile = Path.Combine(workingDir, "whole_protect_signed.ofd");
            await WholeProtectSignExample(sampleOfdFile, wholeProtectFile);
            Console.WriteLine();

            Console.WriteLine("数字签名示例演示完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"示例演示过程中出现错误: {ex.Message}");
        }
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfdrwNet.WinFormsDemo.Converters;

namespace TestSpecificPdf
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 测试 33190-2016-gbt-cd-300.pdf 转换 ===");
            
            // 设置控制台日志
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<Pdf2OfdConverter>();

            // 输入和输出文件路径
            var inputPdf = @"d:\workspace\ofdrw-master\ofdrw-net\33190-2016-gbt-cd-300.pdf";
            var outputOfd = @"d:\workspace\ofdrw-master\ofdrw-net\33190-2016-gbt-cd-300_converted.ofd";

            Console.WriteLine($"输入文件: {inputPdf}");
            Console.WriteLine($"输出文件: {outputOfd}");

            // 检查输入文件
            if (!File.Exists(inputPdf))
            {
                Console.WriteLine("❌ 错误：输入PDF文件不存在");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            var inputFileInfo = new FileInfo(inputPdf);
            Console.WriteLine($"📄 输入文件大小: {inputFileInfo.Length / (1024.0 * 1024.0):F2} MB");

            try
            {
                // 创建转换器
                using var converter = new Pdf2OfdConverter(logger);

                // 记录转换开始时间
                var startTime = DateTime.Now;

                // 添加进度监听
                converter.ProgressChanged += (sender, e) =>
                {
                    var elapsed = DateTime.Now - startTime;
                    Console.WriteLine($"⏱️  转换进度: {e.Percentage}% - {e.Message} (已用时: {elapsed.TotalSeconds:F1}秒)");
                };

                converter.ConversionCompleted += (sender, e) =>
                {
                    var totalTime = DateTime.Now - startTime;
                    if (e.Result.Success)
                    {
                        Console.WriteLine($"\n🎉 转换成功完成！");
                        Console.WriteLine($"📂 输出文件: {e.Result.OutputPath}");
                        Console.WriteLine($"📏 输出文件大小: {e.Result.OutputFileSize / 1024.0:F2} KB");
                        Console.WriteLine($"📄 页面数量: {e.Result.PageCount}");
                        Console.WriteLine($"⏱️  总耗时: {totalTime.TotalSeconds:F2} 秒");
                        Console.WriteLine($"📊 压缩比: {(double)e.Result.OutputFileSize / e.Result.InputFileSize * 100:F1}%");
                    }
                    else
                    {
                        Console.WriteLine($"❌ 转换失败: {e.Result.ErrorMessage}");
                        Console.WriteLine($"⏱️  耗时: {totalTime.TotalSeconds:F2} 秒");
                    }
                };

                Console.WriteLine("\n🚀 开始转换...");
                var result = await converter.ConvertAsync(inputPdf, outputOfd);

                Console.WriteLine($"\n=== 转换结果分析 ===");
                
                if (result.Success)
                {
                    // 验证输出文件
                    if (File.Exists(outputOfd))
                    {
                        var outputFileInfo = new FileInfo(outputOfd);
                        Console.WriteLine($"✅ 输出文件已创建");
                        Console.WriteLine($"📏 输出文件大小: {outputFileInfo.Length / 1024.0:F2} KB");
                        
                        // 与之前的24字节比较
                        if (outputFileInfo.Length > 1024) // 大于1KB
                        {
                            Console.WriteLine("✅ 文件大小正常（远大于之前的24字节占位符）");
                            
                            // 验证是否为有效的ZIP文件（OFD格式）
                            try
                            {
                                using var archive = System.IO.Compression.ZipFile.OpenRead(outputOfd);
                                Console.WriteLine($"✅ OFD文件结构有效，包含 {archive.Entries.Count} 个文件:");
                                
                                foreach (var entry in archive.Entries)
                                {
                                    Console.WriteLine($"  📄 {entry.FullName} ({entry.Length} bytes)");
                                    
                                    // 显示关键文件的内容摘要
                                    if (entry.FullName == "OFD.xml" && entry.Length < 2048)
                                    {
                                        using var stream = entry.Open();
                                        using var reader = new StreamReader(stream);
                                        var content = await reader.ReadToEndAsync();
                                        Console.WriteLine($"    🔍 OFD.xml 内容预览: {content.Substring(0, Math.Min(100, content.Length))}...");
                                    }
                                }
                                
                                // 结构检查
                                bool hasOfdXml = archive.Entries.Any(e => e.FullName == "OFD.xml");
                                bool hasDocumentXml = archive.Entries.Any(e => e.FullName == "Doc/Document.xml");
                                bool hasPageFiles = archive.Entries.Any(e => e.FullName.StartsWith("Doc/Pages/Page_"));
                                
                                Console.WriteLine($"\n📋 OFD结构检查:");
                                Console.WriteLine($"  OFD.xml: {(hasOfdXml ? "✅" : "❌")}");
                                Console.WriteLine($"  Document.xml: {(hasDocumentXml ? "✅" : "❌")}");
                                Console.WriteLine($"  页面文件: {(hasPageFiles ? "✅" : "❌")}");
                                
                                if (hasOfdXml && hasDocumentXml && hasPageFiles)
                                {
                                    Console.WriteLine("🎯 OFD文档结构完整，符合标准");
                                }
                                else
                                {
                                    Console.WriteLine("⚠️  OFD文档结构不完整");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ OFD文件结构验证失败: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ 文件大小异常（可能仍是占位符）");
                        }
                        
                        // 性能分析
                        var conversionSpeed = inputFileInfo.Length / 1024.0 / 1024.0 / (DateTime.Now - startTime).TotalSeconds;
                        Console.WriteLine($"\n📊 性能分析:");
                        Console.WriteLine($"  转换速度: {conversionSpeed:F2} MB/秒");
                        Console.WriteLine($"  压缩率: {(1 - (double)outputFileInfo.Length / inputFileInfo.Length) * 100:F1}%");
                    }
                    else
                    {
                        Console.WriteLine("❌ 输出文件未创建");
                    }
                }

                Console.WriteLine($"\n=== 最终结果 ===");
                Console.WriteLine($"转换状态: {(result.Success ? "🎉 成功" : "❌ 失败")}");
                if (!result.Success)
                {
                    Console.WriteLine($"错误信息: {result.ErrorMessage}");
                }
                
                Console.WriteLine($"\n📈 与修复前对比:");
                Console.WriteLine($"  修复前: 输出24字节占位符");
                Console.WriteLine($"  修复后: 输出{(result.Success && File.Exists(outputOfd) ? new FileInfo(outputOfd).Length / 1024.0 : 0):F1}KB有效OFD文档");
                Console.WriteLine($"  改善程度: {(result.Success && File.Exists(outputOfd) ? new FileInfo(outputOfd).Length / 24.0 : 0):F0}倍提升");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 转换过程中发生异常: {ex.Message}");
                Console.WriteLine($"📋 详细信息: {ex}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
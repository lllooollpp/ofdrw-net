using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfdrwNet.WinFormsDemo.Converters;

namespace TestPdfConversion
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== PDF转OFD转换测试 ===");
            
            // 设置控制台日志
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<Pdf2OfdConverter>();

            // 输入和输出文件路径
            var inputPdf = @"d:\workspace\ofdrw-master\ofdrw-net\GBT_33190-2016_电子文件存储与交换格式版式文档.pdf";
            var outputOfd = @"d:\workspace\ofdrw-master\ofdrw-net\test_output.ofd";

            Console.WriteLine($"输入文件: {inputPdf}");
            Console.WriteLine($"输出文件: {outputOfd}");

            // 检查输入文件
            if (!File.Exists(inputPdf))
            {
                Console.WriteLine("错误：输入PDF文件不存在");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            var inputFileInfo = new FileInfo(inputPdf);
            Console.WriteLine($"输入文件大小: {inputFileInfo.Length / (1024.0 * 1024.0):F2} MB");

            try
            {
                // 创建转换器
                using var converter = new Pdf2OfdConverter(logger);

                // 添加进度监听
                converter.ProgressChanged += (sender, e) =>
                {
                    Console.WriteLine($"转换进度: {e.Percentage}% - {e.Message}");
                };

                converter.ConversionCompleted += (sender, e) =>
                {
                    if (e.Result.Success)
                    {
                        Console.WriteLine($"转换成功完成！");
                        Console.WriteLine($"输出文件: {e.Result.OutputPath}");
                        Console.WriteLine($"输出文件大小: {e.Result.OutputFileSize / 1024.0:F2} KB");
                        Console.WriteLine($"页面数量: {e.Result.PageCount}");
                    }
                    else
                    {
                        Console.WriteLine($"转换失败: {e.Result.ErrorMessage}");
                    }
                };

                Console.WriteLine("\n开始转换...");
                var result = await converter.ConvertAsync(inputPdf, outputOfd);

                if (result.Success)
                {
                    // 验证输出文件
                    if (File.Exists(outputOfd))
                    {
                        var outputFileInfo = new FileInfo(outputOfd);
                        Console.WriteLine($"\n=== 转换结果验证 ===");
                        Console.WriteLine($"✓ 输出文件已创建");
                        Console.WriteLine($"✓ 输出文件大小: {outputFileInfo.Length / 1024.0:F2} KB");
                        
                        if (outputFileInfo.Length > 1024) // 大于1KB
                        {
                            Console.WriteLine("✓ 文件大小正常（大于1KB占位符）");
                            
                            // 验证是否为有效的ZIP文件
                            try
                            {
                                using var archive = System.IO.Compression.ZipFile.OpenRead(outputOfd);
                                Console.WriteLine($"✓ OFD文件结构有效，包含 {archive.Entries.Count} 个文件");
                                
                                foreach (var entry in archive.Entries)
                                {
                                    Console.WriteLine($"  - {entry.FullName} ({entry.Length} bytes)");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"✗ OFD文件结构验证失败: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("✗ 文件大小异常（仍然是1KB占位符？）");
                        }
                    }
                    else
                    {
                        Console.WriteLine("✗ 输出文件未创建");
                    }
                }

                Console.WriteLine($"\n=== 转换汇总 ===");
                Console.WriteLine($"转换状态: {(result.Success ? "成功" : "失败")}");
                if (!result.Success)
                {
                    Console.WriteLine($"错误信息: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换过程中发生异常: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
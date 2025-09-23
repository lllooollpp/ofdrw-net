using OfdrwNet.Converter;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace PdfToOfdTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 设置日志
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                Console.WriteLine("PDF 到 OFD 转换测试程序");
                Console.WriteLine("========================");

                // 检查命令行参数
                if (args.Length < 2)
                {
                    Console.WriteLine("用法: PdfToOfdTest <输入PDF文件> <输出OFD文件>");
                    Console.WriteLine("示例: PdfToOfdTest test.pdf output.ofd");
                    return;
                }

                string inputPdf = args[0];
                string outputOfd = args[1];

                // 检查输入文件是否存在
                if (!File.Exists(inputPdf))
                {
                    Console.WriteLine($"错误: 输入PDF文件不存在: {inputPdf}");
                    return;
                }

                Console.WriteLine($"输入PDF: {inputPdf}");
                Console.WriteLine($"输出OFD: {outputOfd}");

                // 执行转换
                var options = new ConvertHelper.PdfToOfdOptions
                {
                    EnableDeltaX = true,
                    EnableImageExtraction = true,
                    EnableAnnotationExtraction = true,
                    EnableFormExtraction = false // 暂时禁用表单处理
                };

                await ConvertHelper.ConvertPdfToOfdAsync(inputPdf, outputOfd, options);

                Console.WriteLine("转换完成!");
                Console.WriteLine($"输出文件: {outputOfd}");

                // 检查输出文件
                if (File.Exists(outputOfd))
                {
                    var fileInfo = new FileInfo(outputOfd);
                    Console.WriteLine($"文件大小: {fileInfo.Length} 字节");
                }
                else
                {
                    Console.WriteLine("警告: 输出文件未生成");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换失败: {ex.Message}");
                logger.LogError(ex, "转换过程中发生异常");
            }
        }
    }
}
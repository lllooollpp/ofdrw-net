using System;
using System.IO;
using System.Threading.Tasks;
using OfdrwNet;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout.Element.Canvas;

namespace SimpleTest
{
    /// <summary>
    /// 完整的OFD验证测试程序
    /// </summary>
    class ValidationProgram
    {
        public static async Task Run(string[] args)
        {
            Console.WriteLine("🚀 开始OFD文档生成和验证测试");
            Console.WriteLine();

            // 1. 生成OFD文档
            string outputPath = await GenerateOfdDocument();
            
            if (string.IsNullOrEmpty(outputPath))
            {
                Console.WriteLine("❌ OFD文档生成失败，退出验证");
                return;
            }

            // 2. 验证OFD文档
            var validationResult = OfdValidator.Validate(outputPath);

            // 3. 额外的文件分析
            await PerformAdvancedAnalysis(outputPath);

            // 4. 总结
            Console.WriteLine();
            Console.WriteLine("🎯 测试总结");
            Console.WriteLine("=" + new string('=', 50));
            
            if (validationResult.IsValid)
            {
                Console.WriteLine("✅ 所有测试通过！OFD文档生成和验证成功。");
                Console.WriteLine($"✅ 文档路径: {outputPath}");
                Console.WriteLine($"✅ 文件大小: {validationResult.FileSize} 字节");
                Console.WriteLine($"✅ 页面数量: {validationResult.PageCount}");
                
                // 5. 显示详细内容（可选）
                Console.WriteLine();
                Console.Write("是否查看OFD文档的详细内容？(y/N): ");
                var input = Console.ReadLine();
                if (input?.ToLowerInvariant() == "y" || input?.ToLowerInvariant() == "yes")
                {
                    Console.WriteLine();
                    OfdViewer.ViewContent(outputPath);
                }
            }
            else
            {
                Console.WriteLine("❌ 测试失败，存在问题需要修复。");
            }
        }

        /// <summary>
        /// 生成测试用的OFD文档
        /// </summary>
        private static async Task<string> GenerateOfdDocument()
        {
            Console.WriteLine("📝 正在生成测试OFD文档...");
            
            try
            {
                // 使用时间戳避免文件冲突
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string outputPath = $"validation_test_{timestamp}.ofd";
                
                // 删除可能存在的同名文件
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                using (var doc = new OfdrwNet.OFDDoc(outputPath))
                {
                    // 设置A4页面布局
                    var layout = OfdrwNet.PageLayout.A4();
                    doc.SetDefaultPageLayout(layout);

                    // 添加标题
                    var title = new Paragraph("OFD文档验证测试", 20)
                        .SetTextAlign(TextAlign.Center);
                    doc.Add(title);

                    // 添加内容
                    var content1 = new Paragraph("这是第一段内容，用于测试OFD文档的文本显示功能。", 14)
                        .SetTextAlign(TextAlign.Start);
                    doc.Add(content1);

                    var content2 = new Paragraph("这是第二段内容，字体稍小一些。", 12)
                        .SetTextAlign(TextAlign.Start);
                    doc.Add(content2);

                    // 添加居中的信息段落
                    var info = new Paragraph($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", 10)
                        .SetTextAlign(TextAlign.Center);
                    doc.Add(info);

                    // 关闭并生成文档
                    await doc.CloseAsync();

                    Console.WriteLine($"✅ OFD文档生成成功: {outputPath}");
                    return outputPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 生成OFD文档时发生错误: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 执行高级分析
        /// </summary>
        private static async Task PerformAdvancedAnalysis(string ofdPath)
        {
            Console.WriteLine();
            Console.WriteLine("🔬 执行高级分析");
            Console.WriteLine("-" + new string('-', 30));

            try
            {
                // 1. 检查文件签名
                CheckFileSignature(ofdPath);

                // 2. 分析内部结构
                await AnalyzeInternalStructure(ofdPath);

                // 3. 性能分析
                PerformanceAnalysis(ofdPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 高级分析失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查文件签名
        /// </summary>
        private static void CheckFileSignature(string ofdPath)
        {
            try
            {
                using (var fs = File.OpenRead(ofdPath))
                {
                    var buffer = new byte[4];
                    fs.Read(buffer, 0, 4);
                    
                    // ZIP文件的魔数是 PK (0x504B)
                    if (buffer[0] == 0x50 && buffer[1] == 0x4B)
                    {
                        Console.WriteLine("✅ 文件签名正确 (ZIP格式)");
                    }
                    else
                    {
                        Console.WriteLine($"❌ 文件签名异常: {BitConverter.ToString(buffer)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 文件签名检查失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分析内部结构
        /// </summary>
        private static async Task AnalyzeInternalStructure(string ofdPath)
        {
            try
            {
                using (var archive = System.IO.Compression.ZipFile.OpenRead(ofdPath))
                {
                    Console.WriteLine($"📁 内部文件分析:");
                    
                    long totalUncompressedSize = 0;
                    long totalCompressedSize = 0;
                    
                    foreach (var entry in archive.Entries)
                    {
                        totalUncompressedSize += entry.Length;
                        totalCompressedSize += entry.CompressedLength;
                        
                        string compressionRatio = entry.Length > 0 
                            ? $"{(double)entry.CompressedLength / entry.Length * 100:F1}%" 
                            : "0%";
                            
                        Console.WriteLine($"   📄 {entry.FullName}");
                        Console.WriteLine($"      原始大小: {entry.Length} 字节");
                        Console.WriteLine($"      压缩大小: {entry.CompressedLength} 字节");
                        Console.WriteLine($"      压缩比: {compressionRatio}");
                        Console.WriteLine();
                    }
                    
                    double overallCompressionRatio = totalUncompressedSize > 0 
                        ? (double)totalCompressedSize / totalUncompressedSize * 100 
                        : 0;
                        
                    Console.WriteLine($"📊 整体压缩统计:");
                    Console.WriteLine($"   总原始大小: {totalUncompressedSize} 字节");
                    Console.WriteLine($"   总压缩大小: {totalCompressedSize} 字节");
                    Console.WriteLine($"   整体压缩比: {overallCompressionRatio:F1}%");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 内部结构分析失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 性能分析
        /// </summary>
        private static void PerformanceAnalysis(string ofdPath)
        {
            try
            {
                var fileInfo = new FileInfo(ofdPath);
                
                Console.WriteLine($"⚡ 性能分析:");
                Console.WriteLine($"   文件大小: {fileInfo.Length} 字节");
                Console.WriteLine($"   文件大小: {fileInfo.Length / 1024.0:F2} KB");
                
                if (fileInfo.Length < 1024)
                {
                    Console.WriteLine($"   ✅ 文件大小合理 (< 1KB)");
                }
                else if (fileInfo.Length < 10 * 1024)
                {
                    Console.WriteLine($"   ✅ 文件大小正常 (< 10KB)");
                }
                else
                {
                    Console.WriteLine($"   ⚠️ 文件较大，可能需要优化");
                }
                
                Console.WriteLine($"   创建时间: {fileInfo.CreationTime}");
                Console.WriteLine($"   修改时间: {fileInfo.LastWriteTime}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 性能分析失败: {ex.Message}");
            }
        }
    }
}

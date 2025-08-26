namespace OfdrwNet.Tools;

/// <summary>
/// OFD工具示例类
/// 展示OFDMerger和OFDPageDeleter的使用方法
/// </summary>
public static class ToolsExample
{
    /// <summary>
    /// 多文档合并示例
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="inputFiles">输入文件列表</param>
    public static async Task MergeDocumentsExample(string outputPath, params string[] inputFiles)
    {
        Console.WriteLine("=== OFD文档合并示例 ===");

        if (inputFiles == null || inputFiles.Length == 0)
        {
            Console.WriteLine("没有提供输入文件");
            return;
        }

        try
        {
            using var merger = new OFDMerger(outputPath);

            // 添加所有输入文档
            foreach (var inputFile in inputFiles)
            {
                if (File.Exists(inputFile))
                {
                    merger.Add(inputFile);
                    Console.WriteLine($"添加文档: {Path.GetFileName(inputFile)}");
                }
                else
                {
                    Console.WriteLine($"文件不存在，跳过: {inputFile}");
                }
            }

            // 执行合并
            await merger.MergeAsync();

            Console.WriteLine($"合并完成！输出文件: {outputPath}");
            Console.WriteLine($"合并了 {merger.GetDocumentCount()} 个文档，共 {merger.GetPageCount()} 页");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"合并失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 文档页面裁剪示例
    /// </summary>
    /// <param name="inputPath">输入文件路径</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="pageNumbers">要保留的页码（从1开始）</param>
    public static async Task CropPagesExample(string inputPath, string outputPath, params int[] pageNumbers)
    {
        Console.WriteLine("=== OFD页面裁剪示例 ===");

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"输入文件不存在: {inputPath}");
            return;
        }

        try
        {
            using var merger = new OFDMerger(outputPath);
            
            // 只添加指定页面
            merger.Add(inputPath, pageNumbers);
            
            await merger.MergeAsync();
            
            Console.WriteLine($"页面裁剪完成！");
            Console.WriteLine($"从 {Path.GetFileName(inputPath)} 提取了 {pageNumbers.Length} 页");
            Console.WriteLine($"输出文件: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"页面裁剪失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 多文档页面重组示例
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    public static async Task ReorganizePagesExample(string outputPath, string doc1Path, string doc2Path)
    {
        Console.WriteLine("=== OFD页面重组示例 ===");

        if (!File.Exists(doc1Path) || !File.Exists(doc2Path))
        {
            Console.WriteLine("输入文件不完整");
            return;
        }

        try
        {
            using var merger = new OFDMerger(outputPath);
            
            // 自定义页面顺序：文档1的第1,2页 + 文档2的第1页 + 文档1的第3页
            merger.Add(doc1Path, 1, 2);    // 文档1的前两页
            merger.Add(doc2Path, 1);       // 文档2的第一页
            merger.Add(doc1Path, 3);       // 文档1的第三页
            
            await merger.MergeAsync();
            
            Console.WriteLine("页面重组完成！");
            Console.WriteLine($"重组后文档: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"页面重组失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 页面混合示例（简化实现）
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="doc1Path">第一个文档路径</param>
    /// <param name="doc2Path">第二个文档路径</param>
    public static async Task MixPagesExample(string outputPath, string doc1Path, string doc2Path)
    {
        Console.WriteLine("=== OFD页面混合示例 ===");

        if (!File.Exists(doc1Path) || !File.Exists(doc2Path))
        {
            Console.WriteLine("输入文件不完整");
            return;
        }

        try
        {
            using var merger = new OFDMerger(outputPath);
            
            // 混合两个文档的第一页
            merger.AddMix(doc1Path, 1, doc2Path, 1);
            
            await merger.MergeAsync();
            
            Console.WriteLine("页面混合完成！（当前为简化实现）");
            Console.WriteLine($"输出文件: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"页面混合失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 页面删除示例
    /// </summary>
    /// <param name="inputPath">输入文件路径</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="pageNumbersToDelete">要删除的页码（从1开始）</param>
    public static async Task DeletePagesExample(string inputPath, string outputPath, params int[] pageNumbersToDelete)
    {
        Console.WriteLine("=== OFD页面删除示例 ===");

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"输入文件不存在: {inputPath}");
            return;
        }

        try
        {
            using var deleter = new OFDPageDeleter(inputPath, outputPath);
            
            Console.WriteLine($"原文档页面数量: {deleter.GetPageCount()}");
            
            // 删除指定页面
            deleter.DeleteByPageNumbers(pageNumbersToDelete);
            
            Console.WriteLine($"删除页面数量: {deleter.GetPageCount()}");
            
            // 保存修改后的文档
            await deleter.SaveAsync();
            
            Console.WriteLine("页面删除完成！");
            Console.WriteLine($"输出文件: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"页面删除失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 运行所有工具示例（需要有效的OFD文件）
    /// </summary>
    /// <param name="workingDir">工作目录</param>
    /// <param name="sampleOfdFiles">示例OFD文件列表</param>
    public static async Task RunAllExamples(string workingDir, params string[] sampleOfdFiles)
    {
        Console.WriteLine("开始OFD工具示例演示...");
        Console.WriteLine();

        if (!Directory.Exists(workingDir))
        {
            Directory.CreateDirectory(workingDir);
        }

        // 验证示例文件
        var validFiles = sampleOfdFiles?.Where(File.Exists).ToArray() ?? Array.Empty<string>();
        
        if (validFiles.Length == 0)
        {
            Console.WriteLine("没有有效的OFD示例文件，跳过演示");
            return;
        }

        try
        {
            // 1. 文档合并示例
            if (validFiles.Length >= 2)
            {
                await MergeDocumentsExample(
                    Path.Combine(workingDir, "merged_document.ofd"),
                    validFiles[0], validFiles[1]
                );
                Console.WriteLine();
            }

            // 2. 页面裁剪示例
            await CropPagesExample(
                validFiles[0],
                Path.Combine(workingDir, "cropped_document.ofd"),
                1, 2 // 保留前两页
            );
            Console.WriteLine();

            // 3. 页面重组示例
            if (validFiles.Length >= 2)
            {
                await ReorganizePagesExample(
                    Path.Combine(workingDir, "reorganized_document.ofd"),
                    validFiles[0], validFiles[1]
                );
                Console.WriteLine();
            }

            // 4. 页面混合示例
            if (validFiles.Length >= 2)
            {
                await MixPagesExample(
                    Path.Combine(workingDir, "mixed_document.ofd"),
                    validFiles[0], validFiles[1]
                );
                Console.WriteLine();
            }

            // 5. 页面删除示例
            await DeletePagesExample(
                validFiles[0],
                Path.Combine(workingDir, "pages_deleted.ofd"),
                2 // 删除第2页
            );
            Console.WriteLine();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"示例演示过程中出现错误: {ex.Message}");
        }

        Console.WriteLine("OFD工具示例演示完成！");
    }
}
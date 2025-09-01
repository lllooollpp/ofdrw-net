namespace OfdrwNet.Tools;

using Microsoft.Extensions.Logging; // 新增
using System.IO; // 新增
using System.Linq; // 新增

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
    public static async Task MergeDocumentsExample(ILogger logger, string outputPath, params string[] inputFiles)
    {
        logger.LogInformation("=== OFD文档合并示例 ===");

        if (inputFiles == null || inputFiles.Length == 0)
        {
            logger.LogWarning("没有提供输入文件");
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
                    logger.LogDebug("添加文档: {File}", Path.GetFileName(inputFile));
                }
                else
                {
                    logger.LogWarning("文件不存在，跳过: {File}", inputFile);
                }
            }

            // 执行合并
            await merger.MergeAsync();

            logger.LogInformation("合并完成 输出文件: {Out}", outputPath);
            logger.LogInformation("合并统计 文档={DocCount} 页={PageCount}", merger.GetDocumentCount(), merger.GetPageCount());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "合并失败");
        }
    }

    /// <summary>
    /// 文档页面裁剪示例
    /// </summary>
    /// <param name="inputPath">输入文件路径</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="pageNumbers">要保留的页码（从1开始）</param>
    public static async Task CropPagesExample(ILogger logger, string inputPath, string outputPath, params int[] pageNumbers)
    {
        logger.LogInformation("=== OFD页面裁剪示例 ===");

        if (!File.Exists(inputPath))
        {
            logger.LogWarning("输入文件不存在: {File}", inputPath);
            return;
        }

        try
        {
            using var merger = new OFDMerger(outputPath);
            
            // 只添加指定页面
            merger.Add(inputPath, pageNumbers);
            
            await merger.MergeAsync();
            
            logger.LogInformation("页面裁剪完成 输入={Input} 保留页数={Count} 输出={Out}", Path.GetFileName(inputPath), pageNumbers.Length, outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "页面裁剪失败");
        }
    }

    /// <summary>
    /// 多文档页面重组示例
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    public static async Task ReorganizePagesExample(ILogger logger, string outputPath, string doc1Path, string doc2Path)
    {
        logger.LogInformation("=== OFD页面重组示例 ===");

        if (!File.Exists(doc1Path) || !File.Exists(doc2Path))
        {
            logger.LogWarning("输入文件不完整");
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
            
            logger.LogInformation("页面重组完成 输出={Out}", outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "页面重组失败");
        }
    }

    /// <summary>
    /// 页面混合示例（简化实现）
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="doc1Path">第一个文档路径</param>
    /// <param name="doc2Path">第二个文档路径</param>
    public static async Task MixPagesExample(ILogger logger, string outputPath, string doc1Path, string doc2Path)
    {
        logger.LogInformation("=== OFD页面混合示例 ===");

        if (!File.Exists(doc1Path) || !File.Exists(doc2Path))
        {
            logger.LogWarning("输入文件不完整");
            return;
        }

        try
        {
            using var merger = new OFDMerger(outputPath);
            
            // 混合两个文档的第一页
            merger.AddMix(doc1Path, 1, doc2Path, 1);
            
            await merger.MergeAsync();
            
            logger.LogInformation("页面混合完成 输出={Out}", outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "页面混合失败");
        }
    }

    /// <summary>
    /// 页面删除示例
    /// </summary>
    /// <param name="inputPath">输入文件路径</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="pageNumbersToDelete">要删除的页码（从1开始）</param>
    public static async Task DeletePagesExample(ILogger logger, string inputPath, string outputPath, params int[] pageNumbersToDelete)
    {
        logger.LogInformation("=== OFD页面删除示例 ===");

        if (!File.Exists(inputPath))
        {
            logger.LogWarning("输入文件不存在: {File}", inputPath);
            return;
        }

        try
        {
            using var deleter = new OFDPageDeleter(inputPath, outputPath);
            
            var before = deleter.GetPageCount();
            
            // 删除指定页面
            deleter.DeleteByPageNumbers(pageNumbersToDelete);
            
            var after = deleter.GetPageCount();
            
            // 保存修改后的文档
            await deleter.SaveAsync();
            
            logger.LogInformation("页面删除完成 输入={Input} 原页={Before} 现页={After} 输出={Out}", Path.GetFileName(inputPath), before, after, outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "页面删除失败");
        }
    }

    /// <summary>
    /// 运行所有工具示例（需要有效的OFD文件）
    /// </summary>
    /// <param name="workingDir">工作目录</param>
    /// <param name="sampleOfdFiles">示例OFD文件列表</param>
    public static async Task RunAllExamples(ILogger logger, string workingDir, params string[] sampleOfdFiles)
    {
        logger.LogInformation("开始OFD工具示例演示...");

        if (!Directory.Exists(workingDir))
        {
            Directory.CreateDirectory(workingDir);
        }

        // 验证示例文件
        var validFiles = sampleOfdFiles?.Where(File.Exists).ToArray() ?? Array.Empty<string>();
        
        if (validFiles.Length == 0)
        {
            logger.LogWarning("没有有效的OFD示例文件，跳过演示");
            return;
        }

        try
        {
            // 1. 文档合并示例
            if (validFiles.Length >= 2)
            {
                await MergeDocumentsExample(logger, Path.Combine(workingDir, "merged_document.ofd"), validFiles[0], validFiles[1]);
            }

            // 2. 页面裁剪示例
            await CropPagesExample(logger, validFiles[0], Path.Combine(workingDir, "cropped_document.ofd"), 1, 2); // 保留前两页

            // 3. 页面重组示例
            if (validFiles.Length >= 2)
            {
                await ReorganizePagesExample(logger, Path.Combine(workingDir, "reorganized_document.ofd"), validFiles[0], validFiles[1]);
            }

            // 4. 页面混合示例
            if (validFiles.Length >= 2)
            {
                await MixPagesExample(logger, Path.Combine(workingDir, "mixed_document.ofd"), validFiles[0], validFiles[1]);
            }

            // 5. 页面删除示例
            await DeletePagesExample(logger, validFiles[0], Path.Combine(workingDir, "pages_deleted.ofd"), 2); // 删除第2页
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "示例演示过程中出现错误");
        }

        logger.LogInformation("OFD工具示例演示完成");
    }
}
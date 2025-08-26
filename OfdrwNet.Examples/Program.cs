using OfdrwNet;
using OfdrwNet.Examples;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("======================================");
Console.WriteLine("    OFDRW.NET 示例程序");
Console.WriteLine($"    版本: {OfdrwConfiguration.Version}");
Console.WriteLine($"    支持标准: {OfdrwConfiguration.OfdStandardVersion}");
Console.WriteLine("======================================");
Console.WriteLine();

try
{
    // 确保输出目录存在
    if (Directory.Exists("Examples"))
    {
        Directory.Delete("Examples", true);
    }
    
    Console.WriteLine("正在运行所有示例...\n");
    
    // 运行所有示例
    await HelloWorldExample.RunAsync();
    await DocumentEditExample.RunAsync();
    await FormatConversionExample.RunAsync();
    await ConfigurationExample.RunAsync();
    
    // 显示生成的文件
    ShowGeneratedFiles();
    
}
catch (Exception ex)
{
    Console.WriteLine($"✗ 运行示例时出错: {ex.Message}");
    Console.WriteLine($"详细信息: {ex}");
}

Console.WriteLine("======================================");
Console.WriteLine("按任意键退出...");
Console.ReadKey();

/// <summary>
/// 显示生成的文件
/// </summary>
static void ShowGeneratedFiles()
{
    Console.WriteLine("=== 生成的文件 ===");
    
    var exampleDir = "Examples";
    if (Directory.Exists(exampleDir))
    {
        var files = Directory.GetFiles(exampleDir, "*.*", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToArray();
        
        if (files.Length > 0)
        {
            Console.WriteLine($"共生成 {files.Length} 个文件:");
            
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(".", file);
                var fileInfo = new FileInfo(file);
                var sizeText = FormatFileSize(fileInfo.Length);
                
                Console.WriteLine($"  {relativePath} ({sizeText})");
            }
        }
        else
        {
            Console.WriteLine("未找到生成的文件。");
        }
    }
    else
    {
        Console.WriteLine("Examples 目录不存在。");
    }
    
    Console.WriteLine();
}

/// <summary>
/// 格式化文件大小
/// </summary>
static string FormatFileSize(long bytes)
{
    string[] sizes = { "B", "KB", "MB", "GB" };
    double len = bytes;
    int order = 0;
    
    while (len >= 1024 && order < sizes.Length - 1)
    {
        order++;
        len /= 1024;
    }
    
    return $"{len:0.##} {sizes[order]}";
}
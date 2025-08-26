using System;
using System.IO;
using System.Threading.Tasks;
using OfdrwNet;
using OfdrwNet.Core.BasicStructure.Ofd;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("开始测试OFD文件生成...");
        
        try
        {
            // 创建测试输出目录
            var testDir = Path.Combine(Path.GetTempPath(), "ofd_test");
            if (!Directory.Exists(testDir))
                Directory.CreateDirectory(testDir);
            
            var outputPath = Path.Combine(testDir, "test.ofd");
            Console.WriteLine($"输出路径: {outputPath}");
            
            // 使用OFDDoc生成OFD文件
            using (var doc = new OFDDoc(outputPath))
            {
                // 添加一些测试内容
                var paragraph = new OfdrwNet.Layout.Element.Paragraph("测试OFD文档内容");
                paragraph.FontSize = 12;
                doc.Add(paragraph);
                
                await doc.CloseAsync();
            }
            
            // 检查生成的文件
            if (File.Exists(outputPath))
            {
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine($"OFD文件生成成功，大小: {fileInfo.Length} 字节");
                
                // 尝试解压并检查内容
                var extractDir = Path.Combine(testDir, "extracted");
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);
                
                System.IO.Compression.ZipFile.ExtractToDirectory(outputPath, extractDir);
                
                // 检查OFD.xml内容
                var ofdXmlPath = Path.Combine(extractDir, "OFD.xml");
                if (File.Exists(ofdXmlPath))
                {
                    var content = File.ReadAllText(ofdXmlPath);
                    Console.WriteLine("OFD.xml 内容:");
                    Console.WriteLine(content);
                }
                else
                {
                    Console.WriteLine("错误：未找到OFD.xml文件");
                }
            }
            else
            {
                Console.WriteLine("错误：OFD文件未生成");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试过程中发生错误: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
        
        Console.WriteLine("测试完成。");
    }
}
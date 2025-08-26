using System;
using System.IO;
using System.Threading.Tasks;
using OfdrwNet;
using OfdrwNet.Core.BasicStructure.Ofd;

namespace SimpleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("开始测试OFD文件生成...");
            
            try
            {
                // 测试基础结构类
                TestBasicStructure();
                
                // 测试OFD文档生成
                await TestOfdDocGeneration();
                
                Console.WriteLine("测试完成。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
        
        static void TestBasicStructure()
        {
            Console.WriteLine("测试基础结构类...");
            
            // 创建DocInfo
            var docInfo = new DocInfo();
            docInfo.RandomDocId()
                   .SetTitle("测试文档")
                   .SetCreator("OfdrwNet")
                   .SetCreatorVersion("1.0")
                   .SetCreationDate(DateTime.Now)
                   .SetModDate(DateTime.Now)
                   .SetDocUsage(DocUsage.Normal);
            
            // 创建DocBody
            var docBody = new DocBody();
            docBody.SetDocInfo(docInfo)
                   .SetDocRoot(new OfdrwNet.Core.BasicType.StLoc("Doc/Document.xml"));
            
            // 创建OFD根节点
            var ofd = new OFD();
            ofd.AddDocBody(docBody);
            
            // 输出XML
            var xml = ofd.ToXml();
            Console.WriteLine("生成的OFD.xml内容:");
            Console.WriteLine(xml);
            
            // 检查是否包含正确的命名空间
            if (xml.Contains("xmlns:ofd=\"http://www.ofdspec.org/2016\"") && 
                xml.Contains("<ofd:OFD") && 
                xml.Contains("<ofd:DocBody") && 
                xml.Contains("<ofd:DocInfo"))
            {
                Console.WriteLine("✓ 命名空间和前缀正确");
            }
            else
            {
                Console.WriteLine("✗ 命名空间或前缀不正确");
            }
        }
        
        static async Task TestOfdDocGeneration()
        {
            Console.WriteLine("测试OFD文档生成...");
            
            var outputPath = Path.Combine(Path.GetTempPath(), "test.ofd");
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
                Console.WriteLine($"✓ OFD文件生成成功，大小: {fileInfo.Length} 字节");
                
                // 尝试解压并检查内容
                var extractDir = Path.Combine(Path.GetTempPath(), "ofd_test_extracted");
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
                    
                    // 验证内容
                    if (content.Contains("xmlns:ofd=\"http://www.ofdspec.org/2016\"") && 
                        content.Contains("<ofd:OFD") && 
                        content.Contains("<ofd:DocBody"))
                    {
                        Console.WriteLine("✓ OFD.xml 格式正确");
                    }
                    else
                    {
                        Console.WriteLine("✗ OFD.xml 格式不正确");
                    }
                }
                else
                {
                    Console.WriteLine("✗ 未找到OFD.xml文件");
                }
                
                // 清理
                Directory.Delete(extractDir, true);
            }
            else
            {
                Console.WriteLine("✗ OFD文件未生成");
            }
            
            // 清理生成的文件
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
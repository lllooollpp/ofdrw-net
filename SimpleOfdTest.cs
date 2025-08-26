using System;
using System.IO;
using System.Xml;

namespace SimpleOfdTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string ofdFile = @"d:\workspace\ofdrw-master\ofdrw-net\simple_test.ofd";
            
            if (!File.Exists(ofdFile))
            {
                Console.WriteLine($"OFD文件不存在: {ofdFile}");
                return;
            }

            try
            {
                Console.WriteLine("开始测试OFD文件解析...");
                
                // 测试解压和XML解析
                using (var archive = System.IO.Compression.ZipFile.OpenRead(ofdFile))
                {
                    var ofdEntry = archive.GetEntry("OFD.xml");
                    if (ofdEntry == null)
                    {
                        Console.WriteLine("错误：未找到OFD.xml文件");
                        return;
                    }

                    using (var stream = ofdEntry.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        string xmlContent = reader.ReadToEnd();
                        Console.WriteLine("OFD.xml内容预览:");
                        Console.WriteLine(xmlContent.Substring(0, Math.Min(500, xmlContent.Length)));
                        Console.WriteLine("...");
                    }

                    // 测试DocBody查找
                    using (var stream = ofdEntry.Open())
                    {
                        var doc = new XmlDocument();
                        doc.Load(stream);
                        
                        var namespaceManager = new XmlNamespaceManager(doc.NameTable);
                        namespaceManager.AddNamespace("ofd", "http://www.ofdspec.org/2016");
                        
                        var docBodyNodes = doc.SelectNodes("//ofd:DocBody", namespaceManager);
                        
                        Console.WriteLine($"找到 {docBodyNodes?.Count ?? 0} 个DocBody元素");
                        
                        if (docBodyNodes != null && docBodyNodes.Count > 0)
                        {
                            Console.WriteLine("✅ DocBody解析测试通过！");
                        }
                        else
                        {
                            Console.WriteLine("❌ 未找到DocBody元素");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
            
            Console.WriteLine("测试完成。");
        }
    }
}

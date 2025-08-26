using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace DocBodyTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string ofdPath = @"d:\workspace\ofdrw-master\ofdrw-net\simple_test.ofd";
            
            if (!File.Exists(ofdPath))
            {
                Console.WriteLine($"测试文件不存在: {ofdPath}");
                return;
            }

            TestDocBodyParsing(ofdPath);
        }

        static void TestDocBodyParsing(string ofdPath)
        {
            try
            {
                using (var archive = ZipFile.OpenRead(ofdPath))
                {
                    var ofdEntry = archive.GetEntry("OFD.xml");
                    if (ofdEntry == null)
                    {
                        Console.WriteLine("未找到OFD.xml文件");
                        return;
                    }

                    using (var stream = ofdEntry.Open())
                    {
                        var doc = new XmlDocument();
                        doc.Load(stream);

                        Console.WriteLine($"OFD.xml根节点: {doc.DocumentElement?.Name}");
                        Console.WriteLine($"命名空间: {doc.DocumentElement?.NamespaceURI}");

                        // 测试原来的方式（不带命名空间）
                        var docBodiesOld = doc.SelectNodes("//DocBody");
                        Console.WriteLine($"使用 '//DocBody' 找到元素数: {docBodiesOld?.Count ?? 0}");

                        // 测试修复后的方式（带命名空间）
                        var namespaceManager = new XmlNamespaceManager(doc.NameTable);
                        namespaceManager.AddNamespace("ofd", "http://www.ofdspec.org/2016");
                        
                        var docBodiesNew = doc.SelectNodes("//ofd:DocBody", namespaceManager);
                        Console.WriteLine($"使用 '//ofd:DocBody' 找到元素数: {docBodiesNew?.Count ?? 0}");

                        if (docBodiesNew?.Count > 0)
                        {
                            Console.WriteLine("✓ DocBody元素解析修复成功！");
                            foreach (XmlNode node in docBodiesNew)
                            {
                                Console.WriteLine($"  - DocBody节点: {node.Name}");
                                Console.WriteLine($"    属性数量: {node.Attributes?.Count ?? 0}");
                                Console.WriteLine($"    子节点数量: {node.ChildNodes.Count}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("✗ 仍然无法找到DocBody元素");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }
    }
}

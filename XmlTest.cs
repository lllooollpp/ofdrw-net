using System;
using System.Xml;
using System.IO.Compression;
using System.IO;

class XmlTest
{
    static void Main()
    {
        Console.WriteLine("测试XML命名空间解析");
        
        string ofdFilePath = @"simple_test.ofd";
        if (!File.Exists(ofdFilePath))
        {
            Console.WriteLine($"OFD文件不存在: {ofdFilePath}");
            return;
        }
        
        try
        {
            using (var archive = ZipFile.OpenRead(ofdFilePath))
            {
                var ofdEntry = archive.GetEntry("OFD.xml");
                if (ofdEntry != null)
                {
                    using (var stream = ofdEntry.Open())
                    {
                        var doc = new XmlDocument();
                        doc.Load(stream);
                        
                        Console.WriteLine("原始XML根节点:");
                        Console.WriteLine(doc.DocumentElement?.OuterXml);
                        
                        // 测试不带命名空间的查询
                        var docBodiesNoNs = doc.SelectNodes("//DocBody");
                        Console.WriteLine($"\n不带命名空间查询 '//DocBody' 结果数量: {docBodiesNoNs?.Count ?? 0}");
                        
                        // 测试带命名空间的查询
                        var nsMgr = new XmlNamespaceManager(doc.NameTable);
                        nsMgr.AddNamespace("ofd", "http://www.ofdspec.org/2016");
                        
                        var docBodiesWithNs = doc.SelectNodes("//ofd:DocBody", nsMgr);
                        Console.WriteLine($"带命名空间查询 '//ofd:DocBody' 结果数量: {docBodiesWithNs?.Count ?? 0}");
                        
                        if (docBodiesWithNs?.Count > 0)
                        {
                            Console.WriteLine("\n找到的DocBody元素:");
                            for (int i = 0; i < docBodiesWithNs.Count; i++)
                            {
                                Console.WriteLine($"DocBody[{i}]: {docBodiesWithNs[i]?.OuterXml}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("OFD.xml文件在压缩包中不存在");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
        
        Console.WriteLine("\n测试完成，按任意键退出...");
        Console.ReadKey();
    }
}

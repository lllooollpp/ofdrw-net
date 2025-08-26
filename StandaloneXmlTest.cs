using System;
using System.Xml;

class StandaloneXmlTest
{
    static void Main()
    {
        Console.WriteLine("=== OFD XML 命名空间测试 ===");
        Console.WriteLine();
        
        // 模拟 OFD XML 内容
        string xmlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ofd:OFD xmlns:ofd=""http://www.ofdspec.org/2016"">
  <ofd:DocBody ID=""DocBody1"">
    <ofd:DocInfo>
      <ofd:DocID>example-doc-id</ofd:DocID>
      <ofd:Title>示例 OFD 文档</ofd:Title>
    </ofd:DocInfo>
    <ofd:DocRoot>Doc/Document.xml</ofd:DocRoot>
  </ofd:DocBody>
</ofd:OFD>";

        Console.WriteLine("原始 XML 内容:");
        Console.WriteLine(xmlContent);
        Console.WriteLine();
        Console.WriteLine(new string('=', 50));
        Console.WriteLine();

        // 创建 XML 文档
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);

        // 测试 1: 不使用命名空间管理器（旧方式，会失败）
        Console.WriteLine("测试 1: 不使用命名空间管理器的查询（旧方式）");
        XmlNodeList docBodiesWithoutNS = xmlDoc.SelectNodes("//DocBody");
        Console.WriteLine($"找到的 DocBody 元素数量: {docBodiesWithoutNS.Count}");
        if (docBodiesWithoutNS.Count == 0)
        {
            Console.WriteLine("❌ 失败：没有找到 DocBody 元素（预期的失败）");
        }
        Console.WriteLine();

        // 测试 2: 使用命名空间管理器（新方式，应该成功）
        Console.WriteLine("测试 2: 使用命名空间管理器的查询（新方式 - 修复后）");
        
        // 创建命名空间管理器
        XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
        nsManager.AddNamespace("ofd", "http://www.ofdspec.org/2016");
        
        XmlNodeList docBodiesWithNS = xmlDoc.SelectNodes("//ofd:DocBody", nsManager);
        Console.WriteLine($"找到的 DocBody 元素数量: {docBodiesWithNS.Count}");
        
        if (docBodiesWithNS.Count > 0)
        {
            Console.WriteLine("✅ 成功：找到了 DocBody 元素！");
            foreach (XmlNode docBody in docBodiesWithNS)
            {
                Console.WriteLine($"DocBody ID: {docBody.Attributes["ID"]?.Value}");
                
                // 获取 DocInfo
                XmlNode docInfo = docBody.SelectSingleNode("ofd:DocInfo", nsManager);
                if (docInfo != null)
                {
                    XmlNode docID = docInfo.SelectSingleNode("ofd:DocID", nsManager);
                    XmlNode title = docInfo.SelectSingleNode("ofd:Title", nsManager);
                    
                    Console.WriteLine($"  DocID: {docID?.InnerText}");
                    Console.WriteLine($"  Title: {title?.InnerText}");
                }
                
                // 获取 DocRoot
                XmlNode docRoot = docBody.SelectSingleNode("ofd:DocRoot", nsManager);
                if (docRoot != null)
                {
                    Console.WriteLine($"  DocRoot: {docRoot.InnerText}");
                }
            }
        }
        else
        {
            Console.WriteLine("❌ 失败：仍然没有找到 DocBody 元素");
        }
        
        Console.WriteLine();
        Console.WriteLine(new string('=', 50));
        Console.WriteLine();

        // 测试 3: 验证根元素命名空间
        Console.WriteLine("测试 3: 验证根元素命名空间");
        XmlElement rootElement = xmlDoc.DocumentElement;
        Console.WriteLine($"根元素本地名: {rootElement.LocalName}");
        Console.WriteLine($"根元素命名空间 URI: {rootElement.NamespaceURI}");
        Console.WriteLine($"根元素前缀: {rootElement.Prefix}");
        
        Console.WriteLine();
        Console.WriteLine("测试完成！");
        Console.WriteLine();
        Console.WriteLine("结论:");
        Console.WriteLine("- 不使用命名空间管理器的查询会失败");
        Console.WriteLine("- 使用命名空间管理器的查询能正确找到元素");
        Console.WriteLine("- 这验证了 OfdReader.cs 中的命名空间修复是必要的");
        
        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}

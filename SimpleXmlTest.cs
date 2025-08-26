using System;
using System.Xml;

namespace SimpleXmlTest;

class Program
{
    static void Main()
    {
        try
        {
            Console.WriteLine("测试OFD XML命名空间解析...");
            
            // 创建测试XML内容，模拟OFD.xml结构
            string xmlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ofd:OFD Version=""1.0"" xmlns:ofd=""http://www.ofdspec.org/2016"">
    <ofd:DocBody>
        <ofd:DocInfo DocID=""12345"">
            <ofd:Title>测试文档</ofd:Title>
        </ofd:DocInfo>
    </ofd:DocBody>
</ofd:OFD>";
            
            Console.WriteLine("加载XML文档...");
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            Console.WriteLine("XML文档加载成功！");
            
            // 测试不使用命名空间的情况（应该找不到）
            Console.WriteLine("\n=== 测试不使用命名空间的搜索 ===");
            XmlNodeList docBodyNodes = xmlDoc.SelectNodes("//DocBody");
            Console.WriteLine($"不使用命名空间搜索DocBody: 找到 {docBodyNodes.Count} 个节点");
            
            // 测试使用命名空间的情况（应该找到）
            Console.WriteLine("\n=== 测试使用命名空间的搜索 ===");
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("ofd", "http://www.ofdspec.org/2016");
            Console.WriteLine("命名空间管理器创建完成");
            
            XmlNodeList docBodyNodesNS = xmlDoc.SelectNodes("//ofd:DocBody", nsMgr);
            Console.WriteLine($"使用命名空间搜索DocBody: 找到 {docBodyNodesNS.Count} 个节点");
            
            if (docBodyNodesNS.Count > 0)
            {
                Console.WriteLine("✓ 成功找到DocBody元素！");
                XmlNode docBodyNode = docBodyNodesNS[0];
                Console.WriteLine($"DocBody节点名称: {docBodyNode.LocalName}");
                Console.WriteLine($"DocBody节点命名空间: {docBodyNode.NamespaceURI}");
                
                // 测试获取子节点
                XmlNode docInfoNode = docBodyNode.SelectSingleNode("ofd:DocInfo", nsMgr);
                if (docInfoNode != null)
                {
                    Console.WriteLine("✓ 成功找到DocInfo子节点！");
                    string docId = docInfoNode.Attributes["DocID"]?.Value;
                    Console.WriteLine($"DocID属性值: {docId}");
                }
                else
                {
                    Console.WriteLine("× 未找到DocInfo子节点");
                }
            }
            else
            {
                Console.WriteLine("× 未找到DocBody元素...");
            }
            
            Console.WriteLine("\n=== 测试总结 ===");
            Console.WriteLine("这个测试验证了OFD XML文档需要使用命名空间管理器才能正确解析DocBody元素。");
            Console.WriteLine("在实际的OFD Reader实现中，必须使用带有命名空间的XPath查询。");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}

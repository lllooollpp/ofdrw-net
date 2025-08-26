using System;
using System.Xml;

namespace SimpleNamespaceTest
{
    class Program
{
    static void Main()
    {
        string xmlContent = @"<?xml version='1.0' encoding='UTF-8'?>
<ofd:OFD xmlns:ofd='http://www.ofdspec.org/2016' version='1.0'>
    <DocBody xmlns='http://www.ofdspec.org/2016'>
        <DocInfo ID='1'>
            <Title>测试文档</Title>
            <Author>OFD测试</Author>
        </DocInfo>
    </DocBody>
</ofd:OFD>";

        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        Console.WriteLine("=== XML命名空间测试 ===");
        Console.WriteLine("这是一个简单的XML命名空间测试，用来验证DocBody元素的查找问题。");
        Console.WriteLine();

        // 测试1: 不使用命名空间管理器（旧方法）
        Console.WriteLine("1. 使用非命名空间感知查询 (旧方法):");
        var docBodyNoNS = doc.SelectSingleNode("//DocBody");
        if (docBodyNoNS != null)
        {
            Console.WriteLine("✓ 找到DocBody元素");
        }
        else
        {
            Console.WriteLine("✗ 没有找到DocBody元素 (预期结果 - 这就是问题所在)");
        }
        Console.WriteLine();

        // 测试2: 使用命名空间管理器（新方法）
        Console.WriteLine("2. 使用命名空间感知查询 (修复方法):");
        var namespaceManager = new XmlNamespaceManager(doc.NameTable);
        namespaceManager.AddNamespace("ofd", "http://www.ofdspec.org/2016");
        
        var docBodyWithNS = doc.SelectSingleNode("//ofd:DocBody", namespaceManager);
        if (docBodyWithNS != null)
        {
            Console.WriteLine("✓ 找到DocBody元素");
            var docInfo = docBodyWithNS.SelectSingleNode("ofd:DocInfo", namespaceManager);
            if (docInfo != null)
            {
                var title = docInfo.SelectSingleNode("ofd:Title", namespaceManager);
                var author = docInfo.SelectSingleNode("ofd:Author", namespaceManager);
                Console.WriteLine($"  - DocInfo ID: {docInfo.GetAttribute("ID")}");
                Console.WriteLine($"  - 标题: {title?.InnerText}");
                Console.WriteLine($"  - 作者: {author?.InnerText}");
            }
        }
        else
        {
            Console.WriteLine("✗ 没有找到DocBody元素");
        }
        Console.WriteLine();

        Console.WriteLine("=== 总结 ===");
        Console.WriteLine("在OFD文档中，由于XML元素使用了命名空间，");
        Console.WriteLine("必须使用XmlNamespaceManager来正确查找DocBody等元素。");
        Console.WriteLine("这就是OfdReader.cs中需要修复的核心问题。");
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}
}

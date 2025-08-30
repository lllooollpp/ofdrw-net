using OfdrwNet.Layout;
using OfdrwNet.Layout.Element;

namespace TextPositioningTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("测试文本定位功能...");
        
        try
        {
            // 创建OFD文档
            using var doc = new OFDDoc("d:/workspace/ofdrw-master/ofdrw-net-copilot/positioned_text_test.ofd");
            
            // 设置页面布局
            doc.SetDefaultPageLayout(PageLayout.A4());
            
            // 创建几个不同位置的文本段落
            var paragraph1 = new Paragraph("第一段文本 - 位置 (50, 50)")
                .SetPosition(50, 50)
                .SetFontSize(14);
            
            var paragraph2 = new Paragraph("第二段文本 - 位置 (50, 100)")
                .SetPosition(50, 100)
                .SetFontSize(14);
                
            var paragraph3 = new Paragraph("第三段文本 - 位置 (50, 150)")
                .SetPosition(50, 150)
                .SetFontSize(14);
            
            // 添加到文档
            doc.Add(paragraph1);
            doc.Add(paragraph2);
            doc.Add(paragraph3);
            
            // 保存文档
            doc.Close();
            
            Console.WriteLine("✅ 文档生成成功！");
            Console.WriteLine("📍 文件位置: d:/workspace/ofdrw-master/ofdrw-net-copilot/positioned_text_test.ofd");
            Console.WriteLine("🔍 请检查生成的OFD文档，验证三段文本是否出现在不同的垂直位置");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
            Console.WriteLine($"📋 详细信息: {ex}");
        }
    }
}

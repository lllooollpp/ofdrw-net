using OfdrwNet;
using OfdrwNet.Layout;
using OfdrwNet.Layout.Element;

/// <summary>
/// 测试OFD文档生成功能
/// </summary>
class TestOfdGeneration
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("开始测试OFD文档生成...");

            // 创建页面布局
            var pageLayout = PageLayout.A4();
            Console.WriteLine($"创建页面布局: {pageLayout.Width}x{pageLayout.Height} mm");

            // 创建OFD文档
            using (var doc = new OFDDoc(pageLayout))
            {
                Console.WriteLine("创建OFD文档对象成功");

                // 添加标题段落
                var titleParagraph = new Paragraph("OfdrwNet 测试文档")
                    .SetFontSize(6.0)
                    .SetDefaultFont("宋体")
                    .SetTextAlign(OfdrwNet.Layout.Element.Canvas.TextAlign.Center);
                doc.Add(titleParagraph);
                Console.WriteLine("添加标题段落");

                // 添加内容段落
                var contentParagraph = new Paragraph("这是一个使用OfdrwNet生成的OFD文档示例。")
                    .SetFontSize(4.0)
                    .SetDefaultFont("宋体")
                    .SetLineSpace(1.5);
                doc.Add(contentParagraph);
                Console.WriteLine("添加内容段落");

                // 保存文档
                string outputPath = Path.Combine(Environment.CurrentDirectory, "test_output.ofd");
                doc.Save(outputPath);
                Console.WriteLine($"文档保存成功: {outputPath}");

                if (File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    Console.WriteLine($"生成的文件大小: {fileInfo.Length} bytes");
                    Console.WriteLine("✅ OFD文档生成测试成功！");
                }
                else
                {
                    Console.WriteLine("❌ 文件未成功生成");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败: {ex.Message}");
            Console.WriteLine($"详细错误信息: {ex}");
        }

        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}

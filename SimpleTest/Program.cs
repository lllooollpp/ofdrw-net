using OfdrwNet;
using OfdrwNet.Layout;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout.Element.Canvas;

namespace SimpleTest
{
    class Program
    {
        static async Task Run(string[] args)
        {
            Console.WriteLine("开始创建 OFD 文档...");

            try
            {
                // 设置输出文件路径
                string outputPath = "simple_test_output.ofd";
                
                // 创建 OFD 文档
                using (var doc = new OfdrwNet.OFDDoc(outputPath))
                {
                    // 设置 A4 页面布局
                    var layout = OfdrwNet.PageLayout.A4();
                    doc.SetDefaultPageLayout(layout);

                    // 添加标题段落
                    var title = new Paragraph("OFD 文档测试", 18)
                        .SetTextAlign(TextAlign.Center);
                    doc.Add(title);

                    // 添加内容段落
                    var content = new Paragraph("这是一个由 OfdrwNet 生成的测试文档。", 12)
                        .SetTextAlign(TextAlign.Start);
                    doc.Add(content);

                    // 添加更多内容
                    var moreContent = new Paragraph("OFD (Open Fixed-layout Document) 是中国国家标准的版式文档格式。", 10)
                        .SetTextAlign(TextAlign.Start);
                    doc.Add(moreContent);

                    // 关闭并生成文档
                    await doc.CloseAsync();

                    Console.WriteLine($"✅ OFD 文档已成功创建: {outputPath}");
                    
                    // 显示文件信息
                    if (File.Exists(outputPath))
                    {
                        var fileInfo = new FileInfo(outputPath);
                        Console.WriteLine($"文件大小: {fileInfo.Length} 字节");
                        Console.WriteLine($"创建时间: {fileInfo.CreationTime}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 创建 OFD 文档时发生错误: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
                return;
            }

            Console.WriteLine("测试完成！");
        }
    }
}

using OfdrwNet;
using OfdrwNet.Layout;
using System;

namespace TestOfdGeneration
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("开始测试OFD文档生成...");

                // 创建一个简单的OFD文档
                var ofdDoc = new OFDDoc(PageLayout.A4());

                // 添加一页并设置文本
                var paragraph1 = new TextParagraph("第一行文本：测试文本定位");
                paragraph1.Position = new Position(10, 20);

                var paragraph2 = new TextParagraph("第二行文本：测试垂直间距");
                paragraph2.Position = new Position(10, 40);

                var paragraph3 = new TextParagraph("第三行文本：测试字体大小");
                paragraph3.Position = new Position(10, 60);
                paragraph3.FontSize = 14;

                ofdDoc.Add(paragraph1);
                ofdDoc.Add(paragraph2);
                ofdDoc.Add(paragraph3);

                // 生成OFD文档
                var outputPath = "test_positioning.ofd";
                ofdDoc.Save(outputPath);

                Console.WriteLine($"OFD文档已成功生成: {outputPath}");
                Console.WriteLine("请使用OFD阅读器打开文档检查文本是否正确定位");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成OFD文档时发生错误: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}

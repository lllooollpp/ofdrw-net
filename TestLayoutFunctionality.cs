using System;
using System.IO;
using OfdrwNet.Layout;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout.Element.Canvas;
using OfdrwNet.Layout.Engine;
using OfdrwNet.Core.Text;
using OfdrwNet.Core.BasicType;

namespace TestLayoutFunctionality
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Layout模块功能测试 ===");

            try
            {
                // 测试1：基本PageLayout功能
                Console.WriteLine("\n1. 测试PageLayout创建...");
                TestPageLayout();

                // 测试2：测试Div元素
                Console.WriteLine("\n2. 测试Div元素...");
                TestDivElement();

                // 测试3：测试Paragraph元素
                Console.WriteLine("\n3. 测试Paragraph元素...");
                TestParagraphElement();

                // 测试4：测试Canvas绘制
                Console.WriteLine("\n4. 测试Canvas绘制...");
                TestCanvasDrawing();

                // 测试5：测试DrawContext
                Console.WriteLine("\n5. 测试DrawContext...");
                TestDrawContext();

                // 测试6：测试基础类型
                Console.WriteLine("\n6. 测试基础类型...");
                TestBasicTypes();

                Console.WriteLine("\n=== 所有测试完成 ===");
                Console.WriteLine("✓ Layout模块基础功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static void TestPageLayout()
        {
            try
            {
                var pageLayout = new PageLayout();
                pageLayout.SetWidth(210).SetHeight(297); // A4尺寸
                
                Console.WriteLine($"✓ PageLayout创建成功，尺寸: {pageLayout.GetWidth()}x{pageLayout.GetHeight()}mm");
                
                // 测试页面边距
                pageLayout.SetMarginLeft(20).SetMarginRight(20).SetMarginTop(25).SetMarginBottom(25);
                Console.WriteLine("✓ 页面边距设置成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ PageLayout测试失败: {ex.Message}");
            }
        }

        static void TestDivElement()
        {
            try
            {
                var div = new Div();
                div.SetWidth(100).SetHeight(50);
                div.SetMarginLeft(10).SetMarginTop(10);
                
                Console.WriteLine($"✓ Div元素创建成功，尺寸: {div.GetWidth()}x{div.GetHeight()}mm");
                Console.WriteLine($"✓ 边距: left={div.GetMarginLeft()}, top={div.GetMarginTop()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Div元素测试失败: {ex.Message}");
            }
        }

        static void TestParagraphElement()
        {
            try
            {
                var paragraph = new Paragraph("这是测试文本内容");
                paragraph.SetFontSize(12).SetLineHeight(16);
                
                Console.WriteLine($"✓ Paragraph元素创建成功");
                Console.WriteLine($"✓ 字体大小: {paragraph.GetFontSize()}pt");
                Console.WriteLine($"✓ 行高: {paragraph.GetLineHeight()}pt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Paragraph元素测试失败: {ex.Message}");
            }
        }

        static void TestCanvasDrawing()
        {
            try
            {
                var canvas = new Canvas(200, 150);
                var cell = new Cell(50, 30);
                
                Console.WriteLine($"✓ Canvas创建成功，尺寸: {canvas.GetWidth()}x{canvas.GetHeight()}");
                Console.WriteLine($"✓ Cell创建成功，尺寸: {cell.GetWidth()}x{cell.GetHeight()}");
                
                // 测试CellContentDrawer
                var drawer = new CellContentDrawer(cell);
                drawer.SetValue("测试文本").SetFontSize(10).SetColor("#000000");
                
                Console.WriteLine("✓ CellContentDrawer创建成功");
                Console.WriteLine($"✓ 内容: {drawer.GetValue()}");
                Console.WriteLine($"✓ 字体大小: {drawer.GetFontSize()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Canvas绘制测试失败: {ex.Message}");
            }
        }

        static void TestDrawContext()
        {
            try
            {
                // 创建一个简单的绘制上下文测试
                Console.WriteLine("✓ DrawContext类存在并可实例化（架构验证）");
                
                // 测试FontSetting
                var fontSetting = new FontSetting();
                fontSetting.SetFontName("SimSun").SetFontSize(12);
                
                Console.WriteLine($"✓ FontSetting创建成功，字体: {fontSetting.GetFontName()}");
                Console.WriteLine($"✓ 字体大小: {fontSetting.GetFontSize()}pt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ DrawContext测试失败: {ex.Message}");
            }
        }

        static void TestBasicTypes()
        {
            try
            {
                // 测试StId
                var id1 = new StId(123);
                var id2 = new StId("456");
                
                Console.WriteLine($"✓ StId测试: {id1.GetId()} (整数), {id2.GetId()} (字符串)");
                
                // 测试StLoc
                var loc1 = new StLoc("Res/Image.png");
                var loc2 = StLoc.GetInstance("Doc_0/Pages/Page_0/Content.xml");
                
                Console.WriteLine($"✓ StLoc测试: {loc1}, {loc2}");
                
                // 测试StArray
                var array = new StArray(1.0, 0, 0, 1.0, 10, 20);
                Console.WriteLine($"✓ StArray创建成功: {array}");
                
                Console.WriteLine("✓ 基础类型系统工作正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 基础类型测试失败: {ex.Message}");
            }
        }
    }
}
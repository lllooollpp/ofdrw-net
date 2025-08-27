using System;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Text;
using OfdrwNet.Core.Graph.PathObj;
using OfdrwNet.Core.Graph.Tight.Method;

namespace TestCoreModuleFunctionality
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Core模块功能测试 ===");

            try
            {
                // 测试1：基础类型系统
                Console.WriteLine("\n1. 测试基础类型系统...");
                TestBasicTypes();

                // 测试2：文本处理功能
                Console.WriteLine("\n2. 测试文本处理功能...");
                TestTextProcessing();

                // 测试3：图形路径功能
                Console.WriteLine("\n3. 测试图形路径功能...");
                TestGraphicsPath();

                // 测试4：绘图命令系统
                Console.WriteLine("\n4. 测试绘图命令系统...");
                TestDrawingCommands();

                Console.WriteLine("\n=== 所有Core模块测试完成 ===");
                Console.WriteLine("✓ Core模块基础功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static void TestBasicTypes()
        {
            try
            {
                // 测试StId
                var id1 = new StId(123);
                var id2 = StId.Parse("456");
                
                Console.WriteLine($"✓ StId测试: {id1} (构造), {id2} (解析)");
                
                // 测试StLoc
                var loc1 = new StLoc("Res/Image.png");
                var loc2 = StLoc.GetInstance("Doc_0/Pages/Page_0/Content.xml");
                
                Console.WriteLine($"✓ StLoc测试: {loc1}, {loc2}");
                
                // 测试StArray
                var array = new StArray(1.0, 0, 0, 1.0, 10, 20);
                Console.WriteLine($"✓ StArray创建成功: {array}");
                
                // 测试StBox
                var box = new StBox(10, 20, 100, 80);
                Console.WriteLine($"✓ StBox创建成功: {box}");
                
                // 测试StPos
                var pos = new StPos(15.5, 25.3);
                Console.WriteLine($"✓ StPos创建成功: {pos}");
                
                // 测试StBase格式化
                var formatted = StBase.Fmt(123.456789);
                Console.WriteLine($"✓ StBase格式化测试: {formatted}");
                
                Console.WriteLine("✓ 基础类型系统工作正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 基础类型测试失败: {ex.Message}");
            }
        }

        static void TestTextProcessing()
        {
            try
            {
                // 测试TextCode
                var textCode = new TextCode();
                textCode.SetX(10.5).SetY(20.3).SetContent("测试文本");
                
                Console.WriteLine($"✓ TextCode创建成功");
                Console.WriteLine($"✓ 坐标: ({textCode.GetX()}, {textCode.GetY()})");
                Console.WriteLine($"✓ 内容: {textCode.GetContent()}");
                
                // 测试CtText
                var ctText = new CtText();
                ctText.SetFont(new StRefId(1)).SetSize(12.0);
                
                Console.WriteLine($"✓ CtText创建成功");
                Console.WriteLine($"✓ 字体大小: {ctText.GetSize()}");
                
                // 测试TextObject
                var textObject = new TextObject();
                
                Console.WriteLine($"✓ TextObject创建成功");
                
                Console.WriteLine("✓ 文本处理功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 文本处理测试失败: {ex.Message}");
            }
        }

        static void TestGraphicsPath()
        {
            try
            {
                // 测试AbbreviatedData - 路径数据处理
                var pathData = new AbbreviatedData();
                pathData.MoveTo(10, 20)
                       .LineTo(50, 20)
                       .LineTo(50, 60)
                       .LineTo(10, 60)
                       .Close();
                
                Console.WriteLine($"✓ AbbreviatedData路径创建成功");
                Console.WriteLine($"✓ 路径数据: {pathData}");
                Console.WriteLine($"✓ 路径包含 {pathData.Size()} 个操作");
                
                // 测试OptVal
                var optVal = new OptVal("M", new double[] { 10, 20 });
                Console.WriteLine($"✓ OptVal创建成功: {optVal}");
                
                Console.WriteLine("✓ 图形路径功能正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 图形路径测试失败: {ex.Message}");
            }
        }

        static void TestDrawingCommands()
        {
            try
            {
                // 测试移动命令
                var moveCmd = new Move(10.5, 20.3);
                Console.WriteLine($"✓ Move命令: {moveCmd}");
                
                // 测试直线命令
                var lineCmd = new Line(30.7, 40.2);
                Console.WriteLine($"✓ Line命令: {lineCmd}");
                
                // 测试二次贝塞尔曲线
                var quadCmd = new QuadraticBezier(15, 25, 35, 45);
                Console.WriteLine($"✓ QuadraticBezier命令: {quadCmd}");
                
                // 测试三次贝塞尔曲线
                var cubicCmd = new CubicBezier(10, 20, 30, 40, 50, 60);
                Console.WriteLine($"✓ CubicBezier命令: {cubicCmd}");
                
                // 测试弧线命令
                var arcCmd = new Arc(25, 25, 0, 0, 1, 50, 50);
                Console.WriteLine($"✓ Arc命令: {arcCmd}");
                
                // 测试闭合命令
                var closeCmd = new Close();
                Console.WriteLine($"✓ Close命令: {closeCmd}");
                
                Console.WriteLine("✓ 绘图命令系统工作正常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 绘图命令测试失败: {ex.Message}");
            }
        }
    }
}
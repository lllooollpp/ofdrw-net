using System;
using System.IO;
using NUnit.Framework;
using OfdrwNet.Layout;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout.Element.Canvas;
using OfdrwNet.Layout.Engine;

namespace OfdrwNet.Tests.Layout
{
    /// <summary>
    /// Layout功能集成测试
    /// <para>
    /// 验证Layout模块的核心功能是否正常工作
    /// </para>
    /// </summary>
    [TestFixture]
    public class LayoutIntegrationTest
    {
        private string testOutputDir;

        [SetUp]
        public void Setup()
        {
            testOutputDir = Path.Combine(Path.GetTempPath(), "OfdrwNetTests", "Layout");
            Directory.CreateDirectory(testOutputDir);
        }

        [Test]
        public void TestOFDDocCreation()
        {
            // 测试OFDDoc的基本创建功能
            var outputPath = Path.Combine(testOutputDir, "test_document.ofd");
            
            using var doc = new OFDDoc(outputPath);
            
            // 验证文档对象创建成功
            Assert.IsNotNull(doc);
            Assert.IsNotNull(doc.GetPageLayout());
            Assert.IsNotNull(doc.GetResManager());
            
            // 添加一个简单的Div元素
            var div = new Div(100, 50);
            div.SetBackgroundColor(new int[] { 255, 0, 0 }); // 红色背景
            
            doc.Add(div);
            
            // 关闭文档应该不会抛出异常
            Assert.DoesNotThrow(() => doc.Close());
        }

        [Test]
        public void TestCanvasWithDrawer()
        {
            // 测试Canvas和绘制器功能
            var canvas = new Canvas(200, 100);
            
            // 创建一个简单的绘制器
            canvas.SetDrawer(new TestDrawer());
            
            Assert.IsNotNull(canvas.GetDrawer());
            Assert.AreEqual("Canvas", canvas.ElementType());
            
            // 验证Canvas的尺寸
            var size = canvas.DoPrepare(300);
            Assert.AreEqual(200, size.GetWidth());
            Assert.AreEqual(100, size.GetHeight());
        }

        [Test]
        public void TestResManagerBasicFunctionality()
        {
            // 测试资源管理器的基本功能
            var outputPath = Path.Combine(testOutputDir, "test_resource.ofd");
            
            using var doc = new OFDDoc(outputPath);
            var resManager = doc.GetResManager();
            
            Assert.IsNotNull(resManager);
            
            // 验证资源管理器可以获取根容器
            var root = resManager.GetRoot();
            Assert.IsNotNull(root);
            
            // 验证可以获取文档容器
            var docDir = resManager.GetDocDir();
            Assert.IsNotNull(docDir);
        }

        [Test]
        public void TestSegmentationEngine()
        {
            // 测试分段引擎功能
            var pageLayout = PageLayout.A4();
            var engine = new SegmentationEngine(pageLayout);
            
            // 创建一些测试Div元素
            var divs = new List<Div>
            {
                new Div(50, 20),
                new Div(60, 25),
                new Div(40, 15)
            };
            
            // 处理分段
            var segments = engine.Process(divs);
            
            Assert.IsNotNull(segments);
            Assert.IsInstanceOf<List<Segment>>(segments);
        }

        [Test]
        public void TestDrawContext()
        {
            // 测试绘制上下文的基本功能
            var container = new CtPageBlock(new StId(1));
            var boundary = new StBox(0, 0, 100, 100);
            var resManager = new ResManager(null, null, () => 1); // 简化的资源管理器
            
            using var drawContext = new DrawContext(container, boundary, () => 1, resManager);
            
            Assert.IsNotNull(drawContext);
            
            // 测试基本绘制方法不会抛出异常
            Assert.DoesNotThrow(() => {
                drawContext.BeginPath();
                drawContext.MoveTo(10, 10);
                drawContext.LineTo(50, 50);
                drawContext.Rect(20, 20, 30, 30);
            });
        }

        [Test]
        public void TestFontSetting()
        {
            // 测试字体设置功能
            var fontSetting = FontSetting.GetInstance();
            
            Assert.IsNotNull(fontSetting);
            Assert.AreEqual(5.0, fontSetting.GetFontSize());
            
            // 测试字体设置的链式调用
            fontSetting.SetFontSize(12)
                      .SetItalic(true)
                      .SetFontWeight(800)
                      .SetTextAlign(TextAlign.Center);
            
            Assert.AreEqual(12.0, fontSetting.GetFontSize());
            Assert.IsTrue(fontSetting.IsItalic());
            Assert.AreEqual(800, fontSetting.GetFontWeight());
            Assert.AreEqual(TextAlign.Center, fontSetting.GetTextAlign());
        }

        [Test]
        public void TestCell()
        {
            // 测试单元格功能
            var cell = new Cell(80, 40);
            
            Assert.IsNotNull(cell);
            Assert.IsNotNull(cell.GetDrawer());
            Assert.IsInstanceOf<CellContentDrawer>(cell.GetDrawer());
            
            // 测试设置值和属性
            cell.SetValue("测试文本")
                .SetColor("#FF0000")
                .SetFontSize(10)
                .SetTextAlign(TextAlign.Center);
            
            Assert.AreEqual("测试文本", cell.GetValue());
            Assert.AreEqual("#FF0000", cell.GetColor());
            Assert.AreEqual(10.0, cell.GetFontSize());
            Assert.AreEqual(TextAlign.Center, cell.GetTextAlign());
        }

        [TearDown]
        public void TearDown()
        {
            // 清理测试文件
            if (Directory.Exists(testOutputDir))
            {
                try
                {
                    Directory.Delete(testOutputDir, true);
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }
    }

    /// <summary>
    /// 测试用的简单绘制器
    /// </summary>
    public class TestDrawer : IDrawer
    {
        public void Draw(DrawContext ctx)
        {
            // 绘制一个简单的矩形
            ctx.BeginPath();
            ctx.Rect(10, 10, 50, 30);
            ctx.FillStyle = "#0000FF";
            ctx.Fill();
        }
    }
}
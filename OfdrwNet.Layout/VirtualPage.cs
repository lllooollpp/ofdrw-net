using OfdrwNet.Core;
using OfdrwNet.Layout.Element;
using OfdrwNet.Reader.Model;
using System.Xml.Linq;

namespace OfdrwNet.Layout;

/// <summary>
/// 虚拟页面
/// 对应 Java 版本的 org.ofdrw.layout.VirtualPage
/// 虚拟页面介于盒式模型和板式模型两种中间
/// 虚拟页面内包含多个Div对象，这些对象都为绝对定位
/// 由于是绝对定位，因此不存在分页的情况
/// </summary>
public class VirtualPage
{
    /// <summary>
    /// 虚拟页面的样式
    /// </summary>
    public PageLayout Style { get; set; }

    /// <summary>
    /// 虚拟页面的内容
    /// </summary>
    public List<Div> Content { get; private set; } = new List<Div>();

    /// <summary>
    /// 插入的虚拟页面页码（插入位置）
    /// 仅在不为空时候表示需要将插入到指定页码位置
    /// </summary>
    public int? PageNum { get; set; }

    /// <summary>
    /// 页面模板列表
    /// </summary>
    public List<TemplatePageEntity> Templates { get; private set; } = new List<TemplatePageEntity>();

    /// <summary>
    /// 受保护的构造函数
    /// </summary>
    protected VirtualPage() 
    {
        Style = PageLayout.A4(); // 默认A4页面
    }

    /// <summary>
    /// 使用页面样式构造虚拟页面
    /// </summary>
    /// <param name="style">页面样式</param>
    public VirtualPage(PageLayout style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>
    /// 使用宽高构造虚拟页面
    /// </summary>
    /// <param name="width">页面宽度</param>
    /// <param name="height">页面高度</param>
    public VirtualPage(double width, double height)
    {
        Style = new PageLayout(width, height);
    }

    /// <summary>
    /// 不对元素进行分析直接加入到虚拟页面容器内
    /// 请在调用该接口时，对待加入的元素进行分析，否则很有可能抛出异常
    /// 如果没有特殊需求请使用 Add，不要使用该API
    /// </summary>
    /// <param name="div">采用绝对定位的元素</param>
    /// <returns>this</returns>
    public VirtualPage AddUnsafe(Div div)
    {
        if (div != null)
        {
            Content.Add(div);
        }
        return this;
    }

    /// <summary>
    /// 向虚拟页面中加入对象
    /// </summary>
    /// <param name="div">采用绝对定位的元素</param>
    /// <returns>this</returns>
    public VirtualPage Add(Div div)
    {
        if (div == null)
        {
            return this;
        }

        if (div.Float != AFloat.None && div.Float != AFloat.Left)
        {
            Console.WriteLine("警告：虚拟页面下不支持除left外的浮动属性，仅支持绝对定位");
        }

        if (div.Position != Position.Absolute)
        {
            throw new ArgumentException("加入虚拟页面的对象应该采用绝对定位（Position: Absolute）");
        }

        if (div.X == null || div.Y == null)
        {
            throw new ArgumentException("处于绝对定位的模式下的元素应该设置 X 和 Y 坐标");
        }

        if (div.Width == null)
        {
            throw new ArgumentException("绝对定位元素至少需要指定元素宽度（Width）");
        }

        // 追加内容时进行预处理
        div.DoPrepare(div.Width.Value + div.WidthPlus());
        Content.Add(div);
        return this;
    }

    /// <summary>
    /// 获取指定图层类型的Div
    /// </summary>
    /// <param name="layer">图层类型</param>
    /// <returns>该图层相关的所有Div</returns>
    public List<Div> GetContent(LayerType layer)
    {
        return Content.Where(div => div.Layer == layer).ToList();
    }

    /// <summary>
    /// 返回图层相关的内容
    /// </summary>
    /// <returns>图层列表依次是：前景层、正文层、背景层</returns>
    public List<List<Div>> GetLayerContent()
    {
        var result = new List<List<Div>>
        {
            new List<Div>(), // 前景层
            new List<Div>(), // 正文层  
            new List<Div>()  // 背景层
        };

        if (!Content.Any())
        {
            return result;
        }

        var foreground = result[0];
        var body = result[1];
        var background = result[2];

        foreach (var div in Content)
        {
            switch (div.Layer)
            {
                case LayerType.Foreground:
                    foreground.Add(div);
                    break;
                case LayerType.Body:
                    body.Add(div);
                    break;
                case LayerType.Background:
                    background.Add(div);
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// 替换虚拟页面内的内容容器
    /// </summary>
    /// <param name="content">新的内容列表</param>
    /// <returns>this</returns>
    internal VirtualPage SetContent(List<Div> content)
    {
        Content = content ?? new List<Div>();
        return this;
    }

    /// <summary>
    /// 设置虚拟页面页码
    /// </summary>
    /// <param name="pageNum">页码（从1起）</param>
    /// <returns>this</returns>
    public VirtualPage SetPageNum(int pageNum)
    {
        if (pageNum <= 0)
        {
            throw new ArgumentException("虚拟页面页码(pageNum)错误，必须大于0");
        }
        PageNum = pageNum;
        return this;
    }

    /// <summary>
    /// 添加模板页面
    /// </summary>
    /// <param name="template">模板页面实体</param>
    /// <returns>this</returns>
    public VirtualPage AddTemplate(TemplatePageEntity template)
    {
        if (template != null)
        {
            Templates.Add(template);
        }
        return this;
    }

    /// <summary>
    /// 清空内容
    /// </summary>
    /// <returns>this</returns>
    public VirtualPage Clear()
    {
        Content.Clear();
        return this;
    }

    /// <summary>
    /// 检查页面是否为空
    /// </summary>
    /// <returns>是否为空</returns>
    public bool IsEmpty()
    {
        return !Content.Any();
    }

    /// <summary>
    /// 获取页面的边界框
    /// </summary>
    /// <returns>页面边界框</returns>
    public Rectangle GetBounds()
    {
        return new Rectangle(0, 0, Style.Width, Style.Height);
    }

    /// <summary>
    /// 获取内容区域的边界框
    /// </summary>
    /// <returns>内容区域边界框</returns>
    public Rectangle GetContentBounds()
    {
        return Style.GetWorkerArea();
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"VirtualPage(PageNum={PageNum}, Style={Style}, Content={Content.Count}, Templates={Templates.Count})";
    }
}
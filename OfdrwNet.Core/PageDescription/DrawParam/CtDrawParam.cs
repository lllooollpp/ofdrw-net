using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription.Color;

namespace OfdrwNet.Core.PageDescription.DrawParam;

/// <summary>
/// 绘制参数
/// 
/// 绘制参数是一组用于控制绘制渲染效果的修饰参数的集合。
/// 绘制参数可以被不同的图元对象所共享。
/// 
/// 绘制参数可以继承已有的绘制参数，被继承的绘制参数称为
/// 该参数的"基础绘制参数"。
/// 
/// 图元对象通过绘制参数的标识符引用绘制参数。图元对象在引用
/// 绘制参数的同时，还可以定义自己的绘制属性，图元自有的绘制属性
/// 将覆盖引用的绘制参数中的同名属性。
/// 
/// 绘制参数可通过引用基础绘制参数的方式形成嵌套，对单个绘制参数而言，
/// 它继承了其基础绘制参数中的所有属性，并且可以重定义其基础绘制参数中的属性。
/// 
/// 绘制参数的作用顺序采用就近原则，即当多个绘制参数作用于同一个对象并且这些绘制参数
/// 中具有相同的要素时，采用与被作用对象关系最为密切的绘制参数的要素对其进行渲染。
/// 例如，当图元已经定义绘制参数时，则按定义属性进行渲染；当图元未定义绘制参数时，
/// 应首先按照图元定义的绘制参数进行渲染；图元未定义绘制参数时应采用所在图层的默认绘制参数
/// 渲染；当图元和所在图层都没有定义绘制参数时，按照各绘制属性的默认值进行渲染。
/// 
/// 8.2 绘制参数结构 图 22
/// 
/// 对应 Java 版本的 org.ofdrw.core.pageDescription.drawParam.CT_DrawParam
/// </summary>
public class CtDrawParam : OfdElement
{
    /// <summary>
    /// 从现有元素构造绘制参数
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtDrawParam(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的绘制参数元素
    /// </summary>
    public CtDrawParam() : base("DrawParam")
    {
    }

    /// <summary>
    /// 获取对象ID
    /// </summary>
    /// <returns>对象ID</returns>
    public StId? GetId()
    {
        var value = GetAttributeValue("ID");
        return string.IsNullOrEmpty(value) ? null : StId.Parse(value);
    }

    /// <summary>
    /// 设置对象ID
    /// </summary>
    /// <param name="id">对象ID</param>
    /// <returns>this</returns>
    public CtDrawParam SetId(StId id)
    {
        AddAttribute("ID", id.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置基础绘制参数，引用资源文件中的绘制参数的标识符
    /// </summary>
    /// <param name="relative">引用资源文件中的绘制参数的标识符</param>
    /// <returns>this</returns>
    public CtDrawParam SetRelative(StRefId relative)
    {
        AddAttribute("Relative", relative.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取基础绘制参数，引用资源文件中的绘制参数的标识符
    /// </summary>
    /// <returns>引用资源文件中的绘制参数的标识符</returns>
    public StRefId? GetRelative()
    {
        var value = GetAttributeValue("Relative");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置线宽
    /// 非负浮点数，指定了绘制路径绘制时线的宽度。由于
    /// 某些设备不能输出一个像素宽度的线，因此强制规定
    /// 当线宽大于 0 时，无论多小都至少要绘制两个像素的宽度；
    /// 当线宽为 0 时，绘制一个像素的宽度。由于线宽为 0 定义与
    /// 设备相关，所以不推荐使用线宽为 0。
    /// 默认值为 0.353 mm
    /// </summary>
    /// <param name="lineWidth">线宽</param>
    /// <returns>this</returns>
    /// <exception cref="ArgumentException">线宽必须是非负浮点数</exception>
    public CtDrawParam SetLineWidth(double? lineWidth)
    {
        if (lineWidth == null)
        {
            RemoveAttribute("LineWidth");
            return this;
        }
        
        if (lineWidth < 0)
        {
            throw new ArgumentException("线宽必须是非负浮点数");
        }
        
        AddAttribute("LineWidth", FormatDouble(lineWidth.Value));
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取线宽
    /// 非负浮点数，指定了绘制路径绘制时线的宽度。由于
    /// 某些设备不能输出一个像素宽度的线，因此强制规定
    /// 当线宽大于 0 时，无论多小都至少要绘制两个像素的宽度；
    /// 当线宽为 0 时，绘制一个像素的宽度。由于线宽为 0 定义与
    /// 设备相关，所以不推荐使用线宽为 0。
    /// 默认值为 0.353 mm
    /// </summary>
    /// <returns>线宽</returns>
    public double GetLineWidth()
    {
        var str = GetAttributeValue("LineWidth");
        if (string.IsNullOrWhiteSpace(str))
        {
            return 0.353d;
        }
        return double.TryParse(str, out double result) ? result : 0.353d;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置线条连接样式
    /// 可选样式参照 LineJoinType，线条连接样式的取值和显示效果之间的关系见表
    /// </summary>
    /// <param name="join">线条连接样式</param>
    /// <returns>this</returns>
    public CtDrawParam SetJoin(LineJoinType? join)
    {
        if (join == null)
        {
            RemoveAttribute("Join");
            return this;
        }
        AddAttribute("Join", join.Value.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取线条连接样式
    /// 可选样式参照 LineJoinType，线条连接样式的取值和显示效果之间的关系见表
    /// </summary>
    /// <returns>线条连接样式</returns>
    public LineJoinType GetJoin()
    {
        return LineJoinTypeExtensions.Parse(GetAttributeValue("Join"));
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置线端点样式
    /// 可选样式参照 LineCapType，线条端点样式取值与效果之间关系见表 24
    /// </summary>
    /// <param name="cap">线端点样式</param>
    /// <returns>this</returns>
    public CtDrawParam SetCap(LineCapType? cap)
    {
        if (cap == null)
        {
            RemoveAttribute("Cap");
            return this;
        }
        AddAttribute("Cap", cap.Value.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取线端点样式
    /// 可选样式参照 LineCapType，线条端点样式取值与效果之间关系见表 24
    /// 默认值为 Butt
    /// </summary>
    /// <returns>线端点样式</returns>
    public LineCapType GetCap()
    {
        return LineCapTypeExtensions.Parse(GetAttributeValue("Cap"));
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置线条虚线开始位置
    /// 默认值为 0
    /// 当 DashPattern 不出现时，该参数无效
    /// </summary>
    /// <param name="dashOffset">线条虚线开始位置</param>
    /// <returns>this</returns>
    public CtDrawParam SetDashOffset(double? dashOffset)
    {
        if (dashOffset == null)
        {
            RemoveAttribute("DashOffset");
            return this;
        }
        AddAttribute("DashOffset", FormatDouble(dashOffset.Value));
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取线条虚线开始位置
    /// 默认值为 0
    /// 当 DashPattern 不出现时，该参数无效
    /// </summary>
    /// <returns>线条虚线开始位置</returns>
    public double GetDashOffset()
    {
        var str = GetAttributeValue("DashOffset");
        if (string.IsNullOrWhiteSpace(str))
        {
            return 0d;
        }
        return double.TryParse(str, out double result) ? result : 0d;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置线条虚线的重复样式
    /// 数组中共含两个值，第一个值代表虚线的线段的长度，
    /// 第二个值代表虚线间隔的长度。
    /// 默认值为空。
    /// 线条样式的控制效果见表 23
    /// </summary>
    /// <param name="dashPattern">线条虚线的重复样式的数组中共含两个值，第一个值代表虚线的线段的长度，第二个值代表虚线间隔的长度。</param>
    /// <returns>this</returns>
    public CtDrawParam SetDashPattern(StArray? dashPattern)
    {
        if (dashPattern == null)
        {
            RemoveAttribute("DashPattern");
            return this;
        }
        AddAttribute("DashPattern", dashPattern.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取线条虚线的重复样式
    /// 数组中共含两个值，第一个值代表虚线的线段的长度，
    /// 第二个值代表虚线间隔的长度。
    /// 默认值为空。
    /// 线条样式的控制效果见表 23
    /// </summary>
    /// <returns>线条虚线的重复样式的数组中共含两个值，第一个值代表虚线的线段的长度，第二个值代表虚线间隔的长度。</returns>
    public StArray? GetDashPattern()
    {
        var str = GetAttributeValue("DashPattern");
        return string.IsNullOrWhiteSpace(str) ? null : StArray.Parse(str);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置 Join的截断值
    /// Join为 Miter 时小角度结合点长度的截断值，默认值为 3.528。
    /// 当 Join 不等于 Miter 时该参数无效。
    /// </summary>
    /// <param name="miterLimit">Join的截断值长度</param>
    /// <returns>this</returns>
    public CtDrawParam SetMiterLimit(double? miterLimit)
    {
        if (miterLimit == null)
        {
            RemoveAttribute("MiterLimit");
            return this;
        }
        AddAttribute("MiterLimit", FormatDouble(miterLimit.Value));
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取 Join的截断值
    /// Join为 Miter 时小角度结合点长度的截断值，默认值为 3.528。
    /// 当 Join 不等于 Miter 时该参数无效。
    /// </summary>
    /// <returns>Join的截断值长度</returns>
    public double GetMiterLimit()
    {
        var str = GetAttributeValue("MiterLimit");
        if (string.IsNullOrWhiteSpace(str))
        {
            return 3.528d;
        }
        return double.TryParse(str, out double result) ? result : 3.528d;
    }

    /// <summary>
    /// 【可选】
    /// 设置填充颜色
    /// 用以填充路径形成的区域以及文字轮廓内的区域，
    /// 默认值为透明色。关于颜色的描述见 8.3
    /// </summary>
    /// <param name="fillColor">填充颜色</param>
    /// <returns>this</returns>
    public CtDrawParam SetFillColor(CtColor? fillColor)
    {
        if (fillColor == null)
        {
            RemoveOfdElementsByNames("FillColor");
            return this;
        }
        
        // 创建FillColor元素并设置内容
        AddOfdEntity("FillColor", ""); // 先添加空元素
        var element = GetOfdElement("FillColor");
        if (element != null)
        {
            // 复制颜色属性
            foreach (var attr in fillColor.ToXElement().Attributes())
            {
                element.SetAttributeValue(attr.Name, attr.Value);
            }
        }
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取填充颜色
    /// 用以填充路径形成的区域以及文字轮廓内的区域，
    /// 默认值为透明色。关于颜色的描述见 8.3
    /// </summary>
    /// <returns>填充颜色</returns>
    public CtColor? GetFillColor()
    {
        var element = GetOfdElement("FillColor");
        return element == null ? null : new CtColor(element);
    }

    /// <summary>
    /// 【可选】
    /// 设置勾边颜色
    /// 用以填充路径形成的区域以及文字轮廓内的区域，
    /// 默认值为黑色。关于颜色的描述见 8.3
    /// </summary>
    /// <param name="strokeColor">勾边颜色</param>
    /// <returns>this</returns>
    public CtDrawParam SetStrokeColor(CtColor? strokeColor)
    {
        if (strokeColor == null)
        {
            RemoveOfdElementsByNames("StrokeColor");
            return this;
        }
        
        // 创建StrokeColor元素并设置内容
        AddOfdEntity("StrokeColor", ""); // 先添加空元素
        var element = GetOfdElement("StrokeColor");
        if (element != null)
        {
            // 复制颜色属性
            foreach (var attr in strokeColor.ToXElement().Attributes())
            {
                element.SetAttributeValue(attr.Name, attr.Value);
            }
        }
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取勾边颜色
    /// 用以填充路径形成的区域以及文字轮廓内的区域，
    /// 默认值为黑色。关于颜色的描述见 8.3
    /// </summary>
    /// <returns>勾边颜色</returns>
    public CtColor? GetStrokeColor()
    {
        var element = GetOfdElement("StrokeColor");
        return element == null ? null : new CtColor(element);
    }

    /// <summary>
    /// 克隆绘制参数
    /// </summary>
    /// <returns>克隆的绘制参数</returns>
    public new CtDrawParam Clone()
    {
        var copy = new CtDrawParam(new XElement(Element));
        return copy;
    }

    /// <summary>
    /// 格式化double值
    /// </summary>
    /// <param name="value">double值</param>
    /// <returns>格式化字符串</returns>
    private static string FormatDouble(double value)
    {
        return value.ToString("0.######");
    }
}

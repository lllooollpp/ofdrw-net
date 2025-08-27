using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription.Clips;
using OfdrwNet.Core.PageDescription.DrawParam;

namespace OfdrwNet.Core.PageDescription;

/// <summary>
/// 图元对象基类
/// 
/// 图元对象是版式文档中页面上呈现内容的最基本单元，
/// 所有页面显示内容，包括文字、图形、图像等，都属于
/// 图元对象，或是图元对象的组合。
/// 
/// 对应 Java 版本的 org.ofdrw.core.pageDescription.CT_GraphicUnit
/// 8.5 图元对象 图 45 表 34
/// </summary>
/// <typeparam name="T">继承类型</typeparam>
public abstract class CtGraphicUnit<T> : OfdElement where T : CtGraphicUnit<T>
{
    /// <summary>
    /// 从现有元素构造图元对象
    /// </summary>
    /// <param name="element">XML元素</param>
    protected CtGraphicUnit(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造带名称的图元对象
    /// </summary>
    /// <param name="name">元素名称</param>
    protected CtGraphicUnit(string name) : base(name)
    {
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置外接矩形
    /// 采用当前空间坐标系（页面坐标或其他容器坐标），当图
    /// 元绘制超出此矩形区域时进行裁剪。
    /// </summary>
    /// <param name="boundary">外接矩形</param>
    /// <returns>this</returns>
    public T SetBoundary(StBox boundary)
    {
        if (boundary == null)
        {
            throw new ArgumentNullException(nameof(boundary), "外接矩形不能为空");
        }
        SetAttribute("Boundary", boundary.ToString());
        return (T)this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置外接矩形
    /// </summary>
    /// <param name="topLeftX">左上角X坐标</param>
    /// <param name="topLeftY">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>this</returns>
    public T SetBoundary(double topLeftX, double topLeftY, double width, double height)
    {
        var boundary = new StBox(topLeftX, topLeftY, width, height);
        return SetBoundary(boundary);
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取外接矩形
    /// </summary>
    /// <returns>外接矩形</returns>
    public StBox? GetBoundary()
    {
        var value = GetAttributeValue("Boundary");
        return string.IsNullOrEmpty(value) ? null : StBox.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置图元对象的名字
    /// </summary>
    /// <param name="name">图元对象的名字</param>
    /// <returns>this</returns>
    public T SetGraphicName(string name)
    {
        SetAttribute("Name", name);
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取图元对象的名字
    /// </summary>
    /// <returns>图元对象的名字</returns>
    public string? GetGraphicName()
    {
        return GetAttributeValue("Name");
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置是否可见
    /// </summary>
    /// <param name="visible">是否可见</param>
    /// <returns>this</returns>
    public T SetVisible(bool visible)
    {
        SetAttribute("Visible", visible.ToString().ToLower());
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取是否可见
    /// </summary>
    /// <returns>是否可见，默认为true</returns>
    public bool GetVisible()
    {
        var value = GetAttributeValue("Visible");
        return string.IsNullOrEmpty(value) || bool.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置CTM变换矩阵
    /// </summary>
    /// <param name="ctm">变换矩阵</param>
    /// <returns>this</returns>
    public T SetCtm(StArray ctm)
    {
        SetAttribute("CTM", ctm.ToString());
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取CTM变换矩阵
    /// </summary>
    /// <returns>变换矩阵</returns>
    public StArray? GetCtm()
    {
        var value = GetAttributeValue("CTM");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置引用绘制参数的标识
    /// </summary>
    /// <param name="drawParamRef">绘制参数引用</param>
    /// <returns>this</returns>
    public T SetDrawParam(StRefId drawParamRef)
    {
        SetAttribute("DrawParam", drawParamRef.ToString());
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取引用绘制参数的标识
    /// </summary>
    /// <returns>绘制参数引用</returns>
    public StRefId? GetDrawParam()
    {
        var value = GetAttributeValue("DrawParam");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置线宽
    /// </summary>
    /// <param name="lineWidth">线宽</param>
    /// <returns>this</returns>
    public T SetLineWidth(double lineWidth)
    {
        SetAttribute("LineWidth", lineWidth.ToString("F3"));
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取线宽
    /// </summary>
    /// <returns>线宽</returns>
    public double? GetLineWidth()
    {
        var value = GetAttributeValue("LineWidth");
        return double.TryParse(value, out var width) ? width : null;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置虚线模式
    /// </summary>
    /// <param name="dashPattern">虚线模式</param>
    /// <returns>this</returns>
    public T SetDashPattern(StArray dashPattern)
    {
        SetAttribute("DashPattern", dashPattern.ToString());
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取虚线模式
    /// </summary>
    /// <returns>虚线模式</returns>
    public StArray? GetDashPattern()
    {
        var value = GetAttributeValue("DashPattern");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置虚线偏移
    /// </summary>
    /// <param name="dashOffset">虚线偏移</param>
    /// <returns>this</returns>
    public T SetDashOffset(double dashOffset)
    {
        SetAttribute("DashOffset", dashOffset.ToString("F3"));
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取虚线偏移
    /// </summary>
    /// <returns>虚线偏移</returns>
    public double? GetDashOffset()
    {
        var value = GetAttributeValue("DashOffset");
        return double.TryParse(value, out var offset) ? offset : null;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置线段连接方式
    /// </summary>
    /// <param name="join">连接方式</param>
    /// <returns>this</returns>
    public T SetJoin(LineJoinType join)
    {
        SetAttribute("Join", join.ToString());
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取线段连接方式
    /// </summary>
    /// <returns>连接方式</returns>
    public LineJoinType? GetJoin()
    {
        var value = GetAttributeValue("Join");
        return Enum.TryParse<LineJoinType>(value, out var join) ? join : null;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置线端点样式
    /// </summary>
    /// <param name="cap">端点样式</param>
    /// <returns>this</returns>
    public T SetCap(LineCapType cap)
    {
        SetAttribute("Cap", cap.ToString());
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取线端点样式
    /// </summary>
    /// <returns>端点样式</returns>
    public LineCapType? GetCap()
    {
        var value = GetAttributeValue("Cap");
        return Enum.TryParse<LineCapType>(value, out var cap) ? cap : null;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置斜接限制
    /// </summary>
    /// <param name="miterLimit">斜接限制</param>
    /// <returns>this</returns>
    public T SetMiterLimit(double miterLimit)
    {
        SetAttribute("MiterLimit", miterLimit.ToString("F3"));
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取斜接限制
    /// </summary>
    /// <returns>斜接限制，默认为3.528</returns>
    public double GetMiterLimit()
    {
        var value = GetAttributeValue("MiterLimit");
        return double.TryParse(value, out var limit) ? limit : 3.528;
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置透明度
    /// </summary>
    /// <param name="alpha">透明度（0-255）</param>
    /// <returns>this</returns>
    public T SetAlpha(int alpha)
    {
        if (alpha < 0 || alpha > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(alpha), "透明度值应在0-255范围内");
        }
        SetAttribute("Alpha", alpha.ToString());
        return (T)this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取透明度
    /// </summary>
    /// <returns>透明度，默认为255</returns>
    public int GetAlpha()
    {
        var value = GetAttributeValue("Alpha");
        return int.TryParse(value, out var alpha) ? alpha : 255;
    }

    /// <summary>
    /// 【可选】
    /// 设置图元对象的裁剪区域序列
    /// 当存在多个 Clip 对象时，最终裁剪区域为所有 Clip 区域的交集。
    /// </summary>
    /// <param name="clips">裁剪区域序列</param>
    /// <returns>this</returns>
    public T SetClips(Clips.Clips clips)
    {
        if (clips == null) return (T)this;
        
        RemoveOfdElementsByNames("Clips");
        Add(clips);
        return (T)this;
    }

    /// <summary>
    /// 【可选】
    /// 获取图元对象的裁剪区域序列
    /// </summary>
    /// <returns>裁剪区域序列</returns>
    public Clips.Clips? GetClips()
    {
        var clipElement = Element.Element(Const.OfdNamespace + "Clips");
        return clipElement != null ? new Clips.Clips(clipElement) : null;
    }

    /// <summary>
    /// 验证图元对象是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public virtual bool IsValid()
    {
        return GetBoundary() != null;
    }

    /// <summary>
    /// 获取图元对象描述信息
    /// </summary>
    /// <returns>描述字符串</returns>
    public override string ToString()
    {
        var boundary = GetBoundary();
        var name = GetGraphicName();
        return $"{GetType().Name}[Name={name}, Boundary={boundary}]";
    }
}

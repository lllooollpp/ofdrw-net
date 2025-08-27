using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Graph;
using OfdrwNet.Core.PageDescription;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Core.PageDescription.Clips;

namespace OfdrwNet.Core.Text;

/// <summary>
/// 文字对象
/// 
/// 11.2 文字对象 图 59 表 45
/// 
/// 对应 Java 版本的 org.ofdrw.core.text.text.CT_Text
/// </summary>
public class CtText : CtGraphicUnit<CtText>, PageDescription.Clips.IClipAble
{
    /// <summary>
    /// 从现有XML元素构造文字对象
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtText(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的文字对象
    /// </summary>
    public CtText() : base("Text")
    {
    }

    /// <summary>
    /// 使用指定名称构造文字对象
    /// </summary>
    /// <param name="name">元素名称</param>
    protected CtText(string name) : base(name)
    {
    }

    /// <summary>
    /// 获取文字对象
    /// </summary>
    /// <param name="id">文字对象ID</param>
    /// <returns>文字对象 TextObject</returns>
    /// <exception cref="ArgumentException">ID不能为空</exception>
    public static CtText TextObject(StId id)
    {
        if (id == null)
            throw new ArgumentException("ID 不能为空");
        
        var result = new CtText("TextObject");
        result.SetObjId(id);
        return result;
    }

    /// <summary>
    /// 【必选 属性】设置引用资源文件中定义的字形标识
    /// </summary>
    /// <param name="font">引用字形资源文件路径</param>
    /// <returns>this</returns>
    /// <exception cref="ArgumentException">字形资源文件（Font）不能为空</exception>
    public CtText SetFont(StRefId font)
    {
        if (font == null)
            throw new ArgumentException("字形资源文件（Font）不能为空");
        
        AddAttribute("Font", font.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】设置引用资源文件中定义的字形标识
    /// </summary>
    /// <param name="refId">ID</param>
    /// <returns>this</returns>
    public CtText SetFont(long refId)
    {
        return SetFont(new StRefId(refId));
    }

    /// <summary>
    /// 【必选 属性】获取引用资源文件路径
    /// </summary>
    /// <returns>引用字形资源文件路径</returns>
    public StRefId? GetFont()
    {
        var value = GetAttributeValue("Font");
        return string.IsNullOrEmpty(value) ? null : StRefId.Parse(value);
    }

    /// <summary>
    /// 【必选 属性】设置字号，单位为毫米
    /// </summary>
    /// <param name="size">字号，单位为毫米</param>
    /// <returns>this</returns>
    /// <exception cref="ArgumentException">字号（Size）不能为空</exception>
    public CtText SetSize(double size)
    {
        if (size <= 0)
            throw new ArgumentException("字号（Size）必须大于0");
        
        AddAttribute("Size", size.ToString("F2"));
        return this;
    }

    /// <summary>
    /// 【必选 属性】获取字号，单位为毫米
    /// </summary>
    /// <returns>字号，单位为毫米</returns>
    /// <exception cref="ArgumentException">字号（Size）不能为空</exception>
    public double GetSize()
    {
        var value = GetAttributeValue("Size");
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("字号（Size）不能为空");
        
        return double.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置是否勾边
    /// 默认值为 false
    /// </summary>
    /// <param name="stroke">true - 勾边；false - 不勾边</param>
    /// <returns>this</returns>
    public CtText SetStroke(bool? stroke)
    {
        if (stroke.HasValue)
            AddAttribute("Stroke", stroke.Value.ToString().ToLower());
        else
            RemoveAttribute("Stroke");
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取是否勾边
    /// 默认值为 false
    /// </summary>
    /// <returns>true - 勾边；false - 不勾边</returns>
    public bool GetStroke()
    {
        var value = GetAttributeValue("Stroke");
        return !string.IsNullOrWhiteSpace(value) && bool.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置是否填充
    /// 默认值为 true
    /// </summary>
    /// <param name="fill">true - 填充；false - 不填充</param>
    /// <returns>this</returns>
    public CtText SetFill(bool? fill)
    {
        if (fill.HasValue)
            AddAttribute("Fill", fill.Value.ToString().ToLower());
        else
            RemoveAttribute("Fill");
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取是否填充
    /// 默认值为 true
    /// </summary>
    /// <returns>true - 填充；false - 不填充</returns>
    public bool GetFill()
    {
        var value = GetAttributeValue("Fill");
        return string.IsNullOrWhiteSpace(value) || bool.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置字形在水平方向的缩放比
    /// 默认值为 1.0
    /// 例如：当 HScale 值为 0.5 时表示实际显示的字宽为原来字宽的一半。
    /// </summary>
    /// <param name="hScale">字形在水平方向的缩放比</param>
    /// <returns>this</returns>
    public CtText SetHScale(double? hScale)
    {
        if (hScale.HasValue)
            AddAttribute("HScale", hScale.Value.ToString("F2"));
        else
            RemoveAttribute("HScale");
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取字形在水平方向的缩放比
    /// 默认值为 1.0
    /// </summary>
    /// <returns>字形在水平方向的缩放比</returns>
    public double GetHScale()
    {
        var value = GetAttributeValue("HScale");
        return string.IsNullOrWhiteSpace(value) ? 1.0 : double.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置阅读方向
    /// 指定了文字排列的方向，描述见 11.3 文字定位
    /// 默认值为 0
    /// </summary>
    /// <param name="readDirection">阅读方向</param>
    /// <returns>this</returns>
    public CtText SetReadDirection(Direction? readDirection)
    {
        if (readDirection.HasValue)
            AddAttribute("ReadDirection", ((int)readDirection.Value).ToString());
        else
            RemoveAttribute("ReadDirection");
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取阅读方向
    /// 指定了文字排列的方向，描述见 11.3 文字定位
    /// 默认值为 0
    /// </summary>
    /// <returns>阅读方向</returns>
    public Direction GetReadDirection()
    {
        var value = GetAttributeValue("ReadDirection");
        return string.IsNullOrWhiteSpace(value) ? Direction.Left : (Direction)int.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置字符方向
    /// 指定了文字放置的方向，描述见 11.3 文字定位
    /// 默认值为 0
    /// </summary>
    /// <param name="charDirection">字符方向</param>
    /// <returns>this</returns>
    public CtText SetCharDirection(Direction? charDirection)
    {
        if (charDirection.HasValue)
            AddAttribute("CharDirection", ((int)charDirection.Value).ToString());
        else
            RemoveAttribute("CharDirection");
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取字符方向
    /// 指定了文字放置的方向，描述见 11.3 文字定位
    /// 默认值为 0
    /// </summary>
    /// <returns>字符方向</returns>
    public Direction GetCharDirection()
    {
        var value = GetAttributeValue("CharDirection");
        return string.IsNullOrWhiteSpace(value) ? Direction.Left : (Direction)int.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置文字对象的粗细值
    /// 默认值为 400
    /// </summary>
    /// <param name="weight">文字对象的粗细值</param>
    /// <returns>this</returns>
    public CtText SetWeight(Weight? weight)
    {
        if (weight.HasValue)
            AddAttribute("Weight", ((int)weight.Value).ToString());
        else
            RemoveAttribute("Weight");
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取文字对象的粗细值
    /// 默认值为 400
    /// </summary>
    /// <returns>文字对象的粗细值</returns>
    public Weight GetWeight()
    {
        var value = GetAttributeValue("Weight");
        return string.IsNullOrWhiteSpace(value) ? Weight.Normal : (Weight)int.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置是否是斜体样式
    /// 默认值为 false
    /// </summary>
    /// <param name="italic">true - 斜体样式；false - 正常</param>
    /// <returns>this</returns>
    public CtText SetItalic(bool? italic)
    {
        if (italic.HasValue)
            AddAttribute("Italic", italic.Value.ToString().ToLower());
        else
            RemoveAttribute("Italic");
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取是否是斜体样式
    /// 默认值为 false
    /// </summary>
    /// <returns>true - 斜体样式；false - 正常</returns>
    public bool GetItalic()
    {
        var value = GetAttributeValue("Italic");
        return !string.IsNullOrWhiteSpace(value) && bool.Parse(value);
    }

    /// <summary>
    /// 【可选】设置填充颜色
    /// 默认为黑色
    /// </summary>
    /// <param name="fillColor">填充颜色</param>
    /// <returns>this</returns>
    public CtText SetFillColor(CtColor fillColor)
    {
        if (fillColor != null)
        {
            var element = new XElement(fillColor.Element);
            element.Name = Const.OfdNamespace + "FillColor";
            Add(new OfdElement(element));
        }
        return this;
    }

    /// <summary>
    /// 【可选】获取填充颜色
    /// 默认为黑色
    /// </summary>
    /// <returns>填充颜色，null表示黑色</returns>
    public FillColor? GetFillColor()
    {
        var element = GetOfdElement("FillColor");
        return element == null ? null : new FillColor(element);
    }

    /// <summary>
    /// 【可选】设置勾边颜色
    /// 默认为透明色
    /// </summary>
    /// <param name="strokeColor">勾边颜色</param>
    /// <returns>this</returns>
    public CtText SetStrokeColor(CtColor strokeColor)
    {
        if (strokeColor != null)
        {
            var element = new XElement(strokeColor.Element);
            element.Name = Const.OfdNamespace + "StrokeColor";
            Add(new OfdElement(element));
        }
        return this;
    }

    /// <summary>
    /// 【可选】获取勾边颜色
    /// 默认为透明色
    /// </summary>
    /// <returns>勾边颜色，null为透明色</returns>
    public StrokeColor? GetStrokeColor()
    {
        var element = GetOfdElement("StrokeColor");
        return element == null ? null : new StrokeColor(element);
    }

    /// <summary>
    /// 【可选】增加指定字符编码到字符索引之间的变换关系
    /// 描述见 11.4 字符变换
    /// </summary>
    /// <param name="cgTransform">字符编码到字符索引之间的变换关系</param>
    /// <returns>this</returns>
    public CtText AddCgTransform(CtCgTransform cgTransform)
    {
        if (cgTransform != null)
        {
            Add(cgTransform);
        }
        return this;
    }

    /// <summary>
    /// 【可选】获取指定字符编码到字符索引之间的变换关系序列
    /// 描述见 11.4 字符变换
    /// </summary>
    /// <returns>字符编码到字符索引之间的变换关系序列</returns>
    public List<CtCgTransform> GetCgTransforms()
    {
        return GetOfdElements("CGTransform").Select(e => new CtCgTransform(e)).ToList();
    }

    /// <summary>
    /// 【必选】增加文字内容
    /// 也就是一段字符编码串
    /// </summary>
    /// <param name="textCode">文字内容</param>
    /// <returns>this</returns>
    public CtText AddTextCode(TextCode textCode)
    {
        if (textCode != null)
        {
            Add(textCode);
        }
        return this;
    }

    /// <summary>
    /// 【必选】获取文字内容序列
    /// 也就是一段字符编码串
    /// </summary>
    /// <returns>文字内容序列</returns>
    public List<TextCode> GetTextCodes()
    {
        return GetOfdElements("TextCode").Select(e => new TextCode(e)).ToList();
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:Text";
}
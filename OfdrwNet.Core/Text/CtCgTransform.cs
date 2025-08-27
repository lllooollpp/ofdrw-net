using OfdrwNet.Core.BasicType;
using System;
using System.Xml.Linq;

namespace OfdrwNet.Core.Text;

/// <summary>
/// 变换描述
/// 
/// 当存在字形变换时，TextCode对象中使用字形变换节点（CGTransform）描述字符编码
/// 和字形索引之间的关系。
/// 
/// 11.4.1 变换描述 图 66 表 48
/// 
/// 对应 Java 版本的 org.ofdrw.core.text.CT_CGTransform
/// </summary>
public class CtCgTransform : OfdElement
{
    /// <summary>
    /// 从现有XML元素构造变换描述
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtCgTransform(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的变换描述
    /// </summary>
    public CtCgTransform() : base("CGTransform")
    {
    }

    /// <summary>
    /// 【必选 属性】设置TextCode中字符编码的起始位置
    /// 从0开始
    /// </summary>
    /// <param name="codePosition">TextCode中字符编码的起始位置</param>
    /// <returns>this</returns>
    /// <exception cref="ArgumentException">字符编码的起始位置不能为空</exception>
    public CtCgTransform SetCodePosition(int codePosition)
    {
        if (codePosition < 0)
        {
            throw new ArgumentException("字符编码的起始位置不能小于0", nameof(codePosition));
        }
        AddAttribute("CodePosition", codePosition.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】获取TextCode中字符编码的起始位置
    /// 从0开始
    /// </summary>
    /// <returns>TextCode中字符编码的起始位置</returns>
    /// <exception cref="ArgumentException">字符编码的起始位置不能为空</exception>
    public int GetCodePosition()
    {
        var value = GetAttributeValue("CodePosition");
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("字符编码的起始位置不能为空");
        }
        return int.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置变换关系中字符的数量
    /// 该数值应大于等于1，否则属于错误描述
    /// 默认为1
    /// </summary>
    /// <param name="codeCount">变换关系中字符的数量，数值应大于等于1</param>
    /// <returns>this</returns>
    /// <exception cref="ArgumentException">变换关系中字符的数值应大于等于1</exception>
    public CtCgTransform SetCodeCount(int codeCount = 1)
    {
        if (codeCount < 1)
        {
            throw new ArgumentException("变换关系中字符的数值应大于等于1", nameof(codeCount));
        }
        AddAttribute("CodeCount", codeCount.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取变换关系中字符的数量
    /// 该数值应大于等于1，否则属于错误描述
    /// 默认为1
    /// </summary>
    /// <returns>变换关系中字符的数量</returns>
    public int GetCodeCount()
    {
        var value = GetAttributeValue("CodeCount");
        if (string.IsNullOrWhiteSpace(value))
        {
            return 1;
        }
        return int.Parse(value);
    }

    /// <summary>
    /// 【可选 属性】设置变换关系中字形索引的个数
    /// 该数值应大于等于1，否则属于错误描述
    /// 默认为1
    /// </summary>
    /// <param name="glyphCount">变换关系中字形索引的个数</param>
    /// <returns>this</returns>
    /// <exception cref="ArgumentException">变换关系中字符的数值应大于等于1</exception>
    public CtCgTransform SetGlyphCount(int glyphCount = 1)
    {
        if (glyphCount < 1)
        {
            throw new ArgumentException("变换关系中字符的数值应大于等于1", nameof(glyphCount));
        }
        AddAttribute("GlyphCount", glyphCount.ToString());
        return this;
    }

    /// <summary>
    /// 【可选 属性】获取变换关系中字形索引的个数
    /// 该数值应大于等于1，否则属于错误描述
    /// 默认为1
    /// </summary>
    /// <returns>变换关系中字形索引的个数</returns>
    public int GetGlyphCount()
    {
        var value = GetAttributeValue("GlyphCount");
        if (string.IsNullOrWhiteSpace(value))
        {
            return 1;
        }
        return int.Parse(value);
    }

    /// <summary>
    /// 【可选】设置变换后的字形索引列表
    /// </summary>
    /// <param name="glyphs">变换后的字形索引列表</param>
    /// <returns>this</returns>
    public CtCgTransform SetGlyphs(StArray glyphs)
    {
        SetOfdEntity("Glyphs", glyphs.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】获取变换后的字形索引列表
    /// </summary>
    /// <returns>变换后的字形索引列表</returns>
    public StArray? GetGlyphs()
    {
        var value = GetOfdElementText("Glyphs");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:CGTransform";
}

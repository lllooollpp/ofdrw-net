using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Graph;
using OfdrwNet.Core.Graph.PathObj;
using OfdrwNet.Core.PageDescription;
using OfdrwNet.Core.PageDescription.Color;

namespace OfdrwNet.Core.BasicStructure.PageObj.Layer.Block;

/// <summary>
/// 路径对象
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.pageObj.layer.block.PathObject
/// </summary>
public class PathObject : BlockType
{
    public PathObject() : base("PathObject")
    {
    }

    public PathObject(XElement element) : base(element)
    {
    }

    public PathObject(StRefId id) : this()
    {
        this.SetObjID(id);
    }

    public PathObject(long id) : this()
    {
        this.SetObjID(new StRefId(id));
    }

    // 以下是兼容层，提供 Java 版本/Layout 期望的部分 API

    public PathObject SetAbbreviatedData(AbbreviatedData? data)
    {
        RemoveOfdElementsByNames("AbbreviatedData");
        if (data != null)
        {
            Add(data);
        }
        return this;
    }

    public string? GetAbbreviatedData()
    {
        return GetOfdElement("AbbreviatedData")?.Value;
    }

    public PathObject SetFill(bool? fill)
    {
        if (fill == null)
        {
            RemoveAttribute("Fill");
            return this;
        }
        SetAttribute("Fill", fill.Value.ToString().ToLower());
        return this;
    }

    public bool? GetFill()
    {
        var value = GetAttributeValue("Fill");
        return string.IsNullOrEmpty(value) ? null : bool.Parse(value);
    }

    public PathObject SetStroke(bool? stroke)
    {
        if (stroke == null)
        {
            RemoveAttribute("Stroke");
            return this;
        }
        SetAttribute("Stroke", stroke.Value.ToString().ToLower());
        return this;
    }

    public bool? GetStroke()
    {
        var value = GetAttributeValue("Stroke");
        return string.IsNullOrEmpty(value) ? null : bool.Parse(value);
    }

    public PathObject SetLineWidth(double lineWidth)
    {
        SetAttribute("LineWidth", lineWidth.ToString("F3"));
        return this;
    }

    public double? GetLineWidth()
    {
        var value = GetAttributeValue("LineWidth");
        return double.TryParse(value, out var w) ? w : null;
    }

    public PathObject SetCTM(StArray ctm)
    {
        if (ctm == null)
        {
            RemoveAttribute("CTM");
            return this;
        }
        SetAttribute("CTM", ctm.ToString());
        return this;
    }

    public StArray? GetCTM()
    {
        var value = GetAttributeValue("CTM");
        return string.IsNullOrEmpty(value) ? null : StArray.Parse(value);
    }

    public PathObject SetStrokeColor(CtColor? strokeColor)
    {
        if (strokeColor == null)
        {
            RemoveOfdElementsByNames("StrokeColor");
            return this;
        }
        var copy = new OfdrwNet.Core.PageDescription.DrawParam.StrokeColor(new XElement(strokeColor.ToXElement()));
        RemoveOfdElementsByNames("StrokeColor");
        Set(copy);
        return this;
    }

    public OfdrwNet.Core.PageDescription.DrawParam.StrokeColor? GetStrokeColor()
    {
        var element = GetOfdElement("StrokeColor");
        return element != null ? new OfdrwNet.Core.PageDescription.DrawParam.StrokeColor(element) : null;
    }

    public PathObject SetFillColor(CtColor? fillColor)
    {
        if (fillColor == null)
        {
            RemoveOfdElementsByNames("FillColor");
            return this;
        }
        var copy = new OfdrwNet.Core.PageDescription.DrawParam.FillColor(new XElement(fillColor.ToXElement()));
        RemoveOfdElementsByNames("FillColor");
        Set(copy);
        return this;
    }

    public OfdrwNet.Core.PageDescription.DrawParam.FillColor? GetFillColor()
    {
        var element = GetOfdElement("FillColor");
        return element != null ? new OfdrwNet.Core.PageDescription.DrawParam.FillColor(element) : null;
    }

    /// <summary>
    /// 设置对象整体透明度（0-255）
    /// </summary>
    /// <param name="alpha">透明度</param>
    /// <returns>this</returns>
    public PathObject SetAlpha(int alpha)
    {
        // alpha 范围 0-255
        if (alpha < 0 || alpha > 255)
            throw new ArgumentOutOfRangeException(nameof(alpha));
        SetAttribute("Alpha", alpha.ToString());
        return this;
    }

    /// <summary>
    /// 获取透明度（0-255），默认255
    /// </summary>
    /// <returns>透明度</returns>
    public int GetAlpha()
    {
        var v = GetAttributeValue("Alpha");
        return string.IsNullOrEmpty(v) ? 255 : int.Parse(v);
    }

    /// <summary>
    /// 设置虚线模式
    /// </summary>
    /// <param name="pattern">虚线数组（单位：mm）</param>
    /// <returns>this</returns>
    public PathObject SetLineDash(double[]? pattern)
    {
        if (pattern == null)
        {
            RemoveOfdElementsByNames("DashPattern");
            return this;
        }
        // 使用 DrawParam 下的 DashPattern（OfdElement）以便可以通过 Set() 添加到 OFD 元素链中
        var dp = new OfdrwNet.Core.PageDescription.DrawParam.DashPattern(pattern);
        RemoveOfdElementsByNames("DashPattern");
        Set(dp);
        return this;
    }

    /// <summary>
    /// 获取虚线模式数组，null 表示未设置
    /// </summary>
    /// <returns>虚线数组或 null</returns>
    public double[]? GetLineDash()
    {
        var el = GetOfdElement("DashPattern");
        return el == null ? null : new OfdrwNet.Core.PageDescription.DrawParam.DashPattern(el).GetPattern();
    }

    /// <summary>
    /// 设置线段连接样式
    /// </summary>
    /// <param name="join">连接样式对象</param>
    /// <returns>this</returns>
    public PathObject SetLineJoin(OfdrwNet.Core.PageDescription.DrawParam.Join? join)
    {
        if (join == null)
        {
            RemoveOfdElementsByNames("Join");
            return this;
        }
        var copy = new OfdrwNet.Core.PageDescription.DrawParam.Join(new XElement(join.ToXElement()));
        RemoveOfdElementsByNames("Join");
        Set(copy);
        return this;
    }

    /// <summary>
    /// 获取线段连接样式
    /// </summary>
    /// <returns>连接样式或 null</returns>
    public OfdrwNet.Core.PageDescription.DrawParam.Join? GetLineJoin()
    {
        var el = GetOfdElement("Join");
        return el != null ? new OfdrwNet.Core.PageDescription.DrawParam.Join(el) : null;
    }

    /// <summary>
    /// 设置线段端点样式
    /// </summary>
    /// <param name="cap">端点样式对象</param>
    /// <returns>this</returns>
    public PathObject SetLineCap(OfdrwNet.Core.PageDescription.DrawParam.Cap? cap)
    {
        if (cap == null)
        {
            RemoveOfdElementsByNames("Cap");
            return this;
        }
        var copy = new OfdrwNet.Core.PageDescription.DrawParam.Cap(new XElement(cap.ToXElement()));
        RemoveOfdElementsByNames("Cap");
        Set(copy);
        return this;
    }

    /// <summary>
    /// 获取线段端点样式
    /// </summary>
    /// <returns>端点样式或 null</returns>
    public OfdrwNet.Core.PageDescription.DrawParam.Cap? GetLineCap()
    {
        var el = GetOfdElement("Cap");
        return el != null ? new OfdrwNet.Core.PageDescription.DrawParam.Cap(el) : null;
    }

    /// <summary>
    /// 兼容方法：按四个数值设置边界（x,y,w,h）并返回 PathObject 以支持链式调用
    /// </summary>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="w">宽度</param>
    /// <param name="h">高度</param>
    /// <returns>this</returns>
    public PathObject SetBoundary(double x, double y, double w, double h)
    {
        // 调用基类的 SetBoundary(StArray) 以保持底层表示一致
        SetBoundary(new StArray(x, y, w, h));
        return this;
    }
}
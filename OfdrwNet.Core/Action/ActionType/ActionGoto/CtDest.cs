using System;
using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Action.ActionType.ActionGoto;

/// <summary>
/// 目标类型枚举
/// </summary>
public enum DestType
{
    /// <summary>
    /// 左上角为原点，向右下角偏移Left、Top个单位，以某个倍数显示页面
    /// </summary>
    XYZ,

    /// <summary>
    /// 适合页面
    /// </summary>
    Fit,

    /// <summary>
    /// 水平适合页面
    /// </summary>
    FitH,

    /// <summary>
    /// 垂直适合页面  
    /// </summary>
    FitV,

    /// <summary>
    /// 适合矩形区域
    /// </summary>
    FitR
}

/// <summary>
/// DestType 枚举的扩展方法
/// </summary>
public static class DestTypeExtensions
{
    /// <summary>
    /// 解析字符串为 DestType
    /// </summary>
    /// <param name="str">字符串值</param>
    /// <returns>DestType 枚举值</returns>
    public static DestType Parse(string? str)
    {
        return str switch
        {
            "XYZ" => DestType.XYZ,
            "Fit" => DestType.Fit,
            "FitH" => DestType.FitH,
            "FitV" => DestType.FitV,
            "FitR" => DestType.FitR,
            _ => DestType.XYZ // 默认值
        };
    }

    /// <summary>
    /// 将 DestType 转换为字符串
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>字符串表示</returns>
    public static string ToStringValue(this DestType type)
    {
        return type switch
        {
            DestType.XYZ => "XYZ",
            DestType.Fit => "Fit",
            DestType.FitH => "FitH",
            DestType.FitV => "FitV",
            DestType.FitR => "FitR",
            _ => "XYZ"
        };
    }
}

/// <summary>
/// 目标区域定义
/// 对应 Java 版本的 CT_Dest
/// 用于定义书签或链接的目标位置
/// </summary>
public class CtDest : OfdElement
{
    public CtDest() : base("Dest")
    {
    }

    /// <summary>
    /// 从现有 XElement 构造 CtDest
    /// </summary>
    /// <param name="element">现有的 XElement</param>
    public CtDest(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造函数，指定页面ID
    /// </summary>
    /// <param name="pageId">目标页面ID</param>
    public CtDest(StRefId pageId) : this()
    {
        SetPageId(pageId);
    }

    /// <summary>
    /// 构造函数，指定页面ID和目标类型
    /// </summary>
    /// <param name="pageId">目标页面ID</param>
    /// <param name="type">目标类型</param>
    public CtDest(StRefId pageId, DestType type) : this(pageId)
    {
        SetType(type);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置目标类型
    /// 默认值为XYZ
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>this</returns>
    public CtDest SetType(DestType type)
    {
        AddAttribute("Type", type.ToStringValue());
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取目标类型
    /// 默认值为XYZ
    /// </summary>
    /// <returns>目标类型</returns>
    public DestType GetDestType()
    {
        var typeStr = GetAttributeValue("Type");
        return DestTypeExtensions.Parse(typeStr);
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置目标页面ID
    /// </summary>
    /// <param name="pageId">页面引用ID</param>
    /// <returns>this</returns>
    public CtDest SetPageId(StRefId pageId)
    {
        if (pageId == null)
            throw new ArgumentNullException(nameof(pageId));
        AddAttribute("PageID", pageId.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取目标页面ID
    /// </summary>
    /// <returns>页面引用ID，可能为null</returns>
    public StRefId? GetPageId()
    {
        var pageIdStr = GetAttributeValue("PageID");
        return string.IsNullOrEmpty(pageIdStr) ? null : StRefId.Parse(pageIdStr);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置目标区域左上角x坐标
    /// 默认值为0
    /// </summary>
    /// <param name="left">左边距</param>
    /// <returns>this</returns>
    public CtDest SetLeft(double left)
    {
        AddAttribute("Left", left.ToString("F6"));
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取目标区域左上角x坐标
    /// 默认值为0
    /// </summary>
    /// <returns>左边距</returns>
    public double GetLeft()
    {
        var leftStr = GetAttributeValue("Left");
        return string.IsNullOrEmpty(leftStr) ? 0.0 : double.Parse(leftStr);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置目标区域左上角y坐标
    /// 默认值为0
    /// </summary>
    /// <param name="top">上边距</param>
    /// <returns>this</returns>
    public CtDest SetTop(double top)
    {
        AddAttribute("Top", top.ToString("F6"));
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取目标区域左上角y坐标
    /// 默认值为0
    /// </summary>
    /// <returns>上边距</returns>
    public double GetTop()
    {
        var topStr = GetAttributeValue("Top");
        return string.IsNullOrEmpty(topStr) ? 0.0 : double.Parse(topStr);
    }

    /// <summary>
    /// 【可选 属性】
    /// 设置缩放级别
    /// 默认值为空，表示不缩放
    /// </summary>
    /// <param name="zoom">缩放级别</param>
    /// <returns>this</returns>
    public CtDest SetZoom(double zoom)
    {
        AddAttribute("Zoom", zoom.ToString("F6"));
        return this;
    }

    /// <summary>
    /// 【可选 属性】
    /// 获取缩放级别
    /// 默认值为空，表示不缩放
    /// </summary>
    /// <returns>缩放级别，null表示不缩放</returns>
    public double? GetZoom()
    {
        var zoomStr = GetAttributeValue("Zoom");
        if (string.IsNullOrEmpty(zoomStr))
            return null;
        
        return double.TryParse(zoomStr, out var result) ? result : null;
    }

    /// <summary>
    /// 创建目标区域的副本
    /// </summary>
    /// <returns>目标区域副本</returns>
    public new CtDest Clone()
    {
        var clone = new CtDest();
        foreach (var attr in Element.Attributes())
        {
            clone.AddAttribute(attr.Name.LocalName, attr.Value);
        }
        return clone;
    }

    /// <summary>
    /// 设置适合页面的目标
    /// </summary>
    /// <param name="pageId">目标页面ID</param>
    /// <returns>this</returns>
    public CtDest SetFit(StRefId pageId)
    {
        SetPageId(pageId);
        SetType(DestType.Fit);
        return this;
    }

    /// <summary>
    /// 设置XYZ类型的目标
    /// </summary>
    /// <param name="pageId">目标页面ID</param>
    /// <param name="left">左边距，默认0</param>
    /// <param name="top">上边距，默认0</param>
    /// <param name="zoom">缩放级别，null表示不缩放</param>
    /// <returns>this</returns>
    public CtDest SetXYZ(StRefId pageId, double left = 0, double top = 0, double? zoom = null)
    {
        SetPageId(pageId);
        SetType(DestType.XYZ);
        SetLeft(left);
        SetTop(top);
        if (zoom.HasValue)
            SetZoom(zoom.Value);
        return this;
    }

    /// <summary>
    /// 设置矩形适合的目标
    /// </summary>
    /// <param name="pageId">目标页面ID</param>
    /// <param name="left">左边距</param>
    /// <param name="top">上边距</param>
    /// <returns>this</returns>
    public CtDest SetFitR(StRefId pageId, double left, double top)
    {
        SetPageId(pageId);
        SetType(DestType.FitR);
        SetLeft(left);
        SetTop(top);
        return this;
    }
}

using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Resource;

namespace OfdrwNet.Core.Annotation;

/// <summary>
/// OFD注释基类
/// 定义所有注释的通用属性和方法
/// </summary>
public abstract class AnnotationBase
{
    /// <summary>
    /// 注释ID
    /// </summary>
    public StId Id { get; set; }

    /// <summary>
    /// 注释类型
    /// </summary>
    public AnnotationType Type { get; protected set; }

    /// <summary>
    /// 注释所在页面ID
    /// </summary>
    public StId PageId { get; set; }

    /// <summary>
    /// 注释边界框
    /// </summary>
    public StBox Boundary { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public string? Creator { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime ModificationDate { get; set; }

    /// <summary>
    /// 注释内容/文本
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// 是否可打印
    /// </summary>
    public bool Printable { get; set; } = true;

    /// <summary>
    /// 透明度（0.0-1.0）
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>
    /// 注释标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">注释ID</param>
    /// <param name="type">注释类型</param>
    /// <param name="pageId">页面ID</param>
    /// <param name="boundary">边界框</param>
    protected AnnotationBase(StId id, AnnotationType type, StId pageId, StBox boundary)
    {
        Id = id;
        Type = type;
        PageId = pageId;
        Boundary = boundary;
        CreationDate = DateTime.Now;
        ModificationDate = DateTime.Now;
    }

    /// <summary>
    /// 生成注释XML
    /// </summary>
    /// <returns>注释XML内容</returns>
    public abstract string ToXml();

    /// <summary>
    /// 获取注释摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        return $"Annotation[ID={Id}, Type={Type}, Page={PageId}, Bounds={Boundary}]";
    }
}

/// <summary>
/// 高亮注释类
/// 用于在文档中高亮显示指定区域
/// </summary>
public class HighlightAnnotation : AnnotationBase
{
    /// <summary>
    /// 高亮颜色
    /// </summary>
    public OfdrwNet.Core.Resource.Color HighlightColor { get; set; }

    /// <summary>
    /// 高亮区域列表（支持多个不连续区域）
    /// </summary>
    public List<StBox> HighlightAreas { get; set; }

    /// <summary>
    /// 高亮样式
    /// </summary>
    public HighlightStyle Style { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">注释ID</param>
    /// <param name="pageId">页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="highlightColor">高亮颜色</param>
    public HighlightAnnotation(StId id, StId pageId, StBox boundary, OfdrwNet.Core.Resource.Color highlightColor)
        : base(id, AnnotationType.Highlight, pageId, boundary)
    {
        HighlightColor = highlightColor;
        HighlightAreas = new List<StBox>();
        Style = HighlightStyle.Normal;
    }

    /// <summary>
    /// 添加高亮区域
    /// </summary>
    /// <param name="area">高亮区域</param>
    /// <returns>this</returns>
    public HighlightAnnotation AddHighlightArea(StBox area)
    {
        HighlightAreas.Add(area);
        
        // 更新边界框以包含所有高亮区域
        UpdateBoundary();
        
        return this;
    }

    /// <summary>
    /// 添加多个高亮区域
    /// </summary>
    /// <param name="areas">高亮区域列表</param>
    /// <returns>this</returns>
    public HighlightAnnotation AddHighlightAreas(IEnumerable<StBox> areas)
    {
        HighlightAreas.AddRange(areas);
        UpdateBoundary();
        return this;
    }

    /// <summary>
    /// 设置高亮样式
    /// </summary>
    /// <param name="style">高亮样式</param>
    /// <returns>this</returns>
    public HighlightAnnotation SetStyle(HighlightStyle style)
    {
        Style = style;
        return this;
    }

    /// <summary>
    /// 设置透明度
    /// </summary>
    /// <param name="opacity">透明度（0.0-1.0）</param>
    /// <returns>this</returns>
    public HighlightAnnotation SetOpacity(double opacity)
    {
        Opacity = Math.Max(0.0, Math.Min(1.0, opacity));
        return this;
    }

    /// <summary>
    /// 更新边界框以包含所有高亮区域
    /// </summary>
    private void UpdateBoundary()
    {
        if (HighlightAreas.Count == 0)
            return;

        var minX = HighlightAreas.Min(area => area.X);
        var minY = HighlightAreas.Min(area => area.Y);
        var maxX = HighlightAreas.Max(area => area.X + area.Width);
        var maxY = HighlightAreas.Max(area => area.Y + area.Height);

        Boundary = new StBox(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// 生成高亮注释XML
    /// </summary>
    /// <returns>高亮注释XML内容</returns>
    public override string ToXml()
    {
        var xml = $"<ofd:Annot ID='{Id}' Type='Highlight' PageRef='{PageId}' Visible='{Visible}' Print='{Printable}'>\n";
        xml += $"  <ofd:Boundary>{Boundary.X} {Boundary.Y} {Boundary.Width} {Boundary.Height}</ofd:Boundary>\n";
        
        if (!string.IsNullOrEmpty(Creator))
        {
            xml += $"  <ofd:Creator>{Creator}</ofd:Creator>\n";
        }
        
        xml += $"  <ofd:CreationDate>{CreationDate:yyyy-MM-ddTHH:mm:ss}</ofd:CreationDate>\n";
        xml += $"  <ofd:ModDate>{ModificationDate:yyyy-MM-ddTHH:mm:ss}</ofd:ModDate>\n";
        
        if (!string.IsNullOrEmpty(Title))
        {
            xml += $"  <ofd:Title>{Title}</ofd:Title>\n";
        }
        
        if (!string.IsNullOrEmpty(Content))
        {
            xml += $"  <ofd:Contents>{Content}</ofd:Contents>\n";
        }

        // 高亮特定属性
        xml += "  <ofd:Appearance>\n";
        xml += $"    <ofd:HighlightColor>{HighlightColor.ToHexString()}</ofd:HighlightColor>\n";
        xml += $"    <ofd:Opacity>{Opacity:F2}</ofd:Opacity>\n";
        xml += $"    <ofd:Style>{Style}</ofd:Style>\n";
        
        if (HighlightAreas.Count > 0)
        {
            xml += "    <ofd:HighlightAreas>\n";
            foreach (var area in HighlightAreas)
            {
                xml += $"      <ofd:Area>{area.X} {area.Y} {area.Width} {area.Height}</ofd:Area>\n";
            }
            xml += "    </ofd:HighlightAreas>\n";
        }
        
        xml += "  </ofd:Appearance>\n";
        xml += "</ofd:Annot>";

        return xml;
    }

    /// <summary>
    /// 检查指定点是否在高亮区域内
    /// </summary>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <returns>是否在高亮区域内</returns>
    public bool ContainsPoint(double x, double y)
    {
        return HighlightAreas.Any(area => area.Contains(x, y));
    }

    /// <summary>
    /// 检查是否与指定区域重叠
    /// </summary>
    /// <param name="box">检查的区域</param>
    /// <returns>是否重叠</returns>
    public bool IntersectsWith(StBox box)
    {
        return HighlightAreas.Any(area => area.IntersectsWith(box));
    }

    /// <summary>
    /// 获取高亮区域总面积
    /// </summary>
    /// <returns>总面积</returns>
    public double GetTotalArea()
    {
        return HighlightAreas.Sum(area => area.Width * area.Height);
    }

    /// <summary>
    /// 克隆高亮注释
    /// </summary>
    /// <param name="newId">新的注释ID</param>
    /// <returns>克隆的注释</returns>
    public HighlightAnnotation Clone(StId newId)
    {
        var clone = new HighlightAnnotation(newId, PageId, Boundary, HighlightColor)
        {
            Creator = Creator,
            Content = Content,
            Title = Title,
            Visible = Visible,
            Printable = Printable,
            Opacity = Opacity,
            Style = Style
        };

        clone.HighlightAreas.AddRange(HighlightAreas);
        return clone;
    }

    /// <summary>
    /// 获取高亮注释摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        return $"HighlightAnnotation[ID={Id}, Page={PageId}, Areas={HighlightAreas.Count}, Color={HighlightColor.ToHexString()}]";
    }
}

/// <summary>
/// 注释类型枚举
/// </summary>
public enum AnnotationType
{
    /// <summary>
    /// 高亮注释
    /// </summary>
    Highlight,

    /// <summary>
    /// 链接注释
    /// </summary>
    Link,

    /// <summary>
    /// 文本注释
    /// </summary>
    Text,

    /// <summary>
    /// 图章注释
    /// </summary>
    Stamp,

    /// <summary>
    /// 墨迹注释
    /// </summary>
    Ink,

    /// <summary>
    /// 形状注释
    /// </summary>
    Shape
}

/// <summary>
/// 高亮样式枚举
/// </summary>
public enum HighlightStyle
{
    /// <summary>
    /// 普通高亮
    /// </summary>
    Normal,

    /// <summary>
    /// 下划线
    /// </summary>
    Underline,

    /// <summary>
    /// 删除线
    /// </summary>
    Strikethrough,

    /// <summary>
    /// 波浪线
    /// </summary>
    Squiggly
}
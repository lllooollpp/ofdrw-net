using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Annotation;

/// <summary>
/// 链接注释类
/// 用于在文档中创建可点击的链接，支持URL链接、页面跳转、文件链接等
/// </summary>
public class LinkAnnotation : AnnotationBase
{
    /// <summary>
    /// 链接类型
    /// </summary>
    public LinkType LinkType { get; set; }

    /// <summary>
    /// 链接目标
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// 目标页面ID（用于页面跳转）
    /// </summary>
    public StId? TargetPageId { get; set; }

    /// <summary>
    /// 目标位置（用于页面跳转时的具体位置）
    /// </summary>
    public StBox? TargetLocation { get; set; }

    /// <summary>
    /// 链接边框样式
    /// </summary>
    public LinkBorderStyle BorderStyle { get; set; }

    /// <summary>
    /// 边框颜色
    /// </summary>
    public OfdrwNet.Core.Resource.Color? BorderColor { get; set; }

    /// <summary>
    /// 边框宽度
    /// </summary>
    public double BorderWidth { get; set; } = 1.0;

    /// <summary>
    /// 高亮模式（鼠标悬停时的效果）
    /// </summary>
    public LinkHighlightMode HighlightMode { get; set; }

    /// <summary>
    /// 是否在新窗口打开（用于URL链接）
    /// </summary>
    public bool OpenInNewWindow { get; set; } = true;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">注释ID</param>
    /// <param name="pageId">页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="linkType">链接类型</param>
    /// <param name="target">链接目标</param>
    public LinkAnnotation(StId id, StId pageId, StBox boundary, LinkType linkType, string target)
        : base(id, AnnotationType.Link, pageId, boundary)
    {
        LinkType = linkType;
        Target = target ?? throw new ArgumentNullException(nameof(target));
        BorderStyle = LinkBorderStyle.None;
        HighlightMode = LinkHighlightMode.Invert;
    }

    /// <summary>
    /// 创建URL链接注释
    /// </summary>
    /// <param name="id">注释ID</param>
    /// <param name="pageId">页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="url">URL地址</param>
    /// <returns>链接注释对象</returns>
    public static LinkAnnotation CreateUrlLink(StId id, StId pageId, StBox boundary, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL不能为空", nameof(url));
        }

        if (!IsValidUrl(url))
        {
            throw new ArgumentException("URL格式不正确", nameof(url));
        }

        return new LinkAnnotation(id, pageId, boundary, LinkType.Url, url)
        {
            Title = "网页链接",
            Content = $"点击访问: {url}"
        };
    }

    /// <summary>
    /// 创建页面跳转链接注释
    /// </summary>
    /// <param name="id">注释ID</param>
    /// <param name="pageId">当前页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="targetPageId">目标页面ID</param>
    /// <param name="targetLocation">目标位置（可选）</param>
    /// <returns>链接注释对象</returns>
    public static LinkAnnotation CreatePageLink(StId id, StId pageId, StBox boundary, StId targetPageId, StBox? targetLocation = null)
    {
        var link = new LinkAnnotation(id, pageId, boundary, LinkType.Page, targetPageId.ToString())
        {
            TargetPageId = targetPageId,
            TargetLocation = targetLocation,
            Title = "页面跳转",
            Content = $"跳转到第 {targetPageId} 页"
        };

        return link;
    }

    /// <summary>
    /// 创建文件链接注释
    /// </summary>
    /// <param name="id">注释ID</param>
    /// <param name="pageId">页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="filePath">文件路径</param>
    /// <returns>链接注释对象</returns>
    public static LinkAnnotation CreateFileLink(StId id, StId pageId, StBox boundary, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        return new LinkAnnotation(id, pageId, boundary, LinkType.File, filePath)
        {
            Title = "文件链接",
            Content = $"打开文件: {Path.GetFileName(filePath)}"
        };
    }

    /// <summary>
    /// 创建邮件链接注释
    /// </summary>
    /// <param name="id">注释ID</param>
    /// <param name="pageId">页面ID</param>
    /// <param name="boundary">边界框</param>
    /// <param name="email">邮箱地址</param>
    /// <param name="subject">邮件主题（可选）</param>
    /// <returns>链接注释对象</returns>
    public static LinkAnnotation CreateEmailLink(StId id, StId pageId, StBox boundary, string email, string? subject = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("邮箱地址不能为空", nameof(email));
        }

        if (!IsValidEmail(email))
        {
            throw new ArgumentException("邮箱地址格式不正确", nameof(email));
        }

        var target = string.IsNullOrEmpty(subject) ? $"mailto:{email}" : $"mailto:{email}?subject={Uri.EscapeDataString(subject)}";

        return new LinkAnnotation(id, pageId, boundary, LinkType.Email, target)
        {
            Title = "邮件链接",
            Content = $"发送邮件到: {email}"
        };
    }

    /// <summary>
    /// 设置边框样式
    /// </summary>
    /// <param name="style">边框样式</param>
    /// <param name="color">边框颜色</param>
    /// <param name="width">边框宽度</param>
    /// <returns>this</returns>
    public LinkAnnotation SetBorder(LinkBorderStyle style, OfdrwNet.Core.Resource.Color? color = null, double width = 1.0)
    {
        BorderStyle = style;
        BorderColor = color;
        BorderWidth = width;
        return this;
    }

    /// <summary>
    /// 设置高亮模式
    /// </summary>
    /// <param name="mode">高亮模式</param>
    /// <returns>this</returns>
    public LinkAnnotation SetHighlightMode(LinkHighlightMode mode)
    {
        HighlightMode = mode;
        return this;
    }

    /// <summary>
    /// 生成链接注释XML
    /// </summary>
    /// <returns>链接注释XML内容</returns>
    public override string ToXml()
    {
        var xml = $"<ofd:Annot ID='{Id}' Type='Link' PageRef='{PageId}' Visible='{Visible}' Print='{Printable}'>\n";
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

        // 链接特定属性
        xml += "  <ofd:Link>\n";
        xml += $"    <ofd:LinkType>{LinkType}</ofd:LinkType>\n";
        xml += $"    <ofd:Target>{EscapeXml(Target)}</ofd:Target>\n";

        if (TargetPageId != null)
        {
            xml += $"    <ofd:TargetPage>{TargetPageId}</ofd:TargetPage>\n";
        }

        if (TargetLocation != null)
        {
            xml += $"    <ofd:TargetLocation>{TargetLocation.X} {TargetLocation.Y} {TargetLocation.Width} {TargetLocation.Height}</ofd:TargetLocation>\n";
        }

        xml += $"    <ofd:HighlightMode>{HighlightMode}</ofd:HighlightMode>\n";
        xml += $"    <ofd:OpenInNewWindow>{OpenInNewWindow}</ofd:OpenInNewWindow>\n";

        // 边框样式
        if (BorderStyle != LinkBorderStyle.None)
        {
            xml += "    <ofd:Border>\n";
            xml += $"      <ofd:Style>{BorderStyle}</ofd:Style>\n";
            xml += $"      <ofd:Width>{BorderWidth}</ofd:Width>\n";
            if (BorderColor != null)
            {
                xml += $"      <ofd:Color>{BorderColor.ToHexString()}</ofd:Color>\n";
            }
            xml += "    </ofd:Border>\n";
        }

        xml += "  </ofd:Link>\n";
        xml += "</ofd:Annot>";

        return xml;
    }

    /// <summary>
    /// 验证链接目标是否有效
    /// </summary>
    /// <returns>验证结果</returns>
    public bool ValidateTarget()
    {
        return LinkType switch
        {
            LinkType.Url => IsValidUrl(Target),
            LinkType.Email => Target.StartsWith("mailto:") && IsValidEmail(Target.Substring(7).Split('?')[0]),
            LinkType.File => !string.IsNullOrWhiteSpace(Target),
            LinkType.Page => TargetPageId != null && TargetPageId.Value > 0,
            _ => !string.IsNullOrWhiteSpace(Target)
        };
    }

    /// <summary>
    /// 获取链接的显示文本
    /// </summary>
    /// <returns>显示文本</returns>
    public string GetDisplayText()
    {
        return LinkType switch
        {
            LinkType.Url => Target,
            LinkType.Email => Target.StartsWith("mailto:") ? Target.Substring(7).Split('?')[0] : Target,
            LinkType.File => Path.GetFileName(Target),
            LinkType.Page => $"第 {TargetPageId} 页",
            _ => Target
        };
    }

    /// <summary>
    /// 克隆链接注释
    /// </summary>
    /// <param name="newId">新的注释ID</param>
    /// <returns>克隆的注释</returns>
    public LinkAnnotation Clone(StId newId)
    {
        var clone = new LinkAnnotation(newId, PageId, Boundary, LinkType, Target)
        {
            Creator = Creator,
            Content = Content,
            Title = Title,
            Visible = Visible,
            Printable = Printable,
            Opacity = Opacity,
            TargetPageId = TargetPageId,
            TargetLocation = TargetLocation?.Clone(),
            BorderStyle = BorderStyle,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth,
            HighlightMode = HighlightMode,
            OpenInNewWindow = OpenInNewWindow
        };

        return clone;
    }

    /// <summary>
    /// 验证URL格式
    /// </summary>
    /// <param name="url">URL字符串</param>
    /// <returns>是否有效</returns>
    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// 验证邮箱格式
    /// </summary>
    /// <param name="email">邮箱字符串</param>
    /// <returns>是否有效</returns>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// XML转义
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>转义后的文本</returns>
    private static string EscapeXml(string text)
    {
        return text.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;")
                   .Replace("\"", "&quot;")
                   .Replace("'", "&apos;");
    }

    /// <summary>
    /// 获取链接注释摘要信息
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        return $"LinkAnnotation[ID={Id}, Page={PageId}, Type={LinkType}, Target={GetDisplayText()}]";
    }
}

/// <summary>
/// 链接类型枚举
/// </summary>
public enum LinkType
{
    /// <summary>
    /// URL链接
    /// </summary>
    Url,

    /// <summary>
    /// 页面跳转
    /// </summary>
    Page,

    /// <summary>
    /// 文件链接
    /// </summary>
    File,

    /// <summary>
    /// 邮件链接
    /// </summary>
    Email,

    /// <summary>
    /// 自定义链接
    /// </summary>
    Custom
}

/// <summary>
/// 链接边框样式枚举
/// </summary>
public enum LinkBorderStyle
{
    /// <summary>
    /// 无边框
    /// </summary>
    None,

    /// <summary>
    /// 实线
    /// </summary>
    Solid,

    /// <summary>
    /// 虚线
    /// </summary>
    Dashed,

    /// <summary>
    /// 点线
    /// </summary>
    Dotted,

    /// <summary>
    /// 下划线
    /// </summary>
    Underline
}

/// <summary>
/// 链接高亮模式枚举
/// </summary>
public enum LinkHighlightMode
{
    /// <summary>
    /// 无高亮
    /// </summary>
    None,

    /// <summary>
    /// 反转
    /// </summary>
    Invert,

    /// <summary>
    /// 轮廓
    /// </summary>
    Outline,

    /// <summary>
    /// 压入效果
    /// </summary>
    Push
}
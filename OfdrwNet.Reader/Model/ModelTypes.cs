namespace OfdrwNet.Reader.Model;

/// <summary>
/// OFD页面值对象
/// </summary>
public class OfdPageVo
{
    /// <summary>
    /// 页面索引
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 页面内容
    /// </summary>
    public string? Content { get; set; }
}

/// <summary>
/// 印章注释值对象
/// </summary>
public class StampAnnotVo
{
    /// <summary>
    /// 印章ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 印章类型
    /// </summary>
    public string? Type { get; set; }
}

/// <summary>
/// 注释实体
/// </summary>
public class AnnotationEntity
{
    /// <summary>
    /// 注释ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 注释类型
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 注释内容
    /// </summary>
    public string? Content { get; set; }
}
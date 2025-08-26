namespace OfdrwNet.Converter.OfdConverter;

/// <summary>
/// OFD转换器接口
/// 将其他格式文档转换为OFD格式
/// </summary>
public interface IOFDConverter : IDisposable
{
    /// <summary>
    /// 转换文档
    /// </summary>
    /// <param name="inputPath">输入文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ConvertAsync(string inputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 转换指定页面范围的文档
    /// </summary>
    /// <param name="inputPath">输入文件路径</param>
    /// <param name="startPage">起始页码（从0开始）</param>
    /// <param name="endPage">结束页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ConvertAsync(string inputPath, int startPage, int endPage, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置页面大小
    /// </summary>
    /// <param name="pageLayout">页面布局</param>
    void SetPageLayout(OfdrwNet.Layout.PageLayout pageLayout);

    /// <summary>
    /// 获取输出OFD文件路径
    /// </summary>
    string GetOutputPath();
}
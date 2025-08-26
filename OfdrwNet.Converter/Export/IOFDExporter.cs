namespace OfdrwNet.Converter.Export;

/// <summary>
/// OFD导出器基础接口
/// 对应 Java 版本的 org.ofdrw.converter.export.OFDExporter
/// </summary>
public interface IOFDExporter : IDisposable
{
    /// <summary>
    /// 导出所有页面
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExportAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出指定页面
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExportAsync(int pageNum, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出指定页面范围
    /// </summary>
    /// <param name="startPageNum">起始页码（从0开始）</param>
    /// <param name="endPageNum">结束页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExportAsync(int startPageNum, int endPageNum, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取输出文件路径
    /// </summary>
    /// <returns>输出文件路径列表</returns>
    List<string> GetOutputPaths();
}
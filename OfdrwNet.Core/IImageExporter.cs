using SkiaSharp;
using System.Xml.Linq;

namespace OfdrwNet.Core;

/// <summary>
/// 图像渲染器接口，负责渲染各种图形对象到画布上
/// </summary>
public interface IImageRenderer
{
    /// <summary>
    /// 渲染页面内容到画布
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="pageInfo">页面信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RenderPageContentAsync(SKCanvas canvas, dynamic pageInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 渲染单个图层
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="layer">图层元素</param>
    Task RenderLayerAsync(SKCanvas canvas, XElement layer);

    /// <summary>
    /// 渲染文本对象
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="textObject">文本对象元素</param>
    Task RenderTextObjectAsync(SKCanvas canvas, XElement textObject);

    /// <summary>
    /// 渲染图像对象
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="imageObject">图像对象元素</param>
    Task RenderImageObjectAsync(SKCanvas canvas, XElement imageObject);

    /// <summary>
    /// 渲染路径对象
    /// </summary>
    /// <param name="canvas">Skia画布</param>
    /// <param name="pathObject">路径对象元素</param>
    Task RenderPathObjectAsync(SKCanvas canvas, XElement pathObject);
}

/// <summary>
/// 图像导出器接口，定义从文档导出图像的标准方法
/// </summary>
public interface IImageExporter
{
    /// <summary>
    /// 导出指定页面为图像
    /// </summary>
    /// <param name="pageNum">页码（从0开始）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导出的图像数据</returns>
    Task<byte[]> ExportPageToImageAsync(int pageNum, CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出所有页面为图像
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有页面的图像数据列表</returns>
    Task<IEnumerable<byte[]>> ExportAllPagesToImageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取图像格式对应的文件扩展名
    /// </summary>
    /// <param name="format">图像格式</param>
    /// <returns>文件扩展名</returns>
    string GetFileExtension(SKEncodedImageFormat format);
}

/// <summary>
/// 图像导出配置
/// </summary>
public interface IImageExportConfig
{
    /// <summary>
    /// 图像格式
    /// </summary>
    SKEncodedImageFormat ImageFormat { get; }

    /// <summary>
    /// 图像质量（0-100），仅对JPEG有效
    /// </summary>
    int Quality { get; }

    /// <summary>
    /// 分辨率（DPI）
    /// </summary>
    float Dpi { get; }
}

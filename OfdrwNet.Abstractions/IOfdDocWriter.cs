using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OfdrwNet.Abstractions;

/// <summary>
/// 供转换层使用的最小 OFD 文档写入接口，避免循环依赖与反射。
/// 流式返回自身（IOfdDocWriter）。
/// </summary>
public interface IOfdDocWriter : IDisposable
{
    ILogger? Logger { get; }
    IOfdDocWriter AddExternalEmbeddedFont(string fontName, string fontFilePath);
    /// <summary>
    /// 添加一段原始文字运行（GlyphRun）。
    /// 新增 ctm 参数用于精确控制坐标变换（六参数矩阵 a b c d e f）。
    /// </summary>
    /// <param name="fontName">字体名</param>
    /// <param name="fontSizeMm">字体大小（毫米）</param>
    /// <param name="originX">基线原点 X（毫米）</param>
    /// <param name="originY">基线原点 Y（毫米）</param>
    /// <param name="text">文本内容</param>
    /// <param name="deltaX">字形 X 方向增量数组</param>
    /// <param name="deltaY">字形 Y 方向增量数组（暂未使用）</param>
    /// <param name="page">页码，从 1 开始</param>
    /// <param name="ctm">可选 6 参数 CTM 变换矩阵 (a b c d e f)</param>
    IOfdDocWriter AddRawTextGlyphRun(string fontName, double fontSizeMm, double originX, double originY, string text, double[]? deltaX = null, double[]? deltaY = null, int page = 1, double[]? ctm = null);
    /// <summary>
    /// 添加注释资源引用。
    /// </summary>
    /// <param name="annotation">注释对象</param>
    /// <param name="page">页码，从 1 开始</param>
    IOfdDocWriter AddAnnotation(object annotation, int page = 1);
    /// <summary>
    /// 关闭文档并完成所有写入操作。
    /// </summary>
    Task CloseAsync();
}

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
    IOfdDocWriter AddRawTextGlyphRun(string fontName, double fontSizeMm, double originX, double originY, string text, double[]? deltaX = null, double[]? deltaY = null, int page = 1);
    IOfdDocWriter AddRawImage(string format, double x, double y, double width, double height, byte[] data, int page = 1);
    Task CloseAsync();
}

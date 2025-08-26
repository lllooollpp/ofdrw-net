﻿namespace OfdrwNet.Graphics;

/// <summary>
/// 图形绘制模块的基础常量和枚举定义
/// 对应 Java 版本的 org.ofdrw.graphics2d 相关功能
/// 基于 SkiaSharp 实现 OFD 图形绘制
/// </summary>
public static class GraphicsConstants
{
    /// <summary>
    /// 默认线宽（毫米）
    /// </summary>
    public const float DefaultLineWidth = 0.353f;

    /// <summary>
    /// 默认颜色（黑色）
    /// </summary>
    public const uint DefaultColor = 0xFF000000;

    /// <summary>
    /// 默认背景色（白色）
    /// </summary>
    public const uint DefaultBackgroundColor = 0xFFFFFFFF;

    /// <summary>
    /// 毫米到点的转换因子
    /// </summary>
    public const float MmToPoint = 72.0f / 25.4f;

    /// <summary>
    /// 点到毫米的转换因子
    /// </summary>
    public const float PointToMm = 25.4f / 72.0f;
}

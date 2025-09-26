using System;

namespace OfdrwNet.Reader.Rendering
{
    /// <summary>
    /// 全局渲染配置（轻量，可在后续扩展为可热更新或外部注入）
    /// </summary>
    public static class RenderingConfig
    {
        /// <summary>
        /// 是否启用统一缩放模式：
        /// true  -> PageContentExtractor 保留 mm 逻辑坐标，渲染阶段按 RenderContext.ScaleFactor 统一缩放
        /// false -> 提取阶段直接换算为像素坐标（当前临时逻辑）
        /// </summary>
    // 回退：默认关闭统一缩放模式，先保证基础展示正确（像素坐标路径）
    public static bool UnifiedScalingMode { get; set; } = false;

        /// <summary>
        /// 调试开关：是否绘制对象边界/布局辅助框
        /// </summary>
        public static bool DebugDrawBounds { get; set; } = false;
    }
}

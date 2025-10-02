namespace OfdrwNet.Converter.Domain;

/// <summary>
/// CLI 配置聚合，封装所有转换选项
/// </summary>
public sealed class ConverterOptions
{
    /// <summary>
    /// 表格识别置信度阈值 (0.0-1.0)
    /// </summary>
    public float TableThreshold { get; init; } = 0.8f;

    /// <summary>
    /// 公式识别置信度阈值 (0.0-1.0)
    /// </summary>
    public float FormulaThreshold { get; init; } = 0.8f;

    /// <summary>
    /// 渲染意图
    /// </summary>
    public string RenderIntent { get; init; } = "perceptual";

    /// <summary>
    /// 兼容性级别
    /// </summary>
    public CompatLevel CompatLevel { get; init; } = CompatLevel.Std2020;

    /// <summary>
    /// 目标阅读器标识
    /// </summary>
    public string? TargetReader { get; init; }

    /// <summary>
    /// 最大内存限制 (MB)
    /// </summary>
    public int MaxMemMB { get; init; } = 512;

    /// <summary>
    /// 每段页面数（用于分段处理）
    /// </summary>
    public int PagesPerSegment { get; init; } = 100;

    /// <summary>
    /// 权限配置
    /// </summary>
    public PermissionConfig? Permissions { get; init; }

    /// <summary>
    /// 版本策略配置
    /// </summary>
    public VersionPolicy? VersionPolicy { get; init; }

    /// <summary>
    /// 是否启用 JavaScript 快照执行
    /// </summary>
    public bool RunJsSnapshot { get; init; }

    /// <summary>
    /// 是否追加版本
    /// </summary>
    public bool AppendVersion { get; init; }

    /// <summary>
    /// 验证阈值配置是否有效
    /// </summary>
    public void Validate()
    {
        if (TableThreshold < 0.0f || TableThreshold > 1.0f)
        {
            throw new ArgumentException("TableThreshold must be between 0.0 and 1.0");
        }

        if (FormulaThreshold < 0.0f || FormulaThreshold > 1.0f)
        {
            throw new ArgumentException("FormulaThreshold must be between 0.0 and 1.0");
        }

        if (MaxMemMB <= 0)
        {
            throw new ArgumentException("MaxMemMB must be positive");
        }

        if (PagesPerSegment <= 0)
        {
            throw new ArgumentException("PagesPerSegment must be positive");
        }

        Permissions?.Validate();
        VersionPolicy?.Validate();
    }
}

/// <summary>
/// 兼容性级别枚举
/// </summary>
public enum CompatLevel
{
    /// <summary>
    /// 基础兼容性
    /// </summary>
    Base,

    /// <summary>
    /// OFD 2020 标准兼容性
    /// </summary>
    Std2020,

    /// <summary>
    /// 完整功能支持
    /// </summary>
    Full
}

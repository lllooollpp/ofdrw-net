namespace OfdrwNet.Converter.Domain;

/// <summary>
/// 权限配置
/// </summary>
public sealed class PermissionConfig
{
    /// <summary>
    /// 是否允许打印（低质量）
    /// </summary>
    public bool Print { get; init; } = true;

    /// <summary>
    /// 是否允许高质量打印
    /// </summary>
    public bool PrintHQ { get; init; } = true;

    /// <summary>
    /// 是否允许修改内容
    /// </summary>
    public bool Modify { get; init; } = true;

    /// <summary>
    /// 是否允许注释
    /// </summary>
    public bool Annotate { get; init; } = true;

    /// <summary>
    /// 是否允许导出（复制）
    /// </summary>
    public bool Export { get; init; } = true;

    /// <summary>
    /// 所有者标识（用于权限管理）
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// 验证权限配置的有效性
    /// </summary>
    public void Validate()
    {
        // 业务规则：如果 Print=false，则 PrintHQ 必须为 false
        if (!Print && PrintHQ)
        {
            throw new InvalidOperationException("PrintHQ cannot be true when Print is false");
        }
    }

    /// <summary>
    /// 将权限配置转换为权限位掩码
    /// </summary>
    public int ToPermissionBits()
    {
        int bits = 0;
        if (Print) bits |= 0x04;
        if (Modify) bits |= 0x08;
        if (Export) bits |= 0x10;
        if (Annotate) bits |= 0x20;
        if (PrintHQ) bits |= 0x800;
        return bits;
    }
}

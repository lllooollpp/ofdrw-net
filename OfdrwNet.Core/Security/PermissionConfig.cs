using System;
using System.Collections.Generic;
using System.Text;

namespace OfdrwNet.Core.Security;

/// <summary>
/// 转换输出的权限配置。
/// 满足 quickstart 中的权限选项并与测试契约保持一致。
/// </summary>
public sealed class PermissionConfig
{
    /// <summary>
    /// 创建权限配置，默认全部启用。
    /// </summary>
    public PermissionConfig()
    {
    }

    /// <summary>
    /// 打印权限。
    /// </summary>
    public bool Print { get; init; } = true;

    /// <summary>
    /// 高清打印权限。
    /// </summary>
    public bool PrintHQ { get; init; } = true;

    /// <summary>
    /// 修改权限。
    /// </summary>
    public bool Modify { get; init; } = true;

    /// <summary>
    /// 注释权限。
    /// </summary>
    public bool Annotate { get; init; } = true;

    /// <summary>
    /// 导出权限。
    /// </summary>
    public bool Export { get; init; } = true;

    /// <summary>
    /// 拥有者权限（等同于全部权限）。
    /// </summary>
    public bool Owner { get; init; } = true;

    /// <summary>
    /// 填表权限。
    /// </summary>
    public bool FillForms { get; init; } = true;

    /// <summary>
    /// 拷贝权限。
    /// </summary>
    public bool Copy { get; init; } = true;

    /// <summary>
    /// 辅助拷贝（仅可访问性）。
    /// </summary>
    public bool CopyAccessible { get; init; } = true;

    /// <summary>
    /// 集合装订权限。
    /// </summary>
    public bool Assemble { get; init; } = true;

    /// <summary>
    /// 验证配置是否符合约束，若不符合抛出异常。
    /// </summary>
    public void Validate()
    {
        if (PrintHQ && !Print)
        {
            throw new InvalidOperationException("High quality print requires print permission.");
        }

        if (Owner && !(Print && PrintHQ && Modify && Annotate && Export && FillForms && Copy && CopyAccessible && Assemble))
        {
            throw new InvalidOperationException("Owner permission implies all granular permissions must be true.");
        }
    }

    /// <summary>
    /// 将权限编码为位掩码表示（便于传递给底层加密模块）。
    /// </summary>
    public uint ToBitMask()
    {
        var mask = 0u;
        SetBit(ref mask, 0, Print);
        SetBit(ref mask, 1, PrintHQ);
        SetBit(ref mask, 2, Modify);
        SetBit(ref mask, 3, Annotate);
        SetBit(ref mask, 4, Export);
        SetBit(ref mask, 5, FillForms);
        SetBit(ref mask, 6, Copy);
        SetBit(ref mask, 7, CopyAccessible);
        SetBit(ref mask, 8, Assemble);
        SetBit(ref mask, 9, Owner);
        return mask;
    }

    /// <summary>
    /// 输出调试友好的描述。
    /// </summary>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append("Permissions[");
        builder.Append(Print ? "print" : "no-print");
        builder.Append(',');
        builder.Append(PrintHQ ? "hq" : "no-hq");
        builder.Append(',');
        builder.Append(Modify ? "modify" : "no-modify");
        builder.Append(',');
        builder.Append(Annotate ? "annotate" : "no-annotate");
        builder.Append(',');
        builder.Append(Export ? "export" : "no-export");
        builder.Append(',');
        builder.Append(FillForms ? "forms" : "no-forms");
        builder.Append(',');
        builder.Append(Copy ? "copy" : "no-copy");
        builder.Append(',');
        builder.Append(CopyAccessible ? "access" : "no-access");
        builder.Append(',');
        builder.Append(Assemble ? "assemble" : "no-assemble");
        builder.Append(',');
        builder.Append(Owner ? "owner" : "user");
        builder.Append(']');
        return builder.ToString();
    }

    /// <summary>
    /// 从位掩码创建配置。
    /// </summary>
    public static PermissionConfig FromBitMask(uint mask)
    {
        return new PermissionConfig
        {
            Print = GetBit(mask, 0),
            PrintHQ = GetBit(mask, 1),
            Modify = GetBit(mask, 2),
            Annotate = GetBit(mask, 3),
            Export = GetBit(mask, 4),
            FillForms = GetBit(mask, 5),
            Copy = GetBit(mask, 6),
            CopyAccessible = GetBit(mask, 7),
            Assemble = GetBit(mask, 8),
            Owner = GetBit(mask, 9)
        };
    }

    /// <summary>
    /// 根据命名键创建配置。
    /// </summary>
    public static PermissionConfig FromDictionary(IReadOnlyDictionary<string, bool> flags)
    {
        if (flags is null)
        {
            throw new ArgumentNullException(nameof(flags));
        }

        return new PermissionConfig
        {
            Print = GetFlag(flags, "print", defaultValue: true),
            PrintHQ = GetFlag(flags, "printhq", defaultValue: true),
            Modify = GetFlag(flags, "modify", defaultValue: true),
            Annotate = GetFlag(flags, "annotate", defaultValue: true),
            Export = GetFlag(flags, "export", defaultValue: true),
            FillForms = GetFlag(flags, "fillforms", defaultValue: true),
            Copy = GetFlag(flags, "copy", defaultValue: true),
            CopyAccessible = GetFlag(flags, "copyaccessible", defaultValue: true),
            Assemble = GetFlag(flags, "assemble", defaultValue: true),
            Owner = GetFlag(flags, "owner", defaultValue: true)
        };
    }

    private static void SetBit(ref uint mask, int index, bool value)
    {
        if (value)
        {
            mask |= 1u << index;
        }
        else
        {
            mask &= ~(1u << index);
        }
    }

    private static bool GetBit(uint mask, int index) => (mask & (1u << index)) != 0;

    private static bool GetFlag(IReadOnlyDictionary<string, bool> flags, string key, bool defaultValue)
    {
        if (flags.TryGetValue(key, out var value))
        {
            return value;
        }

        if (flags.TryGetValue(key.ToLowerInvariant(), out value))
        {
            return value;
        }

        return defaultValue;
    }
}

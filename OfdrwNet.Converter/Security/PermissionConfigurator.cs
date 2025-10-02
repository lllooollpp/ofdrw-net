using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;

namespace OfdrwNet.Converter.Security;

/// <summary>
/// 权限配置器。
/// </summary>
/// <remarks>
/// 将 CLI 标志映射为 OFD 权限配置。
/// FR-26~FR-28: 文档权限控制
///
/// 功能:
/// - 解析权限标志字符串
/// - 验证权限组合有效性
/// - 生成 OFD 权限位掩码
/// - 支持权限预设模板
///
/// 权限类型:
/// - Print: 打印(低质量)
/// - PrintHQ: 打印(高质量)
/// - Modify: 修改内容
/// - Annotate: 添加注释
/// - Export: 导出/复制
/// </remarks>
public sealed class PermissionConfigurator
{
    private readonly ILogger<PermissionConfigurator> _logger;

    // 预设权限模板
    private static readonly Dictionary<string, PermissionConfig> _presets = new()
    {
        ["full"] = new PermissionConfig
        {
            Print = true,
            PrintHQ = true,
            Modify = true,
            Annotate = true,
            Export = true
        },
        ["readonly"] = new PermissionConfig
        {
            Print = true,
            PrintHQ = true,
            Modify = false,
            Annotate = false,
            Export = true
        },
        ["noprint"] = new PermissionConfig
        {
            Print = false,
            PrintHQ = false,
            Modify = false,
            Annotate = true,
            Export = true
        },
        ["locked"] = new PermissionConfig
        {
            Print = false,
            PrintHQ = false,
            Modify = false,
            Annotate = false,
            Export = false
        }
    };

    /// <summary>
    /// 初始化 PermissionConfigurator 实例。
    /// </summary>
    public PermissionConfigurator(ILogger<PermissionConfigurator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 从 CLI 标志字符串配置权限。
    /// </summary>
    /// <param name="flags">权限标志(逗号分隔,如 "print,modify" 或预设名称 "readonly")</param>
    /// <param name="owner">所有者标识(可选)</param>
    /// <returns>权限配置</returns>
    public PermissionConfig Configure(string flags, string? owner = null)
    {
        if (string.IsNullOrWhiteSpace(flags))
        {
            _logger.LogInformation("No permission flags specified, using default (full access)");
            var fullPreset = _presets["full"];
            return new PermissionConfig
            {
                Print = fullPreset.Print,
                PrintHQ = fullPreset.PrintHQ,
                Modify = fullPreset.Modify,
                Annotate = fullPreset.Annotate,
                Export = fullPreset.Export,
                Owner = owner
            };
        }

        var normalizedFlags = flags.Trim().ToLowerInvariant();

        // 检查是否为预设模板
        if (_presets.TryGetValue(normalizedFlags, out var preset))
        {
            _logger.LogInformation("Using preset permission template: {Preset}", normalizedFlags);
            return new PermissionConfig
            {
                Print = preset.Print,
                PrintHQ = preset.PrintHQ,
                Modify = preset.Modify,
                Annotate = preset.Annotate,
                Export = preset.Export,
                Owner = owner
            };
        }

        // 解析自定义标志
        return ParseFlags(normalizedFlags, owner);
    }

    /// <summary>
    /// 解析权限标志字符串。
    /// </summary>
    private PermissionConfig ParseFlags(string flags, string? owner)
    {
        var parts = flags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var config = new PermissionConfig
        {
            Print = false,
            PrintHQ = false,
            Modify = false,
            Annotate = false,
            Export = false,
            Owner = owner
        };

        var permissions = new Dictionary<string, bool>();

        foreach (var part in parts)
        {
            var flag = part.ToLowerInvariant();

            switch (flag)
            {
                case "print":
                    permissions["Print"] = true;
                    break;
                case "printhq":
                case "print-hq":
                    permissions["PrintHQ"] = true;
                    permissions["Print"] = true; // PrintHQ 隐含 Print
                    break;
                case "modify":
                case "edit":
                    permissions["Modify"] = true;
                    break;
                case "annotate":
                case "comment":
                    permissions["Annotate"] = true;
                    break;
                case "export":
                case "copy":
                    permissions["Export"] = true;
                    break;
                case "noprint":
                    permissions["Print"] = false;
                    permissions["PrintHQ"] = false;
                    break;
                case "nomodify":
                case "noedit":
                    permissions["Modify"] = false;
                    break;
                case "noannotate":
                    permissions["Annotate"] = false;
                    break;
                case "noexport":
                case "nocopy":
                    permissions["Export"] = false;
                    break;
                default:
                    _logger.LogWarning("Unknown permission flag: {Flag}", part);
                    break;
            }
        }

        config = new PermissionConfig
        {
            Print = permissions.GetValueOrDefault("Print", false),
            PrintHQ = permissions.GetValueOrDefault("PrintHQ", false),
            Modify = permissions.GetValueOrDefault("Modify", false),
            Annotate = permissions.GetValueOrDefault("Annotate", false),
            Export = permissions.GetValueOrDefault("Export", false),
            Owner = owner
        };

        _logger.LogInformation(
            "Configured permissions: Print={Print}, PrintHQ={PrintHQ}, Modify={Modify}, Annotate={Annotate}, Export={Export}",
            config.Print, config.PrintHQ, config.Modify, config.Annotate, config.Export);

        // 验证配置有效性
        config.Validate();

        return config;
    }

    /// <summary>
    /// 从权限位掩码解析权限配置。
    /// </summary>
    /// <param name="bits">权限位掩码</param>
    /// <returns>权限配置</returns>
    public PermissionConfig FromPermissionBits(int bits)
    {
        var config = new PermissionConfig
        {
            Print = (bits & 0x04) != 0,
            Modify = (bits & 0x08) != 0,
            Export = (bits & 0x10) != 0,
            Annotate = (bits & 0x20) != 0,
            PrintHQ = (bits & 0x800) != 0
        };

        _logger.LogDebug("Parsed permission bits 0x{Bits:X} to config", bits);
        return config;
    }

    /// <summary>
    /// 获取所有可用的预设模板名称。
    /// </summary>
    public IReadOnlyList<string> GetPresetNames()
    {
        return _presets.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 检查权限是否为只读模式。
    /// </summary>
    public bool IsReadOnly(PermissionConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return !config.Modify && !config.Annotate;
    }

    /// <summary>
    /// 检查权限是否完全锁定。
    /// </summary>
    public bool IsFullyLocked(PermissionConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return !config.Print && !config.PrintHQ && !config.Modify && !config.Annotate && !config.Export;
    }

    /// <summary>
    /// 合并两个权限配置(取交集,更严格)。
    /// </summary>
    public PermissionConfig Merge(PermissionConfig config1, PermissionConfig config2)
    {
        if (config1 == null)
        {
            throw new ArgumentNullException(nameof(config1));
        }

        if (config2 == null)
        {
            throw new ArgumentNullException(nameof(config2));
        }

        var merged = new PermissionConfig
        {
            Print = config1.Print && config2.Print,
            PrintHQ = config1.PrintHQ && config2.PrintHQ,
            Modify = config1.Modify && config2.Modify,
            Annotate = config1.Annotate && config2.Annotate,
            Export = config1.Export && config2.Export,
            Owner = config1.Owner ?? config2.Owner
        };

        _logger.LogDebug("Merged two permission configs (intersection)");
        return merged;
    }
}

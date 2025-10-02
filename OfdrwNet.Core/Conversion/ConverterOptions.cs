using System;
using System.Collections.Generic;
using System.Globalization;
using OfdrwNet.Core.Security;
using OfdrwNet.Core.Versioning;

namespace OfdrwNet.Core.Conversion
{
    /// <summary>
    /// 转换器配置选项聚合，包含所有 CLI 参数映射和 quickstart 字段
    /// 对应 DR 全局需求，用于控制转换过程的各种行为
    /// </summary>
    public class ConverterOptions
    {
        #region 识别相关配置

        /// <summary>
        /// 表格识别置信度阈值，低于此值不生成 CompositeObject 节点
        /// 对应 quickstart 参数 --table-recog-threshold
        /// </summary>
        public float TableRecognitionThreshold { get; set; } = 0.8f;

        /// <summary>
        /// 公式识别置信度阈值，低于此值不生成 CompositeObject 节点
        /// 对应 quickstart 参数 --formula-recog-threshold
        /// </summary>
        public float FormulaRecognitionThreshold { get; set; } = 0.8f;

        #endregion

        #region 兼容性配置

        /// <summary>
        /// 兼容性级别，影响特性降级行为
        /// 对应 quickstart 参数 --compat-level
        /// 默认值: Std2020
        /// </summary>
        public string CompatibilityLevel { get; set; } = "Std2020";

        /// <summary>
        /// 目标阅读器类型，用于特定阅读器的优化和兼容性调整
        /// 对应 quickstart 参数 --target-reader
        /// 常见值: Foxit, Adobe, Suwell
        /// </summary>
        public string TargetReader { get; set; } = "Foxit";

        #endregion

        #region 颜色和渲染配置

        /// <summary>
        /// 渲染意图，控制颜色转换方式
        /// 对应 quickstart 参数 --render-intent
        /// 可选值: perceptual, relative, saturation, absolute
        /// </summary>
        public string RenderIntent { get; set; } = "perceptual";

        #endregion

        #region 内存和性能配置

        /// <summary>
        /// 最大内存使用量（MB），超过此值触发分段处理
        /// 对应 quickstart 参数 --max-mem
        /// </summary>
        public int MaxMemoryMB { get; set; } = 512;

        /// <summary>
        /// 每段页数，用于内存分段策略
        /// 对应 quickstart 参数 --pages-per-segment
        /// </summary>
        public int PagesPerSegment { get; set; } = 100;

        /// <summary>
        /// 并行处理线程数
        /// 对应 quickstart 参数 --parallel（批量模式）
        /// </summary>
        public int ParallelThreads { get; set; } = 4;

        #endregion

        #region 权限和安全配置

        /// <summary>
        /// 权限位配置字符串
        /// 对应 quickstart 参数 --perm
        /// 格式: "print=true,modify=false,export=true"
        /// </summary>
        public string PermissionBits { get; set; } = string.Empty;

        /// <summary>
        /// 解析后的权限配置对象
        /// </summary>
        public PermissionConfig? Permissions { get; set; }

        #endregion

        #region 版本管理配置

        /// <summary>
        /// 版本策略配置字符串
        /// 对应 quickstart 参数 --version-policy
        /// 格式: "maxChain=30,sizeLimit=3x"
        /// </summary>
        public string VersionPolicyString { get; set; } = string.Empty;

        /// <summary>
        /// 解析后的版本策略对象
        /// </summary>
        public VersionPolicy? VersionPolicy { get; set; }

        /// <summary>
        /// 是否启用版本链追加模式
        /// 对应 quickstart 参数 --append-version
        /// </summary>
        public bool AppendVersion { get; set; } = false;

        #endregion

        #region JavaScript 和脚本配置

        /// <summary>
        /// 是否运行 JavaScript 快照
        /// 对应 quickstart 参数 --run-js-snapshot
        /// </summary>
        public bool RunJavaScriptSnapshot { get; set; } = false;

        #endregion

        #region 输入输出配置

        /// <summary>
        /// 输入路径（文件或目录）
        /// 对应 quickstart 参数 --input
        /// </summary>
        public string InputPath { get; set; } = string.Empty;

        /// <summary>
        /// 输出目录路径
        /// 对应 quickstart 参数 --output 或 --output-root
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// 是否为批量转换模式
        /// </summary>
        public bool IsBatchMode { get; set; } = false;

        #endregion

        #region 日志和调试配置

        /// <summary>
        /// 结构化日志文件路径
        /// 对应 quickstart 参数 --structured-log
        /// </summary>
        public string StructuredLogPath { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用详细日志输出
        /// 对应 CLI 参数 --verbose
        /// </summary>
        public bool VerboseLogging { get; set; } = false;

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        /// <returns>验证结果，包含错误信息</returns>
        public ValidationResult Validate()
        {
            var errors = new List<string>();

            // 验证输入路径
            if (string.IsNullOrWhiteSpace(InputPath))
            {
                errors.Add("输入路径不能为空");
            }

            // 验证输出路径
            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                errors.Add("输出路径不能为空");
            }

            // 验证识别阈值
            if (TableRecognitionThreshold < 0 || TableRecognitionThreshold > 1)
            {
                errors.Add("表格识别阈值必须在 0 到 1 之间");
            }

            if (FormulaRecognitionThreshold < 0 || FormulaRecognitionThreshold > 1)
            {
                errors.Add("公式识别阈值必须在 0 到 1 之间");
            }

            // 验证内存配置
            if (MaxMemoryMB <= 0)
            {
                errors.Add("最大内存配置必须大于 0");
            }

            if (PagesPerSegment <= 0)
            {
                errors.Add("每段页数必须大于 0");
            }

            // 验证并行线程数
            if (ParallelThreads <= 0)
            {
                errors.Add("并行线程数必须大于 0");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// 从权限位字符串解析权限配置
        /// </summary>
        public void ParsePermissions()
        {
            if (string.IsNullOrWhiteSpace(PermissionBits))
            {
                Permissions = null;
                return;
            }

            var flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var parts = PermissionBits.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length != 2)
                {
                    continue;
                }

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                var boolValue = value.Equals("true", StringComparison.OrdinalIgnoreCase)
                                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                                || value.Equals("on", StringComparison.OrdinalIgnoreCase);

                flags[key] = boolValue;
            }

            var permissions = PermissionConfig.FromDictionary(flags);
            permissions.Validate();
            Permissions = permissions;
        }

        /// <summary>
        /// 从版本策略字符串解析版本策略配置
        /// </summary>
        public void ParseVersionPolicy()
        {
            if (string.IsNullOrWhiteSpace(VersionPolicyString))
            {
                VersionPolicy = null;
                return;
            }

            int? maxChain = null;
            double? sizeLimitRatio = null;
            bool? autoCompact = null;
            TimeSpan? maxAge = null;

            var parts = VersionPolicyString.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length != 2)
                {
                    continue;
                }

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();
                switch (key.ToLowerInvariant())
                {
                    case "maxchain":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedChain))
                        {
                            maxChain = parsedChain;
                        }
                        break;
                    case "sizelimit":
                        var numeric = value.EndsWith("x", StringComparison.OrdinalIgnoreCase)
                            ? value[..^1]
                            : value;

                        if (double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRatio))
                        {
                            sizeLimitRatio = parsedRatio;
                        }
                        break;
                    case "autocompact":
                        autoCompact = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "maxage":
                        if (TryParseDuration(value, out var duration))
                        {
                            maxAge = duration;
                        }
                        break;
                }
            }

            var policy = new VersionPolicy
            {
                MaxChain = maxChain ?? 30,
                SizeLimitRatio = sizeLimitRatio ?? 3.0,
                AutoCompact = autoCompact ?? true,
                MaxAge = maxAge
            };

            policy.Validate();
            VersionPolicy = policy;
        }

        private static bool TryParseDuration(string input, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            input = input.Trim();
            var suffix = input[^1];
            var numberPart = input[..^1];

            if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            duration = suffix switch
            {
                'd' or 'D' => TimeSpan.FromDays(value),
                'h' or 'H' => TimeSpan.FromHours(value),
                'm' or 'M' => TimeSpan.FromMinutes(value),
                _ => TimeSpan.Zero
            };

            return suffix is 'd' or 'D' or 'h' or 'H' or 'm' or 'M';
        }

        /// <summary>
        /// 创建配置的深度副本
        /// </summary>
        public ConverterOptions Clone()
        {
            var clone = new ConverterOptions
            {
                TableRecognitionThreshold = TableRecognitionThreshold,
                FormulaRecognitionThreshold = FormulaRecognitionThreshold,
                CompatibilityLevel = CompatibilityLevel,
                TargetReader = TargetReader,
                RenderIntent = RenderIntent,
                MaxMemoryMB = MaxMemoryMB,
                PagesPerSegment = PagesPerSegment,
                ParallelThreads = ParallelThreads,
                PermissionBits = PermissionBits,
                VersionPolicyString = VersionPolicyString,
                AppendVersion = AppendVersion,
                RunJavaScriptSnapshot = RunJavaScriptSnapshot,
                InputPath = InputPath,
                OutputPath = OutputPath,
                IsBatchMode = IsBatchMode,
                StructuredLogPath = StructuredLogPath,
                VerboseLogging = VerboseLogging
            };

            clone.ParsePermissions();
            clone.ParseVersionPolicy();

            return clone;
        }

        #endregion
    }

    /// <summary>
    /// 配置验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }

}

using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OfdrwNet.Converter.Extensions;

/// <summary>
/// 元数据服务。
/// </summary>
/// <remarks>
/// 写入 OFD CustomData 和标签。
/// FR-29: 文档元数据扩展
///
/// 功能:
/// - 写入转换来源信息
/// - 嵌入错误/警告记录
/// - 添加自定义标签
/// - 生成转换报告摘要
///
/// CustomData 结构:
/// - conversion.source: PDF 文件路径
/// - conversion.timestamp: 转换时间
/// - conversion.version: 转换器版本
/// - conversion.errors: 错误记录列表
/// </remarks>
public sealed class MetadataService
{
    private readonly ILogger<MetadataService> _logger;
    private readonly Dictionary<string, object> _customData;
    private readonly List<string> _tags;

    /// <summary>
    /// 初始化 MetadataService 实例。
    /// </summary>
    public MetadataService(ILogger<MetadataService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _customData = new Dictionary<string, object>();
        _tags = new List<string>();
    }

    /// <summary>
    /// 添加转换来源信息。
    /// </summary>
    /// <param name="sourcePath">源 PDF 文件路径</param>
    /// <param name="sourceHash">源文件 SHA-256 哈希</param>
    public void AddConversionSource(string sourcePath, string? sourceHash = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path cannot be null or empty", nameof(sourcePath));
        }

        _customData["conversion.source"] = sourcePath;
        _customData["conversion.timestamp"] = DateTime.UtcNow.ToString("O");
        _customData["conversion.version"] = GetConverterVersion();

        if (!string.IsNullOrWhiteSpace(sourceHash))
        {
            _customData["conversion.source_hash"] = sourceHash;
        }

        _logger.LogInformation("Added conversion source metadata: {Source}", sourcePath);
    }

    /// <summary>
    /// 嵌入错误记录。
    /// </summary>
    /// <param name="errors">错误记录列表</param>
    public void EmbedErrors(IEnumerable<ErrorRecord> errors)
    {
        if (errors == null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        var errorList = errors.ToList();
        if (errorList.Count == 0)
        {
            _logger.LogDebug("No errors to embed");
            return;
        }

        var errorSummary = new
        {
            total = errorList.Count,
            fatal = errorList.Count(e => e.Severity == ErrorSeverity.Fatal),
            error = errorList.Count(e => e.Severity == ErrorSeverity.Error),
            warning = errorList.Count(e => e.Severity == ErrorSeverity.Warning),
            info = errorList.Count(e => e.Severity == ErrorSeverity.Info),
            records = errorList.Select(e => new
            {
                severity = e.Severity.ToString(),
                code = e.Code,
                message = e.Message,
                location = e.Location,
                timestamp = e.Timestamp.ToString("O")
            }).ToList()
        };

        _customData["conversion.errors"] = errorSummary;
        _logger.LogInformation("Embedded {Count} error records (Fatal: {Fatal}, Error: {Error}, Warning: {Warning})",
            errorList.Count, errorSummary.fatal, errorSummary.error, errorSummary.warning);
    }

    /// <summary>
    /// 添加自定义标签。
    /// </summary>
    /// <param name="tag">标签名称</param>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag cannot be null or empty", nameof(tag));
        }

        if (!_tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            _tags.Add(tag);
            _logger.LogDebug("Added tag: {Tag}", tag);
        }
    }

    /// <summary>
    /// 批量添加标签。
    /// </summary>
    public void AddTags(IEnumerable<string> tags)
    {
        if (tags == null)
        {
            throw new ArgumentNullException(nameof(tags));
        }

        foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            AddTag(tag);
        }
    }

    /// <summary>
    /// 添加自定义数据项。
    /// </summary>
    /// <param name="key">键(使用点分隔命名空间,如 "app.feature")</param>
    /// <param name="value">值</param>
    public void AddCustomData(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _customData[key] = value;
        _logger.LogDebug("Added custom data: {Key} = {Value}", key, value);
    }

    /// <summary>
    /// 添加批量处理元数据。
    /// </summary>
    /// <param name="batchId">批次 ID</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="fileIndex">文件索引</param>
    public void AddBatchMetadata(string batchId, int batchSize, int fileIndex)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            throw new ArgumentException("Batch ID cannot be null or empty", nameof(batchId));
        }

        _customData["batch.id"] = batchId;
        _customData["batch.size"] = batchSize;
        _customData["batch.index"] = fileIndex;

        _logger.LogInformation("Added batch metadata: {BatchId} ({Index}/{Size})", batchId, fileIndex, batchSize);
    }

    /// <summary>
    /// 添加性能指标。
    /// </summary>
    /// <param name="durationMs">转换耗时(毫秒)</param>
    /// <param name="peakMemoryMB">峰值内存(MB)</param>
    public void AddPerformanceMetrics(long durationMs, double peakMemoryMB)
    {
        _customData["performance.duration_ms"] = durationMs;
        _customData["performance.peak_memory_mb"] = peakMemoryMB;

        _logger.LogInformation("Added performance metrics: {Duration}ms, {Memory}MB", durationMs, peakMemoryMB);
    }

    /// <summary>
    /// 生成 CustomData JSON。
    /// </summary>
    /// <returns>JSON 字符串</returns>
    public string GenerateCustomDataJson()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_customData, options);
            _logger.LogDebug("Generated CustomData JSON ({Length} chars)", json.Length);

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CustomData JSON");
            throw;
        }
    }

    /// <summary>
    /// 获取所有标签。
    /// </summary>
    public IReadOnlyList<string> GetTags()
    {
        return _tags.AsReadOnly();
    }

    /// <summary>
    /// 获取所有自定义数据键。
    /// </summary>
    public IReadOnlyList<string> GetCustomDataKeys()
    {
        return _customData.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 清除所有元数据。
    /// </summary>
    public void Clear()
    {
        _logger.LogInformation("Clearing all metadata (CustomData: {Count}, Tags: {TagCount})",
            _customData.Count, _tags.Count);

        _customData.Clear();
        _tags.Clear();
    }

    /// <summary>
    /// 生成转换报告摘要。
    /// </summary>
    public ConversionSummary GenerateSummary()
    {
        return new ConversionSummary
        {
            SourcePath = _customData.GetValueOrDefault("conversion.source")?.ToString(),
            Timestamp = _customData.GetValueOrDefault("conversion.timestamp")?.ToString(),
            Version = _customData.GetValueOrDefault("conversion.version")?.ToString(),
            CustomDataCount = _customData.Count,
            TagCount = _tags.Count,
            HasErrors = _customData.ContainsKey("conversion.errors")
        };
    }

    /// <summary>
    /// 获取转换器版本。
    /// </summary>
    private string GetConverterVersion()
    {
        var assembly = typeof(MetadataService).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }
}

/// <summary>
/// 转换摘要。
/// </summary>
public sealed class ConversionSummary
{
    /// <summary>
    /// 源文件路径。
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// 转换时间戳。
    /// </summary>
    public string? Timestamp { get; set; }

    /// <summary>
    /// 转换器版本。
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 自定义数据项数量。
    /// </summary>
    public int CustomDataCount { get; set; }

    /// <summary>
    /// 标签数量。
    /// </summary>
    public int TagCount { get; set; }

    /// <summary>
    /// 是否包含错误记录。
    /// </summary>
    public bool HasErrors { get; set; }
}

using Microsoft.Extensions.Logging;
using OfdrwNet.Converter.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OfdrwNet.Converter.Scripting;

/// <summary>
/// QuickJS 快照执行器（可选）。
/// </summary>
/// <remarks>
/// 在沙箱环境中执行 JavaScript 脚本快照。
/// 用于预执行表单计算脚本，固化结果。
/// FR-20: 可选 JavaScript 预执行
///
/// 当前为占位实现，记录脚本但不实际执行。
/// 实际部署需要集成 QuickJS.NET 或类似运行时。
/// </remarks>
public sealed class QuickJsSnapshotExecutor
{
    private readonly ILogger<QuickJsSnapshotExecutor> _logger;
    private readonly bool _isEnabled;

    /// <summary>
    /// 初始化 QuickJsSnapshotExecutor 实例。
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="isEnabled">是否启用（默认 false）</param>
    public QuickJsSnapshotExecutor(ILogger<QuickJsSnapshotExecutor> logger, bool isEnabled = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isEnabled = isEnabled;

        if (!_isEnabled)
        {
            _logger.LogInformation("QuickJS snapshot executor is disabled (placeholder mode)");
        }
    }

    /// <summary>
    /// 执行 JavaScript 脚本并返回结果。
    /// </summary>
    /// <param name="scripts">要执行的脚本列表</param>
    /// <returns>执行结果</returns>
    public ExecutionResult ExecuteScripts(IList<JsScriptInfo> scripts)
    {
        if (scripts == null)
        {
            throw new ArgumentNullException(nameof(scripts));
        }

        var result = new ExecutionResult
        {
            ExecutedScripts = new List<ScriptExecutionInfo>(),
            IsEnabled = _isEnabled
        };

        if (!_isEnabled)
        {
            _logger.LogWarning(
                "QuickJS executor is disabled, skipping {Count} scripts (use --run-js-snapshot to enable)",
                scripts.Count);
            return result;
        }

        _logger.LogInformation("Executing {Count} JavaScript scripts in sandbox", scripts.Count);

        foreach (var script in scripts)
        {
            try
            {
                var execInfo = ExecuteScript(script);
                result.ExecutedScripts.Add(execInfo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to execute script (ObjectId: {ObjectId})", script.ObjectId);

                result.ExecutedScripts.Add(new ScriptExecutionInfo
                {
                    ObjectId = script.ObjectId,
                    ScriptHash = script.Sha256,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        var successCount = result.ExecutedScripts.Count(e => e.Success);
        _logger.LogInformation(
            "Script execution complete: {Success}/{Total} succeeded",
            successCount, result.ExecutedScripts.Count);

        return result;
    }

    /// <summary>
    /// 执行单个脚本。
    /// </summary>
    private ScriptExecutionInfo ExecuteScript(JsScriptInfo script)
    {
        var execInfo = new ScriptExecutionInfo
        {
            ObjectId = script.ObjectId,
            ScriptHash = script.Sha256,
            Success = false
        };

        try
        {
            _logger.LogDebug("Executing script: ObjectId {ObjectId} ({Length} chars)", script.ObjectId, script.Length);

            // 占位实现：模拟执行
            // 实际实现应使用 QuickJS.NET 或类似库
            execInfo.Result = SimulateExecution(script);
            execInfo.Success = true;
            execInfo.ExecutionTimeMs = 0; // 占位值

            _logger.LogDebug("Script executed successfully: ObjectId {ObjectId}", script.ObjectId);
        }
        catch (Exception ex)
        {
            execInfo.Error = ex.Message;
            _logger.LogWarning(ex, "Script execution failed: ObjectId {ObjectId}", script.ObjectId);
        }

        return execInfo;
    }

    /// <summary>
    /// 模拟脚本执行（占位实现）。
    /// </summary>
    private string? SimulateExecution(JsScriptInfo script)
    {
        // 占位实现：返回 null 表示无结果
        // 实际实现应：
        // 1. 创建 QuickJS 上下文
        // 2. 注入安全 API（受限的 Math/String/Date）
        // 3. 禁用危险 API（eval/Function/fetch/XMLHttpRequest）
        // 4. 设置超时（例如 100ms）
        // 5. 执行脚本并捕获返回值
        // 6. 释放上下文

        _logger.LogDebug(
            "Simulated execution for script ObjectId {ObjectId} (actual QuickJS integration pending)",
            script.ObjectId);

        // 简单启发式：检测计算脚本模式
        if (script.ScriptType == "FormField" && script.Snippet != null)
        {
            // 示例：检测简单算术表达式
            if (script.Snippet.Contains("+") || script.Snippet.Contains("*"))
            {
                return "0"; // 占位计算结果
            }
        }

        return null;
    }

    /// <summary>
    /// 生成脚本执行报告（JSON）。
    /// </summary>
    /// <param name="result">执行结果</param>
    /// <param name="outputPath">输出文件路径</param>
    public void GenerateReport(ExecutionResult result, string outputPath)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        }

        try
        {
            var report = new ScriptExecutionReport
            {
                IsEnabled = result.IsEnabled,
                TotalScripts = result.ExecutedScripts.Count,
                SuccessfulScripts = result.ExecutedScripts.Count(e => e.Success),
                FailedScripts = result.ExecutedScripts.Count(e => !e.Success),
                Scripts = result.ExecutedScripts.Select(e => new ScriptReportEntry
                {
                    ObjectId = e.ObjectId,
                    ScriptHash = e.ScriptHash,
                    Success = e.Success,
                    Result = e.Result,
                    Error = e.Error,
                    ExecutionTimeMs = e.ExecutionTimeMs
                }).ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(report, options);
            File.WriteAllText(outputPath, json, System.Text.Encoding.UTF8);

            _logger.LogInformation("Script execution report written to {Path}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write script execution report to {Path}", outputPath);
            throw;
        }
    }
}

/// <summary>
/// 脚本执行结果。
/// </summary>
public sealed class ExecutionResult
{
    /// <summary>
    /// 是否启用执行。
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 已执行的脚本信息列表。
    /// </summary>
    public IList<ScriptExecutionInfo> ExecutedScripts { get; set; } = new List<ScriptExecutionInfo>();
}

/// <summary>
/// 单个脚本的执行信息。
/// </summary>
public sealed class ScriptExecutionInfo
{
    /// <summary>
    /// 脚本对象 ID。
    /// </summary>
    public int ObjectId { get; set; }

    /// <summary>
    /// 脚本哈希（SHA-256）。
    /// </summary>
    public string ScriptHash { get; set; } = string.Empty;

    /// <summary>
    /// 是否执行成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 执行结果（字符串形式）。
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// 错误信息（如果失败）。
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 执行时间（毫秒）。
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// 脚本执行报告（JSON 输出格式）。
/// </summary>
internal sealed class ScriptExecutionReport
{
    public bool IsEnabled { get; set; }
    public int TotalScripts { get; set; }
    public int SuccessfulScripts { get; set; }
    public int FailedScripts { get; set; }
    public List<ScriptReportEntry> Scripts { get; set; } = new();
}

/// <summary>
/// 脚本报告条目。
/// </summary>
internal sealed class ScriptReportEntry
{
    public int ObjectId { get; set; }
    public string ScriptHash { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
    public long ExecutionTimeMs { get; set; }
}

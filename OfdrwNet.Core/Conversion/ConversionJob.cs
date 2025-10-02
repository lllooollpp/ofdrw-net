using System;
using System.Collections.Generic;
using OfdrwNet.Core.Batch;
using OfdrwNet.Core.Compatibility;
using OfdrwNet.Core.Diagnostics;
using OfdrwNet.Core.Pages;
using OfdrwNet.Core.Versioning;

namespace OfdrwNet.Core.Conversion
{
    /// <summary>
    /// 表示一次 PDF→OFD 转换请求，可支持批量转换
    /// 对应 FR-1..3, FR-38 需求
    /// </summary>
    public class ConversionJob
    {
        /// <summary>
        /// 转换任务唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 输入文件路径（单文件）或输入目录（批量转换）
        /// </summary>
        public string InputPath { get; set; } = string.Empty;

        /// <summary>
        /// 输出目录路径
        /// </summary>
        public string OutputDir { get; set; } = string.Empty;

        /// <summary>
        /// 转换配置选项聚合
        /// </summary>
        public ConverterOptions Options { get; set; } = new();

        /// <summary>
        /// 当前转换状态
        /// </summary>
        public ConversionStatus Status { get; set; } = ConversionStatus.Draft;

        /// <summary>
        /// 任务创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 任务开始执行时间
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// 任务完成时间
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 页面处理上下文集合（转换过程中的页面级状态）
        /// </summary>
        public List<PageContext> PageContexts { get; set; } = new();

        /// <summary>
        /// 转换过程中收集的错误和警告记录
        /// </summary>
        public List<ErrorRecord> ErrorRecords { get; set; } = new();

        /// <summary>
        /// 兼容性降级行为记录
        /// </summary>
        public List<DowngradeAction> DowngradeActions { get; set; } = new();

        /// <summary>
        /// 版本链条目（如果启用版本管理）
        /// </summary>
        public List<VersionEntry> VersionEntries { get; set; } = new();

        /// <summary>
        /// 内存使用情况快照（用于性能分析和内存守护）
        /// </summary>
        public List<MemorySnapshot> MemorySnapshots { get; set; } = new();

        /// <summary>
        /// 批量转换结果汇总（批量模式下使用）
        /// </summary>
        public BatchResult? BatchResult { get; set; }

        /// <summary>
        /// 转换的总页数
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 成功转换的页数
        /// </summary>
        public int SuccessfulPages { get; set; }

        /// <summary>
        /// 计算转换进度百分比
        /// </summary>
        public double ProgressPercentage => TotalPages > 0 ? (double)SuccessfulPages / TotalPages * 100 : 0;

        /// <summary>
        /// 是否为批量转换任务
        /// </summary>
        public bool IsBatchJob => !string.IsNullOrEmpty(InputPath) &&
                                 (System.IO.Directory.Exists(InputPath) || InputPath.Contains('*'));

        /// <summary>
        /// 添加页面上下文
        /// </summary>
        public void AddPageContext(PageContext pageContext)
        {
            PageContexts.Add(pageContext);
        }

        /// <summary>
        /// 添加错误记录
        /// </summary>
        public void AddError(ErrorRecord error)
        {
            ErrorRecords.Add(error);
        }

        /// <summary>
        /// 添加降级行为记录
        /// </summary>
        public void AddDowngradeAction(DowngradeAction downgrade)
        {
            DowngradeActions.Add(downgrade);
        }

        /// <summary>
        /// 记录内存快照
        /// </summary>
        public void RecordMemorySnapshot(MemorySnapshot snapshot)
        {
            MemorySnapshots.Add(snapshot);
        }

        /// <summary>
        /// 更新转换状态并设置相应的时间戳
        /// </summary>
        public void UpdateStatus(ConversionStatus newStatus)
        {
            Status = newStatus;

            switch (newStatus)
            {
                case ConversionStatus.Running:
                    StartedAt = DateTime.UtcNow;
                    break;
                case ConversionStatus.Completed:
                case ConversionStatus.Failed:
                case ConversionStatus.PartiallyCompleted:
                    CompletedAt = DateTime.UtcNow;
                    break;
            }
        }

        /// <summary>
        /// 检查是否有致命错误
        /// </summary>
        public bool HasFatalErrors()
        {
            return ErrorRecords.Exists(e => e.Severity == ErrorSeverity.Fatal);
        }

        /// <summary>
        /// 获取转换耗时
        /// </summary>
        public TimeSpan? GetDuration()
        {
            if (StartedAt == null) return null;
            var endTime = CompletedAt ?? DateTime.UtcNow;
            return endTime - StartedAt.Value;
        }
    }

    /// <summary>
    /// 转换任务状态枚举
    /// </summary>
    public enum ConversionStatus
    {
        /// <summary>
        /// 草稿状态 - 任务已创建但未开始执行
        /// </summary>
        Draft = 0,

        /// <summary>
        /// 运行中 - 正在执行转换
        /// </summary>
        Running = 1,

        /// <summary>
        /// 已完成 - 所有页面成功转换且无致命错误
        /// </summary>
        Completed = 2,

        /// <summary>
        /// 部分完成 - 部分页面失败但继续执行
        /// </summary>
        PartiallyCompleted = 3,

        /// <summary>
        /// 失败 - 发生不可恢复的错误（配置错误、结构损坏等）
        /// </summary>
        Failed = 4
    }
}

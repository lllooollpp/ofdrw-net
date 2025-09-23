using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace OfdrwNet.Reader.Diagnostics
{
    /// <summary>
    /// 性能监控器
    /// 收集和分析渲染性能指标
    /// </summary>
    /// <summary>
    /// 性能监控器，收集和分析渲染性能指标
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        private readonly Timer _collectTimer;
        private readonly ConcurrentDictionary<string, PerformanceCounter> _counters;
        private readonly ConcurrentQueue<PerformanceEvent> _events;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        /// <summary>
        /// 性能数据变更事件
        /// </summary>
        public event EventHandler<PerformanceDataChangedEventArgs>? PerformanceDataChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="collectInterval">收集间隔（毫秒）</param>
        public PerformanceMonitor(int collectInterval = 1000)
        {
            _counters = new ConcurrentDictionary<string, PerformanceCounter>();
            _events = new ConcurrentQueue<PerformanceEvent>();

            _collectTimer = new Timer(collectInterval);
            _collectTimer.Elapsed += OnCollectTimerElapsed;
            _collectTimer.Start();

            InitializeDefaultCounters();
        }

        /// <summary>
        /// 开始性能计时
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能计时器</returns>
        public IPerformanceTimer StartTimer(string operationName)
        {
            return new PerformanceTimer(operationName, this);
        }

        /// <summary>
        /// 记录性能事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="duration">持续时间</param>
        /// <param name="metadata">附加元数据</param>
        public void RecordEvent(string eventName, TimeSpan duration, Dictionary<string, object>? metadata = null)
        {
            var perfEvent = new PerformanceEvent
            {
                EventName = eventName,
                Duration = duration,
                Timestamp = DateTime.Now,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            _events.Enqueue(perfEvent);

            // 更新计数器
            UpdateCounter(eventName, duration);

            // 限制事件队列大小
            while (_events.Count > 10000)
            {
                _events.TryDequeue(out _);
            }
        }

        /// <summary>
        /// 获取性能统计
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能统计</returns>
        public PerformanceStatistics? GetStatistics(string operationName)
        {
            if (_counters.TryGetValue(operationName, out var counter))
            {
                return counter.GetStatistics();
            }
            return null;
        }

        /// <summary>
        /// 获取所有性能统计
        /// </summary>
        /// <returns>性能统计字典</returns>
        public Dictionary<string, PerformanceStatistics> GetAllStatistics()
        {
            var result = new Dictionary<string, PerformanceStatistics>();

            foreach (var kvp in _counters)
            {
                result[kvp.Key] = kvp.Value.GetStatistics();
            }

            return result;
        }

        /// <summary>
        /// 获取系统性能指标
        /// </summary>
        /// <returns>系统性能指标</returns>
        public SystemPerformanceMetrics GetSystemMetrics()
        {
            using var process = Process.GetCurrentProcess();

            return new SystemPerformanceMetrics
            {
                CpuUsage = GetCpuUsage(),
                MemoryUsage = process.WorkingSet64,
                PrivateMemorySize = process.PrivateMemorySize64,
                GcMemory = GC.GetTotalMemory(false),
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// 重置所有统计
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                foreach (var counter in _counters.Values)
                {
                    counter.Reset();
                }

                while (_events.TryDequeue(out _)) { }
            }
        }

        /// <summary>
        /// 清理过期数据
        /// </summary>
        /// <param name="maxAge">最大保留时间</param>
        public void CleanupOldData(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.Now - maxAge;

            // 清理过期事件
            var tempEvents = new List<PerformanceEvent>();
            while (_events.TryDequeue(out var perfEvent))
            {
                if (perfEvent.Timestamp >= cutoffTime)
                {
                    tempEvents.Add(perfEvent);
                }
            }

            foreach (var perfEvent in tempEvents)
            {
                _events.Enqueue(perfEvent);
            }
        }

        /// <summary>
        /// 导出性能报告
        /// </summary>
        /// <param name="format">报告格式</param>
        /// <returns>性能报告</returns>
        public string ExportReport(ReportFormat format = ReportFormat.Text)
        {
            var stats = GetAllStatistics();
            var systemMetrics = GetSystemMetrics();

            return format switch
            {
                ReportFormat.Json => ExportJsonReport(stats, systemMetrics),
                ReportFormat.Csv => ExportCsvReport(stats, systemMetrics),
                _ => ExportTextReport(stats, systemMetrics)
            };
        }

        // 私有方法

        /// <summary>
        /// 初始化默认计数器
        /// </summary>
        private void InitializeDefaultCounters()
        {
            _counters["DocumentLoad"] = new PerformanceCounter();
            _counters["PageRender"] = new PerformanceCounter();
            _counters["ResourceLoad"] = new PerformanceCounter();
            _counters["CacheOperation"] = new PerformanceCounter();
        }

        /// <summary>
        /// 更新计数器
        /// </summary>
        private void UpdateCounter(string counterName, TimeSpan duration)
        {
            var counter = _counters.GetOrAdd(counterName, _ => new PerformanceCounter());
            counter.RecordDuration(duration);
        }

        /// <summary>
        /// 获取CPU使用率
        /// </summary>
        private double GetCpuUsage()
        {
            // 简化实现，实际应该使用PerformanceCounter
            using var process = Process.GetCurrentProcess();
            return process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount * 100;
        }

        /// <summary>
        /// 定时器事件处理
        /// </summary>
        private void OnCollectTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var systemMetrics = GetSystemMetrics();
                var allStats = GetAllStatistics();

                PerformanceDataChanged?.Invoke(this, new PerformanceDataChangedEventArgs
                {
                    SystemMetrics = systemMetrics,
                    Statistics = allStats,
                    Timestamp = DateTime.Now
                });
            }
            catch
            {
                // 忽略收集错误
            }
        }

        /// <summary>
        /// 导出文本报告
        /// </summary>
        private string ExportTextReport(Dictionary<string, PerformanceStatistics> stats, SystemPerformanceMetrics systemMetrics)
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== 性能监控报告 ===");
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            report.AppendLine("系统指标:");
            report.AppendLine($"  内存使用: {systemMetrics.MemoryUsage / 1024.0 / 1024.0:F1} MB");
            report.AppendLine($"  GC内存: {systemMetrics.GcMemory / 1024.0 / 1024.0:F1} MB");
            report.AppendLine($"  线程数: {systemMetrics.ThreadCount}");
            report.AppendLine();

            report.AppendLine("操作统计:");
            foreach (var kvp in stats)
            {
                var stat = kvp.Value;
                report.AppendLine($"  {kvp.Key}:");
                report.AppendLine($"    调用次数: {stat.CallCount}");
                report.AppendLine($"    平均时间: {stat.AverageTime.TotalMilliseconds:F1} ms");
                report.AppendLine($"    最大时间: {stat.MaxTime.TotalMilliseconds:F1} ms");
                report.AppendLine($"    最小时间: {stat.MinTime.TotalMilliseconds:F1} ms");
            }

            return report.ToString();
        }

        /// <summary>
        /// 导出JSON报告
        /// </summary>
        private string ExportJsonReport(Dictionary<string, PerformanceStatistics> stats, SystemPerformanceMetrics systemMetrics)
        {
            // 简化实现
            return System.Text.Json.JsonSerializer.Serialize(new { SystemMetrics = systemMetrics, Statistics = stats });
        }

        /// <summary>
        /// 导出CSV报告
        /// </summary>
        private string ExportCsvReport(Dictionary<string, PerformanceStatistics> stats, SystemPerformanceMetrics systemMetrics)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Operation,CallCount,AverageTime,MaxTime,MinTime");

            foreach (var kvp in stats)
            {
                var stat = kvp.Value;
                csv.AppendLine($"{kvp.Key},{stat.CallCount},{stat.AverageTime.TotalMilliseconds:F3},{stat.MaxTime.TotalMilliseconds:F3},{stat.MinTime.TotalMilliseconds:F3}");
            }

            return csv.ToString();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _collectTimer?.Stop();
                _collectTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 性能计时器接口
    /// </summary>
    public interface IPerformanceTimer : IDisposable
    {
        /// <summary>停止计时</summary>
        void Stop();

        /// <summary>经过的时间</summary>
        TimeSpan Elapsed { get; }
    }

    /// <summary>
    /// 性能计时器实现
    /// </summary>
    internal class PerformanceTimer : IPerformanceTimer
    {
        private readonly string _operationName;
        private readonly PerformanceMonitor _monitor;
        private readonly Stopwatch _stopwatch;
        private bool _disposed = false;

        public PerformanceTimer(string operationName, PerformanceMonitor monitor)
        {
            _operationName = operationName;
            _monitor = monitor;
            _stopwatch = Stopwatch.StartNew();
        }

        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public void Stop()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _monitor.RecordEvent(_operationName, _stopwatch.Elapsed);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 性能计数器
    /// </summary>
    internal class PerformanceCounter
    {
        private readonly object _lock = new object();
        private long _callCount;
        private TimeSpan _totalTime;
        private TimeSpan _minTime = TimeSpan.MaxValue;
        private TimeSpan _maxTime = TimeSpan.MinValue;

        public void RecordDuration(TimeSpan duration)
        {
            lock (_lock)
            {
                _callCount++;
                _totalTime += duration;

                if (duration < _minTime)
                    _minTime = duration;

                if (duration > _maxTime)
                    _maxTime = duration;
            }
        }

        public PerformanceStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new PerformanceStatistics
                {
                    CallCount = _callCount,
                    TotalTime = _totalTime,
                    AverageTime = _callCount > 0 ? TimeSpan.FromTicks(_totalTime.Ticks / _callCount) : TimeSpan.Zero,
                    MinTime = _callCount > 0 ? _minTime : TimeSpan.Zero,
                    MaxTime = _callCount > 0 ? _maxTime : TimeSpan.Zero
                };
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _callCount = 0;
                _totalTime = TimeSpan.Zero;
                _minTime = TimeSpan.MaxValue;
                _maxTime = TimeSpan.MinValue;
            }
        }
    }

    /// <summary>
    /// 性能事件
    /// </summary>
    public class PerformanceEvent
    {
        /// <summary>事件名称</summary>
        public string EventName { get; set; } = "";

        /// <summary>持续时间</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>时间戳</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>元数据</summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 性能统计
    /// </summary>
    public class PerformanceStatistics
    {
        /// <summary>调用次数</summary>
        public long CallCount { get; set; }

        /// <summary>总时间</summary>
        public TimeSpan TotalTime { get; set; }

        /// <summary>平均时间</summary>
        public TimeSpan AverageTime { get; set; }

        /// <summary>最小时间</summary>
        public TimeSpan MinTime { get; set; }

        /// <summary>最大时间</summary>
        public TimeSpan MaxTime { get; set; }
    }

    /// <summary>
    /// 系统性能指标
    /// </summary>
    public class SystemPerformanceMetrics
    {
        /// <summary>CPU使用率</summary>
        public double CpuUsage { get; set; }

        /// <summary>内存使用量</summary>
        public long MemoryUsage { get; set; }

        /// <summary>私有内存大小</summary>
        public long PrivateMemorySize { get; set; }

        /// <summary>GC内存</summary>
        public long GcMemory { get; set; }

        /// <summary>线程数</summary>
        public int ThreadCount { get; set; }

        /// <summary>句柄数</summary>
        public int HandleCount { get; set; }

        /// <summary>时间戳</summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 性能数据变更事件参数
    /// </summary>
    public class PerformanceDataChangedEventArgs : EventArgs
    {
        /// <summary>系统指标</summary>
        public SystemPerformanceMetrics? SystemMetrics { get; set; }

        /// <summary>性能统计</summary>
        public Dictionary<string, PerformanceStatistics>? Statistics { get; set; }

        /// <summary>时间戳</summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 报告格式枚举
    /// </summary>
    public enum ReportFormat
    {
        /// <summary>文本格式</summary>
        Text,
        /// <summary>JSON格式</summary>
        Json,
        /// <summary>CSV格式</summary>
        Csv
    }
}

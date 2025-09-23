using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using OfdrwNet.Reader.Threading;

namespace OfdrwNet.Reader.Memory
{
    /// <summary>
    /// 内存管理器
    /// 监控和优化内存使用
    /// </summary>
    public class MemoryManager : IDisposable
    {
        private readonly Timer _monitorTimer;
        private readonly List<IMemoryCleanable> _cleanableObjects;
        private readonly object _lockObject = new object();
        private readonly MemorySettings _settings;
        private bool _disposed = false;

        /// <summary>
        /// 内存压力事件
        /// </summary>
        public event EventHandler<MemoryPressureEventArgs>? MemoryPressure;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">内存设置</param>
        public MemoryManager(MemorySettings? settings = null)
        {
            _settings = settings ?? new MemorySettings();
            _cleanableObjects = new List<IMemoryCleanable>();

            _monitorTimer = new Timer(MonitorMemory, null,
                _settings.MonitorInterval, _settings.MonitorInterval);
        }

        /// <summary>
        /// 注册可清理对象
        /// </summary>
        /// <param name="cleanable">可清理对象</param>
        public void RegisterCleanable(IMemoryCleanable cleanable)
        {
            if (cleanable == null) return;

            lock (_lockObject)
            {
                if (!_cleanableObjects.Contains(cleanable))
                {
                    _cleanableObjects.Add(cleanable);
                }
            }
        }

        /// <summary>
        /// 注销可清理对象
        /// </summary>
        /// <param name="cleanable">可清理对象</param>
        /// <returns>是否注销成功</returns>
        public bool UnregisterCleanable(IMemoryCleanable cleanable)
        {
            lock (_lockObject)
            {
                return _cleanableObjects.Remove(cleanable);
            }
        }

        /// <summary>
        /// 获取当前内存使用情况
        /// </summary>
        /// <returns>内存使用信息</returns>
        public MemoryUsageInfo GetMemoryUsage()
        {
            using var process = Process.GetCurrentProcess();

            return new MemoryUsageInfo
            {
                WorkingSet = process.WorkingSet64,
                PrivateMemorySize = process.PrivateMemorySize64,
                VirtualMemorySize = process.VirtualMemorySize64,
                GcMemoryBeforeCollection = GC.GetTotalMemory(false),
                GcMemoryAfterCollection = GC.GetTotalMemory(true),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// 强制清理内存
        /// </summary>
        /// <param name="level">清理级别</param>
        /// <returns>清理任务</returns>
        public async Task<MemoryCleanupResult> ForceCleanupAsync(CleanupLevel level = CleanupLevel.Normal)
        {
            var startUsage = GetMemoryUsage();
            var result = new MemoryCleanupResult { StartUsage = startUsage };

            try
            {
                // 清理注册的对象
                await CleanupRegisteredObjectsAsync(level);

                // 根据清理级别执行不同的清理策略
                switch (level)
                {
                    case CleanupLevel.Light:
                        GC.Collect(0, GCCollectionMode.Optimized);
                        break;

                    case CleanupLevel.Normal:
                        GC.Collect(1, GCCollectionMode.Default);
                        break;

                    case CleanupLevel.Aggressive:
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        break;

                    case CleanupLevel.Emergency:
                        // 紧急清理
                        await EmergencyCleanupAsync();
                        break;
                }

                // 等待GC完成
                await Task.Delay(100);

                result.EndUsage = GetMemoryUsage();
                result.Success = true;
                result.FreedMemory = Math.Max(0, startUsage.WorkingSet - result.EndUsage.WorkingSet);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndUsage = GetMemoryUsage();
            }

            return result;
        }

        /// <summary>
        /// 检查内存压力
        /// </summary>
        /// <returns>内存压力级别</returns>
        public MemoryPressureLevel CheckMemoryPressure()
        {
            var usage = GetMemoryUsage();
            var workingSetMB = usage.WorkingSet / 1024.0 / 1024.0;

            if (workingSetMB > _settings.EmergencyThresholdMB)
                return MemoryPressureLevel.Emergency;

            if (workingSetMB > _settings.HighThresholdMB)
                return MemoryPressureLevel.High;

            if (workingSetMB > _settings.MediumThresholdMB)
                return MemoryPressureLevel.Medium;

            return MemoryPressureLevel.Low;
        }

        /// <summary>
        /// 设置内存压力阈值
        /// </summary>
        /// <param name="mediumMB">中等压力阈值（MB）</param>
        /// <param name="highMB">高压力阈值（MB）</param>
        /// <param name="emergencyMB">紧急压力阈值（MB）</param>
        public void SetPressureThresholds(double mediumMB, double highMB, double emergencyMB)
        {
            _settings.MediumThresholdMB = mediumMB;
            _settings.HighThresholdMB = highMB;
            _settings.EmergencyThresholdMB = emergencyMB;
        }

        /// <summary>
        /// 优化内存设置
        /// </summary>
        public void OptimizeMemorySettings()
        {
            // 设置GC延迟模式
            if (_settings.OptimizeForLowLatency)
            {
                GCSettings.LatencyMode = GCLatencyMode.LowLatency;
            }
            else
            {
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
            }

            // 设置服务器GC（如果支持）
            if (_settings.UseServerGC && GCSettings.IsServerGC)
            {
                // 服务器GC已启用
            }
        }

        // 私有方法

        /// <summary>
        /// 监控内存使用
        /// </summary>
        private void MonitorMemory(object? state)
        {
            if (_disposed) return;

            try
            {
                var pressureLevel = CheckMemoryPressure();

                if (pressureLevel >= MemoryPressureLevel.Medium)
                {
                    OnMemoryPressure(new MemoryPressureEventArgs
                    {
                        PressureLevel = pressureLevel,
                        MemoryUsage = GetMemoryUsage(),
                        SuggestedAction = GetSuggestedAction(pressureLevel)
                    });

                    // 自动清理（如果启用）
                    if (_settings.EnableAutoCleanup)
                    {
                        var cleanupLevel = pressureLevel switch
                        {
                            MemoryPressureLevel.Medium => CleanupLevel.Light,
                            MemoryPressureLevel.High => CleanupLevel.Normal,
                            MemoryPressureLevel.Emergency => CleanupLevel.Aggressive,
                            _ => CleanupLevel.Light
                        };

                        _ = Task.Run(() => ForceCleanupAsync(cleanupLevel));
                    }
                }
            }
            catch
            {
                // 忽略监控错误
            }
        }

        /// <summary>
        /// 清理注册的对象
        /// </summary>
        private async Task CleanupRegisteredObjectsAsync(CleanupLevel level)
        {
            var tasks = new List<Task>();

            lock (_lockObject)
            {
                foreach (var cleanable in _cleanableObjects)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            cleanable.Cleanup(level);
                        }
                        catch
                        {
                            // 忽略清理错误
                        }
                    }));
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// 紧急清理
        /// </summary>
        private async Task EmergencyCleanupAsync()
        {
            // 清理所有注册对象
            await CleanupRegisteredObjectsAsync(CleanupLevel.Emergency);

            // 强制垃圾回收
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                await Task.Delay(100);
            }

            // 压缩大对象堆
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        /// <summary>
        /// 获取建议操作
        /// </summary>
        private string GetSuggestedAction(MemoryPressureLevel level)
        {
            return level switch
            {
                MemoryPressureLevel.Medium => "建议清理缓存",
                MemoryPressureLevel.High => "建议减少内存使用",
                MemoryPressureLevel.Emergency => "紧急清理内存",
                _ => "无需操作"
            };
        }

        /// <summary>
        /// 触发内存压力事件
        /// </summary>
        private void OnMemoryPressure(MemoryPressureEventArgs args)
        {
            MemoryPressure?.Invoke(this, args);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _monitorTimer?.Dispose();

                lock (_lockObject)
                {
                    _cleanableObjects.Clear();
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 可清理对象接口
    /// </summary>
    public interface IMemoryCleanable
    {
        /// <summary>
        /// 清理内存
        /// </summary>
        /// <param name="level">清理级别</param>
        void Cleanup(CleanupLevel level);

        /// <summary>
        /// 获取内存使用量（字节）
        /// </summary>
        /// <returns>内存使用量</returns>
        long GetMemoryUsage();
    }

    /// <summary>
    /// 内存设置
    /// </summary>
    public class MemorySettings
    {
        /// <summary>监控间隔（毫秒）</summary>
        public int MonitorInterval { get; set; } = 5000;

        /// <summary>中等压力阈值（MB）</summary>
        public double MediumThresholdMB { get; set; } = 200;

        /// <summary>高压力阈值（MB）</summary>
        public double HighThresholdMB { get; set; } = 400;

        /// <summary>紧急压力阈值（MB）</summary>
        public double EmergencyThresholdMB { get; set; } = 600;

        /// <summary>是否启用自动清理</summary>
        public bool EnableAutoCleanup { get; set; } = true;

        /// <summary>是否优化低延迟</summary>
        public bool OptimizeForLowLatency { get; set; } = false;

        /// <summary>是否使用服务器GC</summary>
        public bool UseServerGC { get; set; } = false;
    }

    /// <summary>
    /// 内存使用信息
    /// </summary>
    public class MemoryUsageInfo
    {
        /// <summary>工作集</summary>
        public long WorkingSet { get; set; }

        /// <summary>私有内存大小</summary>
        public long PrivateMemorySize { get; set; }

        /// <summary>虚拟内存大小</summary>
        public long VirtualMemorySize { get; set; }

        /// <summary>GC内存（清理前）</summary>
        public long GcMemoryBeforeCollection { get; set; }

        /// <summary>GC内存（清理后）</summary>
        public long GcMemoryAfterCollection { get; set; }

        /// <summary>Gen0回收次数</summary>
        public int Gen0Collections { get; set; }

        /// <summary>Gen1回收次数</summary>
        public int Gen1Collections { get; set; }

        /// <summary>Gen2回收次数</summary>
        public int Gen2Collections { get; set; }

        /// <summary>时间戳</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 获取内存摘要（MB）
        /// </summary>
        public string GetSummaryMB()
        {
            return $"工作集: {WorkingSet / 1024.0 / 1024.0:F1}MB, " +
                   $"私有内存: {PrivateMemorySize / 1024.0 / 1024.0:F1}MB, " +
                   $"GC内存: {GcMemoryAfterCollection / 1024.0 / 1024.0:F1}MB";
        }
    }

    /// <summary>
    /// 内存清理结果
    /// </summary>
    public class MemoryCleanupResult
    {
        /// <summary>是否成功</summary>
        public bool Success { get; set; }

        /// <summary>开始使用情况</summary>
        public MemoryUsageInfo? StartUsage { get; set; }

        /// <summary>结束使用情况</summary>
        public MemoryUsageInfo? EndUsage { get; set; }

        /// <summary>释放的内存（字节）</summary>
        public long FreedMemory { get; set; }

        /// <summary>错误消息</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>清理耗时</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 获取清理摘要
        /// </summary>
        public string GetSummary()
        {
            if (!Success)
                return $"清理失败: {ErrorMessage}";

            var freedMB = FreedMemory / 1024.0 / 1024.0;
            return $"成功释放 {freedMB:F1}MB 内存，耗时 {Duration.TotalMilliseconds:F1}ms";
        }
    }

    /// <summary>
    /// 内存压力事件参数
    /// </summary>
    public class MemoryPressureEventArgs : EventArgs
    {
        /// <summary>压力级别</summary>
        public MemoryPressureLevel PressureLevel { get; set; }

        /// <summary>内存使用情况</summary>
        public MemoryUsageInfo? MemoryUsage { get; set; }

        /// <summary>建议操作</summary>
        public string SuggestedAction { get; set; } = "";
    }

    /// <summary>
    /// 清理级别枚举
    /// </summary>
    public enum CleanupLevel
    {
        /// <summary>轻度清理</summary>
        Light,
        /// <summary>正常清理</summary>
        Normal,
        /// <summary>激进清理</summary>
        Aggressive,
        /// <summary>紧急清理</summary>
        Emergency
    }

    /// <summary>
    /// 内存压力级别枚举
    /// </summary>
    public enum MemoryPressureLevel
    {
        /// <summary>低压力</summary>
        Low,
        /// <summary>中等压力</summary>
        Medium,
        /// <summary>高压力</summary>
        High,
        /// <summary>紧急压力</summary>
        Emergency
    }
}

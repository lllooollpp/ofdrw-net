using System;
using System.Collections.Generic;
using System.Text.Json;
using OfdrwNet.Reader.Model;
using OfdrwNet.Reader.Rendering;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 文档查看器配置管理器
    /// 管理查看器的各种配置选项
    /// </summary>
    public class DocumentViewerConfiguration : IDocumentViewerConfiguration
    {
        private readonly Dictionary<string, object> _settings;
        private readonly List<IConfigurationObserver> _observers;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DocumentViewerConfiguration()
        {
            _settings = new Dictionary<string, object>();
            _observers = new List<IConfigurationObserver>();
            InitializeDefaultSettings();
        }

        /// <summary>
        /// 默认渲染上下文
        /// </summary>
        public RenderContext DefaultRenderContext { get; set; } = RenderContext.CreateDefault();

        /// <summary>
        /// 缩放因子
        /// </summary>
        public double ZoomFactor
        {
            get => GetSetting<double>("ZoomFactor", 1.0);
            set => SetSetting("ZoomFactor", value);
        }

        /// <summary>
        /// 是否启用缓存
        /// </summary>
        public bool EnableCaching
        {
            get => GetSetting<bool>("EnableCaching", true);
            set => SetSetting("EnableCaching", value);
        }

        /// <summary>
        /// 缓存大小限制（MB）
        /// </summary>
        public int CacheSizeLimit
        {
            get => GetSetting<int>("CacheSizeLimit", 100);
            set => SetSetting("CacheSizeLimit", value);
        }

        /// <summary>
        /// 是否启用预加载
        /// </summary>
        public bool EnablePreloading
        {
            get => GetSetting<bool>("EnablePreloading", true);
            set => SetSetting("EnablePreloading", value);
        }

        /// <summary>
        /// 预加载页面数量
        /// </summary>
        public int PreloadPageCount
        {
            get => GetSetting<int>("PreloadPageCount", 3);
            set => SetSetting("PreloadPageCount", Math.Max(0, value));
        }

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public bool EnablePerformanceMonitoring
        {
            get => GetSetting<bool>("EnablePerformanceMonitoring", false);
            set => SetSetting("EnablePerformanceMonitoring", value);
        }

        /// <summary>
        /// 最大并发任务数
        /// </summary>
        public int MaxConcurrentTasks
        {
            get => GetSetting<int>("MaxConcurrentTasks", Environment.ProcessorCount);
            set => SetSetting("MaxConcurrentTasks", Math.Max(1, value));
        }

        /// <summary>
        /// 渲染质量
        /// </summary>
        public RenderQuality RenderQuality
        {
            get => GetSetting<RenderQuality>("RenderQuality", RenderQuality.Medium);
            set => SetSetting("RenderQuality", value);
        }

        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public T GetSetting<T>(string key, T defaultValue = default!)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is T typedValue)
                        return typedValue;

                    // 尝试类型转换
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        public void SetSetting(string key, object value)
        {
            var oldValue = _settings.TryGetValue(key, out var existing) ? existing : null;
            _settings[key] = value;

            // 通知观察者
            NotifyConfigurationChanged(key, oldValue, value);
        }

        /// <summary>
        /// 移除配置
        /// </summary>
        /// <param name="key">配置键</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveSetting(string key)
        {
            if (_settings.ContainsKey(key))
            {
                var oldValue = _settings[key];
                _settings.Remove(key);
                NotifyConfigurationChanged(key, oldValue, null);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取所有配置键
        /// </summary>
        /// <returns>配置键集合</returns>
        public IEnumerable<string> GetAllKeys()
        {
            return _settings.Keys;
        }

        /// <summary>
        /// 添加配置观察者
        /// </summary>
        /// <param name="observer">观察者</param>
        public void AddObserver(IConfigurationObserver observer)
        {
            if (observer != null && !_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        /// <summary>
        /// 移除配置观察者
        /// </summary>
        /// <param name="observer">观察者</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveObserver(IConfigurationObserver observer)
        {
            return _observers.Remove(observer);
        }

        /// <summary>
        /// 应用渲染配置
        /// </summary>
        /// <param name="renderConfig">渲染配置</param>
        public void ApplyRenderConfiguration(RenderConfiguration renderConfig)
        {
            if (renderConfig == null)
                return;

            // 更新渲染相关设置
            RenderQuality = renderConfig.Quality;
            EnableCaching = renderConfig.EnableCaching;
            CacheSizeLimit = renderConfig.CacheSize;
            MaxConcurrentTasks = renderConfig.MaxParallelTasks;
            EnablePerformanceMonitoring = renderConfig.EnableProgressReporting;

            // 应用到默认渲染上下文
            renderConfig.ApplyTo(DefaultRenderContext);

            NotifyConfigurationApplied(renderConfig);
        }

        /// <summary>
        /// 导出配置为JSON
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ExportToJson()
        {
            var exportData = new Dictionary<string, object>(_settings);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(exportData, options);
        }

        /// <summary>
        /// 从JSON导入配置
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>是否导入成功</returns>
        public bool ImportFromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var importData = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
                if (importData != null)
                {
                    foreach (var kvp in importData)
                    {
                        SetSetting(kvp.Key, kvp.Value);
                    }
                    return true;
                }
            }
            catch
            {
                // 忽略导入错误
            }

            return false;
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void Reset()
        {
            _settings.Clear();
            InitializeDefaultSettings();
            DefaultRenderContext = RenderContext.CreateDefault();

            // 通知重置
            NotifyConfigurationReset();
        }

        /// <summary>
        /// 克隆配置
        /// </summary>
        /// <returns>配置副本</returns>
        public DocumentViewerConfiguration Clone()
        {
            var cloned = new DocumentViewerConfiguration();

            foreach (var kvp in _settings)
            {
                cloned._settings[kvp.Key] = kvp.Value;
            }

            cloned.DefaultRenderContext = DefaultRenderContext.Clone();

            return cloned;
        }

        // 私有方法

        /// <summary>
        /// 初始化默认设置
        /// </summary>
        private void InitializeDefaultSettings()
        {
            _settings["ZoomFactor"] = 1.0;
            _settings["EnableCaching"] = true;
            _settings["CacheSizeLimit"] = 100;
            _settings["EnablePreloading"] = true;
            _settings["PreloadPageCount"] = 3;
            _settings["EnablePerformanceMonitoring"] = false;
            _settings["MaxConcurrentTasks"] = Environment.ProcessorCount;
            _settings["RenderQuality"] = RenderQuality.Medium;
        }

        /// <summary>
        /// 通知配置改变
        /// </summary>
        private void NotifyConfigurationChanged(string key, object? oldValue, object? newValue)
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnConfigurationChanged(key, oldValue, newValue);
                }
                catch
                {
                    // 忽略观察者错误
                }
            }
        }

        /// <summary>
        /// 通知配置应用
        /// </summary>
        private void NotifyConfigurationApplied(RenderConfiguration renderConfig)
        {
            var args = new ConfigurationAppliedEventArgs
            {
                AppliedConfiguration = renderConfig,
                Timestamp = DateTime.Now
            };

            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnConfigurationApplied(args);
                }
                catch
                {
                    // 忽略观察者错误
                }
            }
        }

        /// <summary>
        /// 通知配置重置
        /// </summary>
        private void NotifyConfigurationReset()
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnConfigurationReset();
                }
                catch
                {
                    // 忽略观察者错误
                }
            }
        }
    }

    /// <summary>
    /// 配置观察者接口
    /// </summary>
    public interface IConfigurationObserver
    {
        /// <summary>
        /// 配置改变时调用
        /// </summary>
        void OnConfigurationChanged(string key, object? oldValue, object? newValue);

        /// <summary>
        /// 配置应用时调用
        /// </summary>
        void OnConfigurationApplied(ConfigurationAppliedEventArgs args);

        /// <summary>
        /// 配置重置时调用
        /// </summary>
        void OnConfigurationReset();
    }

    /// <summary>
    /// 配置应用事件参数
    /// </summary>
    public class ConfigurationAppliedEventArgs : EventArgs
    {
        /// <summary>应用的配置</summary>
        public RenderConfiguration? AppliedConfiguration { get; set; }

        /// <summary>应用时间</summary>
        public DateTime Timestamp { get; set; }
    }
}

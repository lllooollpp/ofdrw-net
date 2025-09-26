using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OfdrwNet.Reader.Rendering
{
    /// <summary>
    /// 渲染配置管理�?
    /// 管理渲染参数、质量设置和性能选项
    /// </summary>
    public class RenderingConfigurationManager
    {
        private readonly Dictionary<string, RenderProfile> _profiles;
        private RenderProfile _currentProfile;

        /// <summary>
        /// 当前渲染配置
        /// </summary>
        public RenderingConfiguration CurrentConfiguration => _currentProfile.Configuration;

        /// <summary>
        /// 当前配置文件名称
        /// </summary>
        public string CurrentProfileName => _currentProfile.Name;

        /// <summary>
        /// 可用配置文件列表
        /// </summary>
        public IEnumerable<string> AvailableProfiles => _profiles.Keys;

        /// <summary>
        /// 构造函�?
        /// </summary>
        public RenderingConfigurationManager()
        {
            _profiles = new Dictionary<string, RenderProfile>();
            InitializeDefaultProfiles();
            _currentProfile = _profiles["Default"];
        }

        /// <summary>
        /// 设置当前配置文件
        /// </summary>
        /// <param name="profileName">配置文件名称</param>
        /// <returns>是否设置成功</returns>
        public bool SetProfile(string profileName)
        {
            if (_profiles.TryGetValue(profileName, out var profile))
            {
                _currentProfile = profile;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加自定义配置文�?
        /// </summary>
        /// <param name="name">配置文件名称</param>
        /// <param name="configuration">渲染配置</param>
        /// <param name="description">描述</param>
        public void AddProfile(string name, RenderingConfiguration configuration, string description = "")
        {
            _profiles[name] = new RenderProfile
            {
                Name = name,
                Description = description,
                Configuration = configuration
            };
        }

        /// <summary>
        /// 移除配置文件
        /// </summary>
        /// <param name="profileName">配置文件名称</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveProfile(string profileName)
        {
            if (profileName == "Default" || profileName == "HighQuality" || profileName == "Fast")
            {
                return false; // 不能删除内置配置文件
            }

            return _profiles.Remove(profileName);
        }

        /// <summary>
        /// 获取配置文件
        /// </summary>
        /// <param name="profileName">配置文件名称</param>
        /// <returns>配置文件，如果不存在则返回null</returns>
        public RenderProfile? GetProfile(string profileName)
        {
            return _profiles.TryGetValue(profileName, out var profile) ? profile : null;
        }

        /// <summary>
        /// 克隆当前配置
        /// </summary>
        /// <returns>配置副本</returns>
        public RenderingConfiguration CloneCurrentConfiguration()
        {
            return _currentProfile.Configuration.Clone();
        }

        /// <summary>
        /// 更新当前配置的特定属�?
        /// </summary>
        /// <param name="updater">配置更新委托</param>
        public void UpdateCurrentConfiguration(Action<RenderingConfiguration> updater)
        {
            updater?.Invoke(_currentProfile.Configuration);
        }

        /// <summary>
        /// 保存配置文件到JSON
        /// </summary>
        /// <param name="profileName">配置文件名称</param>
        /// <returns>JSON字符�?/returns>
        public string SaveProfileToJson(string profileName)
        {
            if (!_profiles.TryGetValue(profileName, out var profile))
                throw new ArgumentException($"配置文件不存�? {profileName}");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(profile, options);
        }

        /// <summary>
        /// 从JSON加载配置文件
        /// </summary>
        /// <param name="json">JSON字符�?/param>
        /// <param name="profileName">配置文件名称</param>
        /// <returns>是否加载成功</returns>
        public bool LoadProfileFromJson(string json, string profileName)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var profile = JsonSerializer.Deserialize<RenderProfile>(json, options);
                if (profile != null)
                {
                    profile.Name = profileName;
                    _profiles[profileName] = profile;
                    return true;
                }
            }
            catch
            {
                // 忽略反序列化错误
            }

            return false;
        }

        /// <summary>
        /// 重置为默认配�?
        /// </summary>
        public void ResetToDefault()
        {
            _currentProfile = _profiles["Default"];
        }

        // 私有方法

        /// <summary>
        /// 初始化默认配置文�?
        /// </summary>
        private void InitializeDefaultProfiles()
        {
            // 默认配置
            _profiles["Default"] = new RenderProfile
            {
                Name = "Default",
                Description = "平衡的质量和性能设置",
                Configuration = new RenderingConfiguration
                {
                    Quality = RenderQualityLevel.Medium,
                    SmoothingMode = SmoothingMode.HighQuality,
                    TextRenderingHint = TextRenderingHint.AntiAlias,
                    InterpolationMode = InterpolationMode.HighQualityBilinear,
                    CompositingQuality = CompositingQuality.HighQuality,
                    ImageQuality = ImageQuality.Medium,
                    EnableCaching = true,
                    CacheSize = 100,
                    EnableParallelRendering = true,
                    MaxParallelTasks = Environment.ProcessorCount,
                    EnableProgressReporting = true,
                    MemoryOptimization = MemoryOptimizationLevel.Balanced
                }
            };

            // 高质量配�?
            _profiles["HighQuality"] = new RenderProfile
            {
                Name = "HighQuality",
                Description = "最高质量渲染设置",
                Configuration = new RenderingConfiguration
                {
                    Quality = RenderQualityLevel.High,
                    SmoothingMode = SmoothingMode.HighQuality,
                    TextRenderingHint = TextRenderingHint.ClearTypeGridFit,
                    InterpolationMode = InterpolationMode.HighQualityBicubic,
                    CompositingQuality = CompositingQuality.HighQuality,
                    ImageQuality = ImageQuality.High,
                    EnableCaching = true,
                    CacheSize = 200,
                    EnableParallelRendering = true,
                    MaxParallelTasks = Environment.ProcessorCount,
                    EnableProgressReporting = true,
                    MemoryOptimization = MemoryOptimizationLevel.Quality
                }
            };

            // 快速配�?
            _profiles["Fast"] = new RenderProfile
            {
                Name = "Fast",
                Description = "快速渲染设置，优先考虑性能",
                Configuration = new RenderingConfiguration
                {
                    Quality = RenderQualityLevel.Low,
                    SmoothingMode = SmoothingMode.HighSpeed,
                    TextRenderingHint = TextRenderingHint.SingleBitPerPixel,
                    InterpolationMode = InterpolationMode.Low,
                    CompositingQuality = CompositingQuality.HighSpeed,
                    ImageQuality = ImageQuality.Low,
                    EnableCaching = true,
                    CacheSize = 50,
                    EnableParallelRendering = true,
                    MaxParallelTasks = Environment.ProcessorCount * 2,
                    EnableProgressReporting = false,
                    MemoryOptimization = MemoryOptimizationLevel.Memory
                }
            };

            // 打印配置
            _profiles["Print"] = new RenderProfile
            {
                Name = "Print",
                Description = "打印优化配置",
                Configuration = new RenderingConfiguration
                {
                    Quality = RenderQualityLevel.High,
                    SmoothingMode = SmoothingMode.HighQuality,
                    TextRenderingHint = TextRenderingHint.ClearTypeGridFit,
                    InterpolationMode = InterpolationMode.HighQualityBicubic,
                    CompositingQuality = CompositingQuality.HighQuality,
                    ImageQuality = ImageQuality.High,
                    EnableCaching = false, // 打印时不缓存
                    CacheSize = 0,
                    EnableParallelRendering = false, // 打印时使用单线程
                    MaxParallelTasks = 1,
                    EnableProgressReporting = true,
                    MemoryOptimization = MemoryOptimizationLevel.Quality
                }
            };
        }
    }

    /// <summary>
    /// 渲染配置文件
    /// </summary>
    public class RenderProfile
    {
        /// <summary>配置文件名称</summary>
        public string Name { get; set; } = "";

        /// <summary>配置文件描述</summary>
        public string Description { get; set; } = "";

        /// <summary>渲染配置</summary>
        public RenderingConfiguration Configuration { get; set; } = new RenderingConfiguration();

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>最后修改时�?/summary>
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 渲染配置
    /// </summary>
    public class RenderingConfiguration
    {
        /// <summary>整体渲染质量</summary>
        public RenderQualityLevel Quality { get; set; } = RenderQualityLevel.Medium;

        /// <summary>平滑模式</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.HighQuality;

        /// <summary>文本渲染提示</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.AntiAlias;

        /// <summary>插值模�?/summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.HighQualityBilinear;

        /// <summary>合成质量</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.HighQuality;

        /// <summary>图像质量</summary>
        public ImageQuality ImageQuality { get; set; } = ImageQuality.Medium;

        /// <summary>是否启用缓存</summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>缓存大小（MB�?/summary>
        public int CacheSize { get; set; } = 100;

        /// <summary>是否启用并行渲染</summary>
        public bool EnableParallelRendering { get; set; } = true;

        /// <summary>最大并行任务数</summary>
        public int MaxParallelTasks { get; set; } = Environment.ProcessorCount;

        /// <summary>是否启用进度报告</summary>
        public bool EnableProgressReporting { get; set; } = true;

        /// <summary>内存优化级别</summary>
        public MemoryOptimizationLevel MemoryOptimization { get; set; } = MemoryOptimizationLevel.Balanced;

        /// <summary>自定义属�?/summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 克隆配置
        /// </summary>
        /// <returns>配置副本</returns>
        public RenderingConfiguration Clone()
        {
            return new RenderingConfiguration
            {
                Quality = Quality,
                SmoothingMode = SmoothingMode,
                TextRenderingHint = TextRenderingHint,
                InterpolationMode = InterpolationMode,
                CompositingQuality = CompositingQuality,
                ImageQuality = ImageQuality,
                EnableCaching = EnableCaching,
                CacheSize = CacheSize,
                EnableParallelRendering = EnableParallelRendering,
                MaxParallelTasks = MaxParallelTasks,
                EnableProgressReporting = EnableProgressReporting,
                MemoryOptimization = MemoryOptimization,
                CustomProperties = new Dictionary<string, object>(CustomProperties)
            };
        }

        /// <summary>
        /// 应用到渲染上下文
        /// </summary>
        /// <param name="context">渲染上下�?/param>
        public void ApplyTo(Model.RenderContext context)
        {
            context.SmoothingMode = SmoothingMode;
            context.TextRenderingHint = TextRenderingHint;
            context.InterpolationMode = InterpolationMode;
            context.ImageInterpolationMode = InterpolationMode;
            context.CompositingQuality = CompositingQuality;
            context.ImageQuality = ImageQuality;

            // 设置自定义属�?
            foreach (var property in CustomProperties)
            {
                context.Properties[property.Key] = property.Value;
            }
        }
    }

    /// <summary>
    /// 整体渲染质量枚举
    /// </summary>
    public enum RenderQualityLevel
    {
        /// <summary>草图质量</summary>
        Draft,
        /// <summary>低质�?/summary>
        Low,
        /// <summary>中等质量</summary>
        Medium,
        /// <summary>高质�?/summary>
        High,
        /// <summary>最高质�?/summary>
        Highest
    }

    /// <summary>
    /// 内存优化级别枚举
    /// </summary>
    public enum MemoryOptimizationLevel
    {
        /// <summary>优先内存使用</summary>
        Memory,
        /// <summary>平衡内存和质�?/summary>
        Balanced,
        /// <summary>优先质量</summary>
        Quality
    }
}

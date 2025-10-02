using System;
using System.Collections.Generic;
using System.Linq;
using OfdrwNet.Core.Forms;
using OfdrwNet.Core.Recognition;
using OfdrwNet.Core.Resources;
using OfdrwNet.Core.Scripting;

namespace OfdrwNet.Core.Pages
{
    /// <summary>
    /// 单页处理上下文，记录页面级别的转换状态和资源使用情况
    /// 对应 FR-10..15 需求，用于页面转换过程中的状态跟踪
    /// </summary>
    public class PageContext
    {
        /// <summary>
        /// 页码（从1开始）
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 所属转换任务的唯一标识。
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// 页面的零基索引（PageNumber - 1）。
        /// </summary>
        public int ZeroBasedIndex => PageNumber > 0 ? PageNumber - 1 : PageNumber;

        /// <summary>
        /// 页面状态
        /// </summary>
        public PageStatus Status { get; set; } = PageStatus.NotStarted;

        /// <summary>
        /// 源PDF页面对象的标识符或引用
        /// </summary>
        public string? SourceObjectId { get; set; }

        /// <summary>
        /// 页面开始处理时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 页面完成处理时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 提取的矢量对象列表
        /// </summary>
        public List<ExtractedVector> ExtractedVectors { get; set; } = new();

        /// <summary>
        /// 页面使用的字体资源
        /// </summary>
        public List<FontResource> FontsUsed { get; set; } = new();

        /// <summary>
        /// 页面使用的图像资源
        /// </summary>
        public List<ImageResource> ImagesUsed { get; set; } = new();

        /// <summary>
        /// 复合对象识别结果（表格和公式）
        /// </summary>
        public List<CompositeResult> CompositeResults { get; set; } = new();

        /// <summary>
        /// 表单字段（如果存在）
        /// </summary>
        public List<FormField> FormFields { get; set; } = new();

        /// <summary>
        /// JavaScript 脚本信息
        /// </summary>
        public List<JsScriptInfo> JavaScriptInfos { get; set; } = new();

        /// <summary>
        /// 多媒体资源
        /// </summary>
        public List<MediaResource> MediaResources { get; set; } = new();

        /// <summary>
        /// 页面级别的错误记录
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 页面级别的警告记录
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 页面转换的统计信息
        /// </summary>
        public PageStatistics Statistics { get; set; } = new();

        /// <summary>
        /// 页面转换的配置参数（继承自全局配置但可能有页面特定的覆盖）
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// 获取页面处理耗时
        /// </summary>
        public TimeSpan? Duration => EndTime.HasValue && StartTime.HasValue
            ? EndTime.Value - StartTime.Value
            : null;

        /// <summary>
        /// 是否处理成功
        /// </summary>
        public bool IsSuccessful => Status == PageStatus.Completed && Errors.Count == 0;

        /// <summary>
        /// 是否有警告
        /// </summary>
        public bool HasWarnings => Warnings.Count > 0;

        /// <summary>
        /// 添加错误信息
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add($"[Page {PageNumber}] {error}");
        }

        /// <summary>
        /// 添加警告信息
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add($"[Page {PageNumber}] {warning}");
        }

        /// <summary>
        /// 添加字体资源
        /// </summary>
        public void AddFont(FontResource font)
        {
            if (font is null)
            {
                throw new ArgumentNullException(nameof(font));
            }

            if (!FontsUsed.Any(f => string.Equals(f.DisplayName, font.DisplayName, StringComparison.Ordinal)))
            {
                FontsUsed.Add(font);
            }
        }

        /// <summary>
        /// 添加图像资源
        /// </summary>
        public void AddImage(ImageResource image)
        {
            if (image is null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            ImagesUsed.Add(image);
        }

        /// <summary>
        /// 添加复合对象识别结果
        /// </summary>
        public void AddCompositeResult(CompositeResult result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            CompositeResults.Add(result);
        }

        /// <summary>
        /// 开始页面处理
        /// </summary>
        public void Start()
        {
            Status = PageStatus.Processing;
            StartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 完成页面处理
        /// </summary>
        public void Complete()
        {
            Status = PageStatus.Completed;
            EndTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 标记页面处理失败
        /// </summary>
        public void Fail(string reason)
        {
            Status = PageStatus.Failed;
            EndTime = DateTime.UtcNow;
            AddError(reason);
        }
    }

    /// <summary>
    /// 页面处理状态枚举
    /// </summary>
    public enum PageStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// 处理中
        /// </summary>
        Processing = 1,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 2,

        /// <summary>
        /// 处理失败
        /// </summary>
        Failed = 3,

        /// <summary>
        /// 已跳过
        /// </summary>
        Skipped = 4
    }

    /// <summary>
    /// 页面转换统计信息
    /// </summary>
    public class PageStatistics
    {
        /// <summary>
        /// 提取的文本对象数量
        /// </summary>
        public int TextObjectCount { get; set; }

        /// <summary>
        /// 提取的图像数量
        /// </summary>
        public int ImageCount { get; set; }

        /// <summary>
        /// 提取的矢量路径数量
        /// </summary>
        public int VectorPathCount { get; set; }

        /// <summary>
        /// 识别的表格数量
        /// </summary>
        public int TableCount { get; set; }

        /// <summary>
        /// 识别的公式数量
        /// </summary>
        public int FormulaCount { get; set; }

        /// <summary>
        /// 字体嵌入数量
        /// </summary>
        public int EmbeddedFontCount { get; set; }

        /// <summary>
        /// 颜色转换次数
        /// </summary>
        public int ColorConversions { get; set; }

        /// <summary>
        /// 兼容性降级操作次数
        /// </summary>
        public int CompatibilityDowngrades { get; set; }
    }

    /// <summary>
    /// 提取的矢量对象（占位符，将在其他任务中完善）
    /// </summary>
    public class ExtractedVector
    {
        /// <summary>对象ID</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>矢量类型</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>边界框</summary>
        public string BoundingBox { get; set; } = string.Empty;
    }
}

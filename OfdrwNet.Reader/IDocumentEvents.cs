using System;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 文档事件接口，定义文档操作相关的事件
    /// </summary>
    public interface IDocumentEvents
    {
        /// <summary>
        /// 文档加载进度事件
        /// </summary>
        event EventHandler<DocumentLoadProgressEventArgs> LoadProgress;

        /// <summary>
        /// 页面渲染完成事件
        /// </summary>
        event EventHandler<PageRenderedEventArgs> PageRendered;

        /// <summary>
        /// 错误发生事件
        /// </summary>
        event EventHandler<ErrorEventArgs> ErrorOccurred;
    }

    /// <summary>
    /// 文档加载进度事件参数
    /// </summary>
    public class DocumentLoadProgressEventArgs : EventArgs
    {
        /// <summary>
        /// 当前步骤
        /// </summary>
        public int CurrentStep { get; set; }

        /// <summary>
        /// 总步骤数
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// 步骤描述
        /// </summary>
        public string StepDescription { get; set; } = string.Empty;

        /// <summary>
        /// 进度百分比
        /// </summary>
        public double ProgressPercentage => (double)CurrentStep / TotalSteps * 100;
    }

    /// <summary>
    /// 页面渲染完成事件参数
    /// </summary>
    public class PageRenderedEventArgs : EventArgs
    {
        /// <summary>
        /// 页面编号
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 渲染耗时
        /// </summary>
        public TimeSpan RenderDuration { get; set; }

        /// <summary>
        /// 对象数量
        /// </summary>
        public int ObjectCount { get; set; }

        /// <summary>
        /// 是否从缓存获取
        /// </summary>
        public bool FromCache { get; set; }
    }

    /// <summary>
    /// 错误事件参数
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 错误类型
        /// </summary>
        public ErrorType ErrorType { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 异常对象
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// 错误上下文
        /// </summary>
        public string Context { get; set; } = string.Empty;
    }

    /// <summary>
    /// 错误类型
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// 文档加载错误
        /// </summary>
        DocumentLoad,

        /// <summary>
        /// 页面渲染错误
        /// </summary>
        PageRender,

        /// <summary>
        /// 资源加载错误
        /// </summary>
        ResourceLoad,

        /// <summary>
        /// 导航错误
        /// </summary>
        Navigation,

        /// <summary>
        /// 配置错误
        /// </summary>
        Configuration,

        /// <summary>
        /// 内存错误
        /// </summary>
        Memory,

        /// <summary>
        /// 其他错误
        /// </summary>
        Other
    }
}

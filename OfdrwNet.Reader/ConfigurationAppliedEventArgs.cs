using System;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 配置应用事件参数
    /// </summary>
    public class ConfigurationAppliedEventArgs : EventArgs
    {
        /// <summary>
        /// 应用的配置
        /// </summary>
        public IDocumentViewerConfiguration? Configuration { get; set; }

        /// <summary>
        /// 应用时间
        /// </summary>
        public DateTime AppliedAt { get; set; }

        /// <summary>
        /// 配置名称
        /// </summary>
        public string ConfigurationName { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功应用
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// 错误消息（如果应用失败）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}

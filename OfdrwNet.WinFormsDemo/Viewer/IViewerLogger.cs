using System;

namespace OfdrwNet.WinFormsDemo.Viewer
{
    /// <summary>
    /// OFD查看器日志接口，定义日志记录的基本方法
    /// </summary>
    public interface IViewerLogger
    {
        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="args">格式化参数</param>
        void LogInfo(string message, params object[] args);

        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="args">格式化参数</param>
        void LogWarning(string message, params object[] args);

        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息</param>
        /// <param name="args">格式化参数</param>
        void LogError(string message, Exception? exception = null, params object[] args);

        /// <summary>
        /// 记录性能相关日志
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="durationMs">耗时（毫秒）</param>
        /// <param name="additionalData">附加数据</param>
        void LogPerformance(string operationName, double durationMs, object? additionalData = null);

        /// <summary>
        /// 记录调试级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="args">格式化参数</param>
        void LogDebug(string message, params object[] args);
    }
}

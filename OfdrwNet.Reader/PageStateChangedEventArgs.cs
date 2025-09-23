using System;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 页面状态变化事件参数
    /// </summary>
    public class PageStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 页面信息
        /// </summary>
        public PageInfo? PageInfo { get; set; }

        /// <summary>
        /// 旧状态
        /// </summary>
        public PageState OldState { get; set; }

        /// <summary>
        /// 新状态
        /// </summary>
        public PageState NewState { get; set; }

        /// <summary>
        /// 状态变化时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 状态变化原因
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 额外的上下文信息
        /// </summary>
        public object? Context { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PageStateChangedEventArgs()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pageInfo">页面信息</param>
        /// <param name="oldState">旧状态</param>
        /// <param name="newState">新状态</param>
        /// <param name="reason">变化原因</param>
        public PageStateChangedEventArgs(PageInfo pageInfo, PageState oldState, PageState newState, string? reason = null)
        {
            PageInfo = pageInfo;
            OldState = oldState;
            NewState = newState;
            Reason = reason;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 获取描述字符串
        /// </summary>
        /// <returns>描述字符串</returns>
        public override string ToString()
        {
            var pageDesc = PageInfo != null ? $"页面{PageInfo.Index}" : "未知页面";
            var reasonDesc = !string.IsNullOrEmpty(Reason) ? $" ({Reason})" : "";
            return $"{pageDesc}: {OldState} -> {NewState}{reasonDesc} [{Timestamp:HH:mm:ss.fff}]";
        }
    }
}

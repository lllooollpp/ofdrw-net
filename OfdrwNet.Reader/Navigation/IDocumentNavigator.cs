using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader.Navigation
{
    /// <summary>
    /// 文档导航器接口，管理文档浏览和页面导航
    /// </summary>
    public interface IDocumentNavigator
    {
        /// <summary>
        /// 当前导航状态
        /// </summary>
        NavigationState State { get; }

        /// <summary>
        /// 跳转到指定页面
        /// </summary>
        /// <param name="pageNumber">页码 (1-based)</param>
        /// <param name="animationType">跳转动画类型</param>
        /// <returns>操作结果</returns>
        Task<NavigationResult> GoToPageAsync(int pageNumber, AnimationType animationType = AnimationType.None);

        /// <summary>
        /// 导航到下一页
        /// </summary>
        /// <returns>操作结果</returns>
        Task<NavigationResult> NextPageAsync();

        /// <summary>
        /// 导航到上一页
        /// </summary>
        /// <returns>操作结果</returns>
        Task<NavigationResult> PreviousPageAsync();

        /// <summary>
        /// 跳转到首页
        /// </summary>
        /// <returns>操作结果</returns>
        Task<NavigationResult> FirstPageAsync();

        /// <summary>
        /// 跳转到末页
        /// </summary>
        /// <returns>操作结果</returns>
        Task<NavigationResult> LastPageAsync();

        /// <summary>
        /// 设置缩放级别
        /// </summary>
        /// <param name="zoomLevel">缩放级别 (1.0 = 100%)</param>
        /// <param name="centerPoint">缩放中心点</param>
        /// <returns>操作结果</returns>
        Task<NavigationResult> SetZoomAsync(double zoomLevel, Point? centerPoint = null);

        /// <summary>
        /// 获取所有页面的缩略图
        /// </summary>
        /// <param name="thumbnailSize">缩略图尺寸</param>
        /// <param name="pageRange">页面范围 (null表示所有页面)</param>
        /// <returns>缩略图列表</returns>
        Task<List<PageThumbnail>> GetThumbnailsAsync(
            Size thumbnailSize,
            PageRange? pageRange = null);

        /// <summary>
        /// 导航状态变化事件
        /// </summary>
        event EventHandler<NavigationStateChangedEventArgs> StateChanged;
    }

    /// <summary>
    /// 导航结果
    /// </summary>
    public class NavigationResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 当前页面编号
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 操作耗时
        /// </summary>
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// 页面缩略图
    /// </summary>
    public class PageThumbnail
    {
        /// <summary>
        /// 页面编号
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 缩略图位图
        /// </summary>
        public Bitmap? Thumbnail { get; set; }

        /// <summary>
        /// 原始尺寸
        /// </summary>
        public Size OriginalSize { get; set; }

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// 导航状态变化事件参数
    /// </summary>
    public class NavigationStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧状态
        /// </summary>
        public NavigationState? OldState { get; set; }

        /// <summary>
        /// 新状态
        /// </summary>
        public NavigationState? NewState { get; set; }

        /// <summary>
        /// 变化类型
        /// </summary>
        public NavigationType ChangeType { get; set; }
    }

    /// <summary>
    /// 动画类型
    /// </summary>
    public enum AnimationType
    {
        /// <summary>
        /// 无动画
        /// </summary>
        None,

        /// <summary>
        /// 淡入淡出
        /// </summary>
        Fade,

        /// <summary>
        /// 滑动
        /// </summary>
        Slide,

        /// <summary>
        /// 缩放
        /// </summary>
        Zoom
    }

    /// <summary>
    /// 页面范围
    /// </summary>
    public class PageRange
    {
        /// <summary>
        /// 起始页码
        /// </summary>
        public int StartPage { get; set; }

        /// <summary>
        /// 结束页码
        /// </summary>
        public int EndPage { get; set; }

        /// <summary>
        /// 构造页面范围
        /// </summary>
        /// <param name="startPage">起始页码</param>
        /// <param name="endPage">结束页码</param>
        public PageRange(int startPage, int endPage)
        {
            StartPage = startPage;
            EndPage = endPage;
        }

        /// <summary>
        /// 所有页面范围
        /// </summary>
        public static PageRange All => new PageRange(1, int.MaxValue);

        /// <summary>
        /// 单页范围
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <returns>单页范围</returns>
        public static PageRange SinglePage(int pageNumber) => new PageRange(pageNumber, pageNumber);
    }
}

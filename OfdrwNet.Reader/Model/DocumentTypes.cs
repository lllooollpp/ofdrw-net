using System;
using System.Collections.Generic;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// 文档状态枚举
    /// </summary>
    public enum DocumentState
    {
        /// <summary>
        /// 未加载
        /// </summary>
        NotLoaded,

        /// <summary>
        /// 加载中
        /// </summary>
        Loading,

        /// <summary>
        /// 已加载
        /// </summary>
        Loaded,

        /// <summary>
        /// 错误状态
        /// </summary>
        Error,

        /// <summary>
        /// 已释放
        /// </summary>
        Disposed
    }

    /// <summary>
    /// 页面状态枚举
    /// </summary>
    public enum PageState
    {
        /// <summary>
        /// 未加载
        /// </summary>
        NotLoaded,

        /// <summary>
        /// 加载中
        /// </summary>
        Loading,

        /// <summary>
        /// 已加载
        /// </summary>
        Loaded,

        /// <summary>
        /// 渲染中
        /// </summary>
        Rendering,

        /// <summary>
        /// 已渲染
        /// </summary>
        Rendered,

        /// <summary>
        /// 错误状态
        /// </summary>
        Error,

        /// <summary>
        /// 已缓存
        /// </summary>
        Cached
    }

    /// <summary>
    /// 导航类型枚举
    /// </summary>
    public enum NavigationType
    {
        /// <summary>
        /// 跳转到指定页面
        /// </summary>
        GoToPage,

        /// <summary>
        /// 下一页
        /// </summary>
        NextPage,

        /// <summary>
        /// 上一页
        /// </summary>
        PreviousPage,

        /// <summary>
        /// 首页
        /// </summary>
        FirstPage,

        /// <summary>
        /// 末页
        /// </summary>
        LastPage,

        /// <summary>
        /// 缩放
        /// </summary>
        Zoom,

        /// <summary>
        /// 平移
        /// </summary>
        Pan
    }

    /// <summary>
    /// 资源类型枚举
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// 字体资源
        /// </summary>
        Font,

        /// <summary>
        /// 图像资源
        /// </summary>
        Image,

        /// <summary>
        /// 颜色空间资源
        /// </summary>
        ColorSpace,

        /// <summary>
        /// 矢量图形资源
        /// </summary>
        Vector,

        /// <summary>
        /// 其他类型资源
        /// </summary>
        Other
    }

    /// <summary>
    /// OFD版本枚举
    /// </summary>
    public enum OfdVersion
    {
        /// <summary>
        /// OFD 1.0 (GB/T 33190-2016)
        /// </summary>
        V1_0,

        /// <summary>
        /// OFD 1.1
        /// </summary>
        V1_1,

        /// <summary>
        /// OFD 2.0
        /// </summary>
        V2_0,

        /// <summary>
        /// 未知版本
        /// </summary>
        Unknown
    }

    /// <summary>
    /// 文档源类型枚举
    /// </summary>
    public enum DocumentSourceType
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        FilePath,

        /// <summary>
        /// 数据流
        /// </summary>
        Stream,

        /// <summary>
        /// 已解压目录
        /// </summary>
        Directory
    }

    /// <summary>
    /// 渲染错误类型枚举
    /// </summary>
    public enum RenderErrorType
    {
        /// <summary>
        /// 对象解析错误
        /// </summary>
        ObjectParseError,

        /// <summary>
        /// 资源缺失错误
        /// </summary>
        ResourceMissingError,

        /// <summary>
        /// 坐标变换错误
        /// </summary>
        TransformError,

        /// <summary>
        /// 字体渲染错误
        /// </summary>
        FontRenderError,

        /// <summary>
        /// 图像渲染错误
        /// </summary>
        ImageRenderError,

        /// <summary>
        /// 矢量图形渲染错误
        /// </summary>
        VectorRenderError,

        /// <summary>
        /// 内存不足错误
        /// </summary>
        OutOfMemoryError,

        /// <summary>
        /// 其他未知错误
        /// </summary>
        UnknownError
    }

    /// <summary>
    /// 动画类型枚举
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
        /// 起始页码 (1-based)
        /// </summary>
        public int StartPage { get; set; }

        /// <summary>
        /// 结束页码 (1-based)
        /// </summary>
        public int EndPage { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PageRange()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="startPage">起始页码</param>
        /// <param name="endPage">结束页码</param>
        public PageRange(int startPage, int endPage)
        {
            StartPage = startPage;
            EndPage = endPage;
        }

        /// <summary>
        /// 验证页面范围是否有效
        /// </summary>
        /// <returns>是否有效</returns>
        public bool IsValid()
        {
            return StartPage > 0 && EndPage >= StartPage;
        }

        /// <summary>
        /// 获取页面数量
        /// </summary>
        /// <returns>页面数量</returns>
        public int GetPageCount()
        {
            return IsValid() ? EndPage - StartPage + 1 : 0;
        }

        /// <summary>
        /// 检查指定页码是否在范围内
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <returns>是否在范围内</returns>
        public bool Contains(int pageNumber)
        {
            return IsValid() && pageNumber >= StartPage && pageNumber <= EndPage;
        }
    }
}

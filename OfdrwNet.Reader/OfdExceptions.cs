using System;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// OFD异常基类
    /// </summary>
    public class OfdException : Exception
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errorCode">错误代码</param>
        /// <param name="message">错误消息</param>
        public OfdException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errorCode">错误代码</param>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public OfdException(string errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// 文档加载异常
    /// </summary>
    public class DocumentLoadException : OfdException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        public DocumentLoadException(string message) : base("DOC_LOAD_ERROR", message)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public DocumentLoadException(string message, Exception innerException) : base("DOC_LOAD_ERROR", message, innerException)
        {
        }
    }

    /// <summary>
    /// 渲染异常
    /// </summary>
    public class RenderException : OfdException
    {
        /// <summary>
        /// 对象ID
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <param name="message">错误消息</param>
        public RenderException(string objectId, string message) : base("RENDER_ERROR", message)
        {
            ObjectId = objectId;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public RenderException(string objectId, string message, Exception innerException) : base("RENDER_ERROR", message, innerException)
        {
            ObjectId = objectId;
        }
    }

    /// <summary>
    /// 导航异常
    /// </summary>
    public class NavigationException : OfdException
    {
        /// <summary>
        /// 页面编号
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pageNumber">页面编号</param>
        /// <param name="message">错误消息</param>
        public NavigationException(int pageNumber, string message) : base("NAV_ERROR", message)
        {
            PageNumber = pageNumber;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pageNumber">页面编号</param>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public NavigationException(int pageNumber, string message, Exception innerException) : base("NAV_ERROR", message, innerException)
        {
            PageNumber = pageNumber;
        }
    }
}

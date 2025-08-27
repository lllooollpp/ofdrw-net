using System;

namespace OfdrwNet.Layout.Engine.Render
{
    /// <summary>
    /// 渲染异常
    /// <para>
    /// 在元素渲染过程中发生的异常
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-03-21 14:20:00
    /// </summary>
    public class RenderException : Exception
    {
        /// <summary>
        /// 创建渲染异常
        /// </summary>
        public RenderException() : base()
        {
        }

        /// <summary>
        /// 创建渲染异常
        /// </summary>
        /// <param name="message">异常消息</param>
        public RenderException(string message) : base(message)
        {
        }

        /// <summary>
        /// 创建渲染异常
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="innerException">内部异常</param>
        public RenderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
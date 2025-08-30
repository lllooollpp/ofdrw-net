using System;
using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Doc.VPreferences.Zoom
{
    /// <summary>
    /// 自动缩放模式
    /// 
    /// 默认值为 Default
    /// 
    /// 7.5 表 9 视图首选项
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-07 09:18:57
    /// </summary>
    public class ZoomMode : ZoomScale
    {
        public ZoomMode(XElement proxy) : base(proxy)
        {
        }

        private ZoomMode() : base("ZoomMode")
        {
        }

        private ZoomMode(string type) : this()
        {
            Element.Value = type; // 修复：使用Element.Value代替AddText
        }

        public enum ZoomType
        {
            /// <summary>
            /// 默认缩放
            /// </summary>
            Default,
            /// <summary>
            /// 合适高度
            /// </summary>
            FitHeight,
            /// <summary>
            /// 合适宽度
            /// </summary>
            FitWidth,
            /// <summary>
            /// 合适区域
            /// </summary>
            FitRect
        }

        /// <summary>
        /// 获取自动缩放模式类型
        /// 类型参考 ZoomType
        /// </summary>
        /// <returns>自动缩放模式类型</returns>
        public ZoomType GetZoomType()
        {
            var str = GetText();
            switch (str)
            {
                case "Default":
                    return ZoomType.Default;
                case "FitHeight":
                    return ZoomType.FitHeight;
                case "FitWidth":
                    return ZoomType.FitWidth;
                case "FitRect":
                    return ZoomType.FitRect;
                default:
                    throw new ArgumentException($"未知的自动缩放模式：{str}");
            }
        }

        /// <summary>
        /// 获取工厂方式枚举的实例
        /// </summary>
        /// <param name="type">自动缩放模式类型</param>
        /// <returns>自动缩放模式</returns>
        public static ZoomMode GetInstance(ZoomType type)
        {
            return new ZoomMode(type.ToString());
        }
    }
}
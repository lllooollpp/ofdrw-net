using System;

namespace OfdrwNet.Core.BasicStructure.Doc.VPreferences
{
    /// <summary>
    /// 窗口模式
    /// 
    /// 7.5 表 9 视图首选项属性
    /// 默认值为 None
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-07 06:33:01
    /// </summary>
    public enum PageMode
    {
        /// <summary>
        /// 常规模式
        /// </summary>
        None,
        /// <summary>
        /// 开启后全文显示
        /// </summary>
        FullScreen,
        /// <summary>
        /// 同时呈现文档大纲
        /// </summary>
        UseOutlines,
        /// <summary>
        /// 同时呈现缩略图
        /// </summary>
        UseThumbs,
        /// <summary>
        /// 同时呈现语义结构
        /// </summary>
        UseCustomTags,
        /// <summary>
        /// 同时呈现图层
        /// </summary>
        UseLayers,
        /// <summary>
        /// 同时呈现附件
        /// </summary>
        UseAttatchs,
        /// <summary>
        /// 同时呈现书签
        /// </summary>
        UseBookmarks
    }

    public static class PageModeExtensions
    {
        /// <summary>
        /// 获取窗口模式实例
        /// </summary>
        /// <param name="mode">模式名称</param>
        /// <returns>实例</returns>
        public static PageMode GetInstance(string mode)
        {
            mode = mode?.Trim() ?? "";
            switch (mode)
            {
                case "":
                case "None":
                    return PageMode.None;
                case "FullScreen":
                    return PageMode.FullScreen;
                case "UseOutlines":
                    return PageMode.UseOutlines;
                case "UseThumbs":
                    return PageMode.UseThumbs;
                case "UseCustomTags":
                    return PageMode.UseCustomTags;
                case "UseLayers":
                    return PageMode.UseLayers;
                case "UseAttatchs":
                    return PageMode.UseAttatchs;
                case "UseBookmarks":
                    return PageMode.UseBookmarks;
                default:
                    throw new ArgumentException($"未知的窗口模式：{mode}");
            }
        }
    }
}

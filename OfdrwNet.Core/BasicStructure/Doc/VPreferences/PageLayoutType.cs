using System;

namespace OfdrwNet.Core.BasicStructure.Doc.VPreferences
{
    /// <summary>
    /// 页面布局
    /// 
    /// 7.5 表 9 视图首选项
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-07 06:54:01
    /// </summary>
    public enum PageLayoutType
    {
        /// <summary>
        /// 单页模式
        /// </summary>
        OnePage,
        /// <summary>
        /// 单列模式
        /// </summary>
        OneColumn,
        /// <summary>
        /// 对开模式
        /// </summary>
        TwoPageL,
        /// <summary>
        /// 对开连续模式
        /// </summary>
        TwoColumnL,
        /// <summary>
        /// 对开靠右模式
        /// </summary>
        TwoPageR,
        /// <summary>
        /// 对开连续靠右模式
        /// </summary>
        TwoColumnR
    }

    public static class PageLayoutTypeExtensions
    {
        public static PageLayoutType GetInstance(string pageLayout)
        {
            pageLayout = pageLayout?.Trim() ?? "";

            switch (pageLayout)
            {
                case "":
                case "OnePage":
                    return PageLayoutType.OnePage;
                case "OneColumn":
                    return PageLayoutType.OneColumn;
                case "TwoPageL":
                    return PageLayoutType.TwoPageL;
                case "TwoColumnL":
                    return PageLayoutType.TwoColumnL;
                case "TwoPageR":
                    return PageLayoutType.TwoPageR;
                case "TwoColumnR":
                    return PageLayoutType.TwoColumnR;
                default:
                    throw new ArgumentException($"未知页面布局类型：{pageLayout}");
            }
        }
    }
}

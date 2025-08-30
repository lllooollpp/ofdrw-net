using System;

namespace OfdrwNet.Core.BasicStructure.Doc.VPreferences
{
    /// <summary>
    /// 标题栏显示模式
    /// 
    /// 默认值为 FileName，当设置为 DocTitle但不存在 Title属性时，
    /// 按照 FileName 处理
    /// 
    /// 7.5 表 9 视图首选项
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-07 09:07:50
    /// </summary>
    public enum TabDisplay
    {
        /// <summary>
        /// 文件名称
        /// </summary>
        FileName,
        /// <summary>
        /// 呈现元数据中的 Title 属性
        /// </summary>
        DocTitle
    }

    public static class TabDisplayExtensions
    {
        public static TabDisplay GetInstance(string tabDisplay)
        {
            if (string.IsNullOrWhiteSpace(tabDisplay))
            {
                return TabDisplay.FileName;
            }
            switch (tabDisplay)
            {
                case "FileName":
                    return TabDisplay.FileName;
                case "DocTitle":
                    return TabDisplay.DocTitle;
                default:
                    throw new ArgumentException($"未知的标题栏显示模式：{tabDisplay}");
            }
        }
    }
}

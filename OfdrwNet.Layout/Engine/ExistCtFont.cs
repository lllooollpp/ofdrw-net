using System;
using OfdrwNet.Core.BasicStructure.Res;

namespace OfdrwNet.Layout.Engine
{
    /// <summary>
    /// 包装查找到的字体信息与对应的绝对字体路径
    /// </summary>
    public class ExistCtFont
    {
        /// <summary>
        /// 找到的字体信息（FontInfo）
        /// </summary>
        public FontInfo FontInfo { get; }

        /// <summary>
        /// 字体文件在本地的绝对路径（如果可用）
        /// </summary>
        public string? AbsPath { get; }

        public ExistCtFont(FontInfo fontInfo, string? absPath)
        {
            FontInfo = fontInfo ?? throw new System.ArgumentNullException(nameof(fontInfo));
            AbsPath = absPath;
        }
    }
}

namespace OfdrwNet.Layout.Element
{
    /// <summary>
    /// 浮动类型
    /// </summary>
    public enum AFloat
    {
        /// <summary>
        /// 左浮动
        /// </summary>
        Left,
        /// <summary>
        /// 右浮动
        /// </summary>
        Right,
        /// <summary>
        /// 居中
        /// </summary>
        Center,
        /// <summary>
        /// 不浮动
        /// </summary>
        None
    }

    /// <summary>
    /// 清除浮动类型
    /// </summary>
    public enum Clear
    {
        /// <summary>
        /// 清除左浮动
        /// </summary>
        Left,
        /// <summary>
        /// 清除右浮动
        /// </summary>
        Right,
        /// <summary>
        /// 清除两边浮动
        /// </summary>
        Both,
        /// <summary>
        /// 不清除浮动
        /// </summary>
        None
    }

    /// <summary>
    /// 位置类型
    /// </summary>
    public enum Position
    {
        /// <summary>
        /// 静态定位
        /// </summary>
        Static,
        /// <summary>
        /// 相对定位
        /// </summary>
        Relative,
        /// <summary>
        /// 绝对定位
        /// </summary>
        Absolute,
        /// <summary>
        /// 固定定位
        /// </summary>
        Fixed
    }

    /// <summary>
    /// 显示类型
    /// </summary>
    public enum Display
    {
        /// <summary>
        /// 块级元素
        /// </summary>
        Block,
        /// <summary>
        /// 内联元素
        /// </summary>
        Inline,
        /// <summary>
        /// 内联块元素
        /// </summary>
        InlineBlock,
        /// <summary>
        /// 不显示
        /// </summary>
        None
    }
}
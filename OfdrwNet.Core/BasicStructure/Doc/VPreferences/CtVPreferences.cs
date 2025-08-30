using System.Xml.Linq;
using OfdrwNet.Core.BasicStructure.Doc.VPreferences.Zoom;

namespace OfdrwNet.Core.BasicStructure.Doc.VPreferences
{
    /// <summary>
    /// 视图首选项
    /// 
    /// 本标准支持设置文档视图首选项（VPreferences）节点，以达到限定文档初始化视图
    /// 便于阅读的目的。
    /// 
    /// 7.5 图 10 视图首选项结构
    /// 
    /// 作者：权观宇
    /// 时间：2019-10-07 05:55:03
    /// </summary>
    public class CtVPreferences : OfdElement
    {
        public CtVPreferences(XElement proxy) : base(proxy)
        {
        }

        public CtVPreferences() : base("VPreferences")
        {
        }

        /// <summary>
        /// 【可选】
        /// 设置窗口模式
        /// 可选的模式列表，请参考 PageMode
        /// 默认值为 None
        /// </summary>
        /// <param name="pageMode">窗口模式</param>
        /// <returns>this</returns>
        public CtVPreferences SetPageMode(PageMode pageMode)
        {
            SetOfdEntity("PageMode", pageMode.ToString());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取窗口模式
        /// 可选的模式列表，请参考 PageMode
        /// 默认值为 None
        /// </summary>
        /// <returns>窗口模式</returns>
        public PageMode GetPageMode()
        {
            var mode = GetOfdElementText("PageMode");
            if (string.IsNullOrWhiteSpace(mode))
            {
                return PageMode.None;
            }
            return PageModeExtensions.GetInstance(mode);
        }

        /// <summary>
        /// 【可选】
        /// 设置页面布局模式
        /// 可选的模式请参考 PageLayoutType
        /// 默认值为 OneColumn
        /// </summary>
        /// <param name="pageLayout">页面布局模式</param>
        /// <returns>this</returns>
        public CtVPreferences SetPageLayout(PageLayoutType pageLayout)
        {
            SetOfdEntity("PageLayout", pageLayout.ToString());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取页面布局模式
        /// 可选的模式请参考 PageLayoutType
        /// 默认值为 OneColumn
        /// </summary>
        /// <returns>页面布局模式</returns>
        public PageLayoutType GetPageLayout()
        {
            var str = GetOfdElementText("PageLayout");
            if (string.IsNullOrWhiteSpace(str))
            {
                return PageLayoutType.OneColumn;
            }
            return PageLayoutTypeExtensions.GetInstance(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置标题栏显示模式
        /// 默认值为 FileName，当设置为 DocTitle但不存在 Title属性时，
        /// 按照 FileName 处理
        /// </summary>
        /// <param name="tabDisplay">标题栏显示模式</param>
        /// <returns>this</returns>
        public CtVPreferences SetTabDisplay(TabDisplay tabDisplay)
        {
            SetOfdEntity("TabDisplay", tabDisplay.ToString());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取标题栏显示模式
        /// 默认值为 FileName，当设置为 DocTitle但不存在 Title属性时，
        /// 按照 FileName 处理
        /// </summary>
        /// <returns>标题栏显示模式</returns>
        public TabDisplay GetTabDisplay()
        {
            var str = GetOfdElementText("TabDisplay");
            if (string.IsNullOrWhiteSpace(str))
            {
                return TabDisplay.FileName;
            }
            return TabDisplayExtensions.GetInstance(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置是否隐藏工具栏
        /// 默认值：false
        /// </summary>
        /// <param name="hideToolbar">true - 隐藏；false - 不隐藏</param>
        /// <returns>this</returns>
        public CtVPreferences SetHideToolbar(bool hideToolbar)
        {
            SetOfdEntity("HideToolbar", hideToolbar.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取是否隐藏工具栏
        /// 默认值：false
        /// </summary>
        /// <returns>true - 隐藏；false - 不隐藏</returns>
        public bool GetHideToolbar()
        {
            var str = GetOfdElementText("HideToolbar");
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置是否隐藏菜单栏
        /// 默认值：false
        /// </summary>
        /// <param name="hideMenubar">true - 隐藏；false - 不隐藏</param>
        /// <returns>this</returns>
        public CtVPreferences SetHideMenubar(bool hideMenubar)
        {
            SetOfdEntity("HideMenubar", hideMenubar.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取是否隐藏菜单栏
        /// 默认值：false
        /// </summary>
        /// <returns>true - 隐藏；false - 不隐藏</returns>
        public bool GetHideMenubar()
        {
            var str = GetOfdElementText("HideMenubar");
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置是否隐藏主窗口之外的其他窗口组件
        /// 默认值：false
        /// </summary>
        /// <param name="hideWindowUI">true - 隐藏；false - 不隐藏</param>
        /// <returns>this</returns>
        public CtVPreferences SetHideWindowUI(bool hideWindowUI)
        {
            SetOfdEntity("HideWindowUI", hideWindowUI.ToString().ToLower());
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取是否隐藏主窗口之外的其他窗口组件
        /// 默认值：false
        /// </summary>
        /// <returns>true - 隐藏；false - 不隐藏</returns>
        public bool GetHideWindowUI()
        {
            var str = GetOfdElementText("HideWindowUI");
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }
            return bool.Parse(str);
        }

        /// <summary>
        /// 【可选】
        /// 设置文档自动缩放模式
        /// 参考值 ZoomMode.Type
        /// </summary>
        /// <param name="zoomMode">文档自动缩放模式</param>
        /// <returns>this</returns>
        public CtVPreferences SetZoomMode(ZoomMode zoomMode)
        {
            // 从节点中删除所有可以选择的类型
            RemoveOfdElementsByNames("Zoom", "ZoomMode");
            Add(zoomMode);
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 设置文档的缩放率
        /// </summary>
        /// <param name="zoom">文档的缩放率</param>
        /// <returns>this</returns>
        public CtVPreferences SetZoom(double zoom)
        {
            // 从节点中删除所有可以选择的类型
            RemoveOfdElementsByNames("Zoom", "ZoomMode");
            Add(new ZoomValue(zoom));
            return this;
        }

        /// <summary>
        /// 【可选】
        /// 获取具体的缩放处理方式和值
        /// </summary>
        /// <returns>具体的缩放处理方式和值 ZoomValue 或 ZoomMode</returns>
        public ZoomScale? GetZoomScale()
        {
            var zoomMode = GetOfdElement("ZoomMode");
            if (zoomMode != null)
            {
                return new ZoomMode(zoomMode);
            }
            var zoom = GetOfdElement("Zoom");
            if (zoom != null)
            {
                return new ZoomValue(zoom);
            }
            return null;
        }
    }
}

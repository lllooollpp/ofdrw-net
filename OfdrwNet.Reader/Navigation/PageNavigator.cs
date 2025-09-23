using System;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader.Navigation
{
    /// <summary>
    /// 页面导航器
    /// 提供页面跳转、历史记录、定位等功能
    /// </summary>
    /// <summary>
    /// 页面导航器，提供页面跳转、历史记录、定位等功能
    /// </summary>
    public class PageNavigator
    {
        private readonly OfdDocument _document;
        private int _currentPageIndex;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="document">要导航的OFD文档</param>
        /// <exception cref="ArgumentNullException">当document为null时抛出</exception>
        public PageNavigator(OfdDocument document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _currentPageIndex = 0;
        }

        /// <summary>
        /// 当前页索引
        /// </summary>
        public int CurrentPageIndex => _currentPageIndex;

        /// <summary>
        /// 当前页信息
        /// </summary>
        public PageInfo? CurrentPage => _document.Pages.Count > _currentPageIndex ? _document.Pages[_currentPageIndex] : null;

        /// <summary>
        /// 跳转到指定页
        /// </summary>
        public bool GoToPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _document.Pages.Count)
                return false;
            _currentPageIndex = pageIndex;
            return true;
        }

        /// <summary>
        /// 跳转到下一页
        /// </summary>
        public bool NextPage()
        {
            if (_currentPageIndex < _document.Pages.Count - 1)
            {
                _currentPageIndex++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 跳转到上一页
        /// </summary>
        public bool PreviousPage()
        {
            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 跳转到首页
        /// </summary>
        public void FirstPage()
        {
            _currentPageIndex = 0;
        }

        /// <summary>
        /// 跳转到末页
        /// </summary>
        public void LastPage()
        {
            _currentPageIndex = _document.Pages.Count > 0 ? _document.Pages.Count - 1 : 0;
        }
    }
}

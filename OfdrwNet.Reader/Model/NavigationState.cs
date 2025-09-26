using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// 导航状态管理
    /// 维护当前文档的导航状态、历史记录和序列化能力
    /// </summary>
    public class NavigationState
    {
        /// <summary>
        /// 当前页面索引（从0开始）
        /// </summary>
        public int CurrentPageIndex { get; set; }

        /// <summary>
        /// 当前页面号（从1开始，与CurrentPageIndex兼容）
        /// </summary>
        public int CurrentPage
        {
            get => CurrentPageIndex + 1;
            set => CurrentPageIndex = Math.Max(0, value - 1);
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 当前缩放级别
        /// </summary>
        public double ZoomLevel { get; set; } = 1.0;

        /// <summary>
        /// 当前视口位置
        /// </summary>
        public Point ViewportPosition { get; set; }

        /// <summary>
        /// 导航历史记录
        /// </summary>
        public NavigationHistory History { get; private set; } = new NavigationHistory();

        /// <summary>
        /// 书签列表
        /// </summary>
        public List<NavigationBookmark> Bookmarks { get; private set; } = new List<NavigationBookmark>();

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// 导航到指定页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="recordHistory">是否记录历史</param>
        /// <returns>导航是否成功</returns>
        public bool NavigateToPage(int pageIndex, bool recordHistory = true)
        {
            if (pageIndex < 0 || pageIndex >= TotalPages)
                return false;

            if (recordHistory && pageIndex != CurrentPageIndex)
            {
                History.AddEntry(new NavigationHistoryEntry
                {
                    PageIndex = CurrentPageIndex,
                    ZoomLevel = ZoomLevel,
                    ViewportPosition = ViewportPosition,
                    Timestamp = DateTime.UtcNow
                });
            }

            CurrentPageIndex = pageIndex;
            LastModified = DateTime.UtcNow;
            return true;
        }

        /// <summary>
        /// 设置缩放级别
        /// </summary>
        /// <param name="zoomLevel">缩放级别</param>
        /// <param name="centerPoint">缩放中心点</param>
        public void SetZoomLevel(double zoomLevel, Point? centerPoint = null)
        {
            if (zoomLevel <= 0)
                return;

            ZoomLevel = Math.Max(0.1, Math.Min(10.0, zoomLevel));

            // 如果指定了中心点，调整视口位置以保持中心点不变
            if (centerPoint.HasValue)
            {
                var center = centerPoint.Value;
                ViewportPosition = new Point(
                    (int)(center.X * ZoomLevel - center.X),
                    (int)(center.Y * ZoomLevel - center.Y)
                );
            }

            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// 设置视口位置
        /// </summary>
        /// <param name="position">视口位置</param>
        public void SetViewportPosition(Point position)
        {
            ViewportPosition = position;
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// 后退导航
        /// </summary>
        /// <returns>是否成功后退</returns>
        public bool GoBack()
        {
            var entry = History.GoBack();
            if (entry != null)
            {
                CurrentPageIndex = entry.PageIndex;
                ZoomLevel = entry.ZoomLevel;
                ViewportPosition = entry.ViewportPosition;
                LastModified = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 前进导航
        /// </summary>
        /// <returns>是否成功前进</returns>
        public bool GoForward()
        {
            var entry = History.GoForward();
            if (entry != null)
            {
                CurrentPageIndex = entry.PageIndex;
                ZoomLevel = entry.ZoomLevel;
                ViewportPosition = entry.ViewportPosition;
                LastModified = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 添加书签
        /// </summary>
        /// <param name="title">书签标题</param>
        /// <param name="pageIndex">页面索引（可选，默认当前页）</param>
        /// <returns>添加的书签</returns>
        public NavigationBookmark AddBookmark(string title, int? pageIndex = null)
        {
            var bookmark = new NavigationBookmark
            {
                Id = Guid.NewGuid(),
                Title = title,
                PageIndex = pageIndex ?? CurrentPageIndex,
                ZoomLevel = ZoomLevel,
                ViewportPosition = ViewportPosition,
                CreatedTime = DateTime.UtcNow
            };

            Bookmarks.Add(bookmark);
            LastModified = DateTime.UtcNow;
            return bookmark;
        }

        /// <summary>
        /// 删除书签
        /// </summary>
        /// <param name="bookmarkId">书签ID</param>
        /// <returns>是否成功删除</returns>
        public bool RemoveBookmark(Guid bookmarkId)
        {
            var bookmark = Bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
            if (bookmark != null)
            {
                Bookmarks.Remove(bookmark);
                LastModified = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 导航到书签
        /// </summary>
        /// <param name="bookmarkId">书签ID</param>
        /// <returns>是否成功导航</returns>
        public bool NavigateToBookmark(Guid bookmarkId)
        {
            var bookmark = Bookmarks.FirstOrDefault(b => b.Id == bookmarkId);
            if (bookmark != null)
            {
                NavigateToPage(bookmark.PageIndex);
                SetZoomLevel(bookmark.ZoomLevel);
                SetViewportPosition(bookmark.ViewportPosition);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 重置导航状态
        /// </summary>
        public void Reset()
        {
            CurrentPageIndex = 0;
            ZoomLevel = 1.0;
            ViewportPosition = Point.Empty;
            History.Clear();
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// 序列化为JSON
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// 序列化状态（与ToJson相同，提供兼容性）
        /// </summary>
        /// <returns>序列化的状态字符串</returns>
        public string SerializeState()
        {
            return ToJson();
        }

        /// <summary>
        /// 恢复状态（与FromJson相同，提供兼容性）
        /// </summary>
        /// <param name="stateData">状态数据</param>
        public void RestoreState(string stateData)
        {
            var restoredState = FromJson(stateData);
            if (restoredState != null)
            {
                CurrentPageIndex = restoredState.CurrentPageIndex;
                TotalPages = restoredState.TotalPages;
                ZoomLevel = restoredState.ZoomLevel;
                ViewportPosition = restoredState.ViewportPosition;
                History = restoredState.History;
                Bookmarks = restoredState.Bookmarks;
                LastModified = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 从JSON反序列化
        /// </summary>
        /// <param name="json">JSON字符串</param>
        /// <returns>导航状态对象</returns>
        public static NavigationState? FromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                return JsonSerializer.Deserialize<NavigationState>(json, options);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void SaveToFile(string filePath)
        {
            var json = ToJson();
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 从文件加载
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>导航状态对象</returns>
        public static NavigationState? LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                return FromJson(json);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 导航历史记录管理
    /// </summary>
    public class NavigationHistory
    {
        private readonly List<NavigationHistoryEntry> _entries = new List<NavigationHistoryEntry>();
        private int _currentIndex = -1;
        private const int MaxHistorySize = 100;

        /// <summary>
        /// 是否可以后退
        /// </summary>
        public bool CanGoBack => _currentIndex > 0;

        /// <summary>
        /// 是否可以前进
        /// </summary>
        public bool CanGoForward => _currentIndex >= 0 && _currentIndex < _entries.Count - 1;

        /// <summary>
        /// 历史记录数量
        /// </summary>
        public int Count => _entries.Count;

        /// <summary>
        /// 添加历史记录条目
        /// </summary>
        /// <param name="entry">历史记录条目</param>
        public void AddEntry(NavigationHistoryEntry entry)
        {
            // 如果当前不在历史记录末尾，删除当前位置之后的所有记录
            if (_currentIndex >= 0 && _currentIndex < _entries.Count - 1)
            {
                _entries.RemoveRange(_currentIndex + 1, _entries.Count - _currentIndex - 1);
            }

            // 添加新记录
            _entries.Add(entry);
            _currentIndex = _entries.Count - 1;

            // 保持历史记录大小限制
            if (_entries.Count > MaxHistorySize)
            {
                _entries.RemoveAt(0);
                _currentIndex--;
            }
        }

        /// <summary>
        /// 后退
        /// </summary>
        /// <returns>历史记录条目</returns>
        public NavigationHistoryEntry? GoBack()
        {
            if (CanGoBack)
            {
                _currentIndex--;
                return _entries[_currentIndex];
            }
            return null;
        }

        /// <summary>
        /// 前进
        /// </summary>
        /// <returns>历史记录条目</returns>
        public NavigationHistoryEntry? GoForward()
        {
            if (CanGoForward)
            {
                _currentIndex++;
                return _entries[_currentIndex];
            }
            return null;
        }

        /// <summary>
        /// 获取历史记录列表
        /// </summary>
        /// <returns>历史记录条目列表</returns>
        public List<NavigationHistoryEntry> GetEntries()
        {
            return new List<NavigationHistoryEntry>(_entries);
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _currentIndex = -1;
        }
    }

    /// <summary>
    /// 导航历史记录条目
    /// </summary>
    public class NavigationHistoryEntry
    {
        /// <summary>
        /// 页面索引
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 缩放级别
        /// </summary>
        public double ZoomLevel { get; set; }

        /// <summary>
        /// 视口位置
        /// </summary>
        public Point ViewportPosition { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 可选的描述信息
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 转换为显示字符串
        /// </summary>
        /// <returns>显示字符串</returns>
        public override string ToString()
        {
            return $"页面 {PageIndex + 1}, 缩放 {ZoomLevel:P0}, {Timestamp:yyyy-MM-dd HH:mm:ss}";
        }
    }

    /// <summary>
    /// 导航书签
    /// </summary>
    public class NavigationBookmark
    {
        /// <summary>
        /// 书签ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 书签标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 页面索引
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 缩放级别
        /// </summary>
        public double ZoomLevel { get; set; }

        /// <summary>
        /// 视口位置
        /// </summary>
        public Point ViewportPosition { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 可选的描述信息
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 转换为显示字符串
        /// </summary>
        /// <returns>显示字符串</returns>
        public override string ToString()
        {
            return $"{Title} - 页面 {PageIndex + 1}";
        }
    }

    /// <summary>
    /// 导航状态变更事件参数
    /// </summary>
    public class NavigationStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更类型
        /// </summary>
        public NavigationChangeType ChangeType { get; set; }

        /// <summary>
        /// 旧的页面索引
        /// </summary>
        public int OldPageIndex { get; set; }

        /// <summary>
        /// 新的页面索引
        /// </summary>
        public int NewPageIndex { get; set; }

        /// <summary>
        /// 旧的缩放级别
        /// </summary>
        public double OldZoomLevel { get; set; }

        /// <summary>
        /// 新的缩放级别
        /// </summary>
        public double NewZoomLevel { get; set; }

        /// <summary>
        /// 旧的视口位置
        /// </summary>
        public Point OldViewportPosition { get; set; }

        /// <summary>
        /// 新的视口位置
        /// </summary>
        public Point NewViewportPosition { get; set; }
    }

    /// <summary>
    /// 导航变更类型
    /// </summary>
    public enum NavigationChangeType
    {
        /// <summary>
        /// 页面改变
        /// </summary>
        PageChanged,

        /// <summary>
        /// 缩放改变
        /// </summary>
        ZoomChanged,

        /// <summary>
        /// 视口位置改变
        /// </summary>
        ViewportChanged,

        /// <summary>
        /// 书签添加
        /// </summary>
        BookmarkAdded,

        /// <summary>
        /// 书签删除
        /// </summary>
        BookmarkRemoved,

        /// <summary>
        /// 历史记录改变
        /// </summary>
        HistoryChanged
    }
}

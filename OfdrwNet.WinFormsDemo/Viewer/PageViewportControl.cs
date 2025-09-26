using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;
using OfdrwNet.Reader.Rendering;

namespace OfdrwNet.WinFormsDemo.Viewer
{
    /// <summary>
    /// OFD页面显示控件，负责页面内容的可视化渲染
    /// </summary>
    public partial class PageViewportControl : UserControl
    {
        private IEnumerable<RenderObject>? _pageObjects;
        private RenderingEngine? _renderingEngine;
        private RenderContext _renderContext = RenderContext.CreateDefault();
        private bool _isLoading;
        private string? _lastError;
        private float _zoom = 1.0f; // 逻辑缩放（额外于 RenderContext.ScaleFactor）
        // 离屏渲染支持
        private Bitmap? _backBuffer;
        private bool _renderDirty;
        private bool _isRendering;
        private CancellationTokenSource? _renderCts;
        private readonly object _renderLock = new();
        private RenderRequestReason _pendingReason = RenderRequestReason.None;

        private enum RenderRequestReason
        {
            None = 0,
            ContextChanged,
            ContentChanged,
            Resize,
            ZoomChanged,
            External
        }

        /// <summary>
        /// 当前缩放；设置后触发重绘
        /// </summary>
        public float Zoom
        {
            get => _zoom;
            set
            {
                if (value <= 0) return;
                _zoom = value;
                RequestRender(RenderRequestReason.ZoomChanged);
            }
        }

        /// <summary>
        /// 指示是否加载中
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; Invalidate(); }
        }

        /// <summary>
        /// 注入渲染上下文（可共享）
        /// </summary>
        public void SetRenderContext(RenderContext ctx)
        {
            _renderContext = ctx ?? RenderContext.CreateDefault();
            RequestRender(RenderRequestReason.ContextChanged);
        }

        /// <summary>
        /// 设置页面数据源（对象集合 + 引擎）。不做深拷贝，调用方保证生命周期。
        /// </summary>
        public void SetPageContent(IEnumerable<RenderObject>? objects, RenderingEngine? engine)
        {
            _pageObjects = objects;
            _renderingEngine = engine;
            _lastError = null;
            RequestRender(RenderRequestReason.ContentChanged);
        }

        /// <summary>
        /// 触发数据更新重绘（外部在解析完成后调用）
        /// </summary>
        public void NotifyDataChanged()
        {
            _lastError = null;
            RequestRender(RenderRequestReason.ContentChanged);
        }
        /// <summary>
        /// 初始化PageViewportControl的新实例
        /// </summary>
        public PageViewportControl()
        {
            InitializeComponent();

            // 启用双缓冲以减少闪烁
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // 基础控件设置
            BackColor = Color.White;
            Name = "PageViewportControl";

            ResumeLayout(false);
        }

        /// <summary>
        /// 重写OnPaint方法以自定义绘制页面内容
        /// </summary>
        /// <param name="e">绘制事件参数</param>
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(Color.White);

            // 加载状态
            if (IsLoading)
            {
                DrawCenteredMessage(g, "正在加载页面...");
                return;
            }

            if (_renderingEngine == null || _pageObjects == null)
            {
                DrawCenteredMessage(g, "未加载页面");
                return;
            }

            if (!_pageObjects.Any())
            {
                DrawCenteredMessage(g, "页面无可渲染对象");
                return;
            }

            // 离屏缓存路径：仅绘制已完成的位图；未完成显示提示，错误显示叠加
            if (_backBuffer != null)
            {
                g.DrawImageUnscaled(_backBuffer, 0, 0);
            }
            else
            {
                DrawCenteredMessage(g, "正在准备渲染...");
            }

            if (_isRendering)
            {
                DrawTopLeftInfo(g, "渲染中...");
            }

            if (!string.IsNullOrEmpty(_lastError))
            {
                DrawErrorOverlay(g, _lastError!);
            }
        }

        /// <summary>
        /// 请求后台重新渲染（离屏）
        /// </summary>
        private void RequestRender(RenderRequestReason reason)
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0) { Invalidate(); return; }

            var stackTrace = new System.Diagnostics.StackTrace(1, true);
            var frame = stackTrace.GetFrame(0);
            var callerInfo = frame != null ? $"{frame.GetMethod()?.Name}:{frame.GetFileLineNumber()}" : "Unknown";

            lock (_renderLock)
            {
                _renderDirty = true;
                bool hadCts = _renderCts != null;
                bool wasRendering = _isRendering;

                if (wasRendering)
                {
                    if (reason > _pendingReason) _pendingReason = reason; // 记录最高优先级
                    System.Diagnostics.Trace.WriteLine($"[PageViewport] RequestRender(累计) - 原因: {reason}, 正在渲染中, pending={_pendingReason}");
                    return;
                }

                _pendingReason = RenderRequestReason.None;
                _renderCts?.Cancel();
                System.Diagnostics.Trace.WriteLine($"[PageViewport] RequestRender(启动) - 原因: {reason}, 来源: {callerInfo}, hadCts={hadCts}");
                _renderCts = new CancellationTokenSource();
                _ = StartRenderLoopAsync(_renderCts.Token);
            }
        }

        private async Task StartRenderLoopAsync(CancellationToken token)
        {
            System.Diagnostics.Trace.WriteLine($"[PageViewport] 开始渲染循环");

            while (true)
            {
                CancellationTokenSource? localCts;
                IEnumerable<RenderObject>? objs;
                RenderingEngine? engine;
                RenderContext ctx;
                int w, h;
                lock (_renderLock)
                {
                    if (!_renderDirty || token.IsCancellationRequested)
                    {
                        _isRendering = false;
                        System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染循环结束 - dirty={_renderDirty}, cancelled={token.IsCancellationRequested}");
                        return;
                    }
                    _isRendering = true;
                    _renderDirty = false; // 消费一次
                    localCts = _renderCts; // 用于取消检测
                    objs = _pageObjects;
                    engine = _renderingEngine;
                    ctx = _renderContext.Clone();
                    ctx.ScaleFactor *= _zoom;
                    w = Width; h = Height;
                }

                System.Diagnostics.Trace.WriteLine($"[PageViewport] 准备渲染 - 对象数量={objs?.Count() ?? 0}, 尺寸={w}x{h}, 缩放={ctx.ScaleFactor}");

                if (objs == null || engine == null || w <= 0 || h <= 0)
                {
                    lock (_renderLock) { _isRendering = false; }
                    System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染跳过 - 对象或引擎为空，或尺寸无效");
                    return;
                }

                try
                {
                    System.Diagnostics.Trace.WriteLine($"[PageViewport] 开始后台渲染...");
                    var bmp = new Bitmap(w, h);
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        System.Diagnostics.Trace.WriteLine($"[PageViewport] 调用渲染引擎，对象数量: {objs.Count()}");

                        // 真正渲染（同步等待确保 g 生命周期内完成）
                        var renderResult = await engine.RenderPageAsync(objs, g, ctx);

                        System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染引擎返回结果 - 成功: {renderResult.Success}, 错误: {renderResult.ErrorMessage}");

                        if (renderResult.Statistics != null)
                        {
                            System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染统计 - 总对象: {renderResult.Statistics.ObjectCount}, 成功: {renderResult.Statistics.SuccessfulObjects}, 失败: {renderResult.Statistics.FailedObjects}");
                        }

                        // 即使渲染引擎返回错误，我们也应该显示已渲染的内容
                        if (!renderResult.Success && !string.IsNullOrEmpty(renderResult.ErrorMessage))
                        {
                            System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染引擎报告问题，但继续显示: {renderResult.ErrorMessage}");
                            _lastError = renderResult.ErrorMessage;
                        }
                    }

                    System.Diagnostics.Trace.WriteLine($"[PageViewport] 检查取消状态 - token.IsCancellationRequested={token.IsCancellationRequested}, localCts.IsCancellationRequested={localCts?.IsCancellationRequested ?? false}");

                    // 检查是否在渲染过程中有新的渲染请求（通过比较 CTS 实例）
                    bool wasSuperseded;
                    lock (_renderLock)
                    {
                        wasSuperseded = _renderCts != localCts;
                    }

                    System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染完成状态检查 - wasSuperseded={wasSuperseded}");

                    if (wasSuperseded)
                    {
                        System.Diagnostics.Trace.WriteLine($"[PageViewport] 有新的渲染请求，但先显示当前结果");
                        // 即使有新请求，也先显示当前成功的渲染结果
                        var old = Interlocked.Exchange(ref _backBuffer, bmp);
                        old?.Dispose();
                        System.Diagnostics.Trace.WriteLine($"[PageViewport] 当前结果已显示，继续处理新请求");
                        if (!IsDisposed && !token.IsCancellationRequested)
                        {
                            try { BeginInvoke((Action)(Invalidate)); } catch { }
                        }
                        // 继续循环，处理新的渲染请求
                    }
                    else
                    {
                        // 交换位图 - 渲染成功且没有被取代
                        var old = Interlocked.Exchange(ref _backBuffer, bmp);
                        old?.Dispose();
                        System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染完成，更新显示");
                        if (!IsDisposed && !token.IsCancellationRequested)
                        {
                            try { BeginInvoke((Action)(Invalidate)); } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染异常: {ex.Message}");
                    System.Diagnostics.Trace.WriteLine($"[PageViewport] 异常详情: {ex}");
                    _lastError = ex.Message;
                    try { if (!IsDisposed) BeginInvoke((Action)(Invalidate)); } catch { }
                }

                // 循环检查是否在渲染期间又被标记为脏；否则退出
                lock (_renderLock)
                {
                    if (!_renderDirty)
                    {
                        if (_pendingReason != RenderRequestReason.None)
                        {
                            var pend = _pendingReason;
                            _pendingReason = RenderRequestReason.None;
                            System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染结束检测到待处理请求 -> 重新渲染 (原因: {pend})");
                            _renderDirty = true; // 触发再次循环
                        }
                        else
                        {
                            _isRendering = false;
                            System.Diagnostics.Trace.WriteLine($"[PageViewport] 渲染循环正常结束");
                            return;
                        }
                    }
                }
                System.Diagnostics.Trace.WriteLine($"[PageViewport] 继续下一轮渲染...");
                // 继续下一轮（立即，不延时）
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            System.Diagnostics.Trace.WriteLine($"[PageViewport] OnResize 触发 - 新尺寸: {Width}x{Height}, 正在渲染: {_isRendering}");
            if (Width > 0 && Height > 0)
            {
                RequestRender(RenderRequestReason.Resize);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderCts?.Cancel();
                _backBuffer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void DrawCenteredMessage(System.Drawing.Graphics g, string msg)
        {
            using var font = new System.Drawing.Font("Microsoft YaHei", 11f);
            using var brush = new System.Drawing.SolidBrush(Color.Gray);
            var size = g.MeasureString(msg, font);
            g.DrawString(msg, font, brush, (Width - size.Width) / 2f, (Height - size.Height) / 2f);
        }

        private void DrawErrorOverlay(System.Drawing.Graphics g, string err)
        {
            using var back = new System.Drawing.SolidBrush(Color.FromArgb(200, Color.Firebrick));
            using var fore = new System.Drawing.SolidBrush(Color.White);
            using var font = new System.Drawing.Font("Consolas", 9f);
            var text = "渲染错误: " + err;
            var size = g.MeasureString(text, font);
            var rect = new RectangleF(4, 4, size.Width + 8, size.Height + 4);
            g.FillRectangle(back, rect);
            g.DrawString(text, font, fore, 8, 6);
        }

        private void DrawTopLeftInfo(System.Drawing.Graphics g, string info)
        {
            using var back = new System.Drawing.SolidBrush(Color.FromArgb(140, Color.Black));
            using var fore = new System.Drawing.SolidBrush(Color.White);
            using var font = new System.Drawing.Font("Consolas", 8f);
            var size = g.MeasureString(info, font);
            g.FillRectangle(back, new RectangleF(4, Height - size.Height - 8, size.Width + 8, size.Height + 4));
            g.DrawString(info, font, fore, 8, Height - size.Height - 6);
        }
    }
}

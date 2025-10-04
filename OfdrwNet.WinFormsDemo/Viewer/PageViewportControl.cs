using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;
using OfdrwNet.Reader.Rendering;
using System.Threading;

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
        private float _zoom = 1.0f;
        
        // Performance optimization: reduce rendering
        private Bitmap? _backBuffer;
        private bool _renderDirty;
        private bool _isRendering;
        private CancellationTokenSource? _renderCts;
        private readonly object _renderLock = new();
        private RenderRequestReason _pendingReason = RenderRequestReason.None;
        
        // Cache key to avoid unnecessary re-renders
        private string? _lastRenderCacheKey;
        private int _lastRenderWidth;
        private int _lastRenderHeight;
        private DateTime _lastRenderTime = DateTime.MinValue;
        
        // Debug control - reduce log spam
        private static bool _enableDebugLogs = false;
        private int _debugCounter = 0;

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
                if (Math.Abs(value - _zoom) < 0.001f) return; // Skip minimal changes
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
            set { 
                if (_isLoading != value) {
                    _isLoading = value; 
                    Invalidate(); 
                }
            }
        }

        /// <summary>
        /// 注入渲染上下文（可共享）
        /// </summary>
        public void SetRenderContext(RenderContext ctx)
        {
            var newCtx = ctx ?? RenderContext.CreateDefault();
            if (IsSameRenderContext(newCtx)) return; // Skip if same
            
            _renderContext = newCtx;
            RequestRender(RenderRequestReason.ContextChanged);
        }

        /// <summary>
        /// 设置页面数据源（对象集合 + 引擎）。不做深拷贝，调用方保证生命周期。
        /// </summary>
        public void SetPageContent(IEnumerable<RenderObject>? objects, RenderingEngine? engine)
        {
            bool contentChanged = !ReferenceEquals(_pageObjects, objects) || !ReferenceEquals(_renderingEngine, engine);
            if (!contentChanged) return; // Skip if same references
            
            _pageObjects = objects;
            _renderingEngine = engine;
            _lastError = null;
            _lastRenderCacheKey = null; // Invalidate cache
            RequestRender(RenderRequestReason.ContentChanged);
        }

        /// <summary>
        /// 触发数据更新重绘（外部在解析完成后调用）
        /// </summary>
        public void NotifyDataChanged()
        {
            _lastError = null;
            _lastRenderCacheKey = null; // Invalidate cache
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
                     ControlStyles.DoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            BackColor = Color.White;
            Name = "PageViewportControl";
            ResumeLayout(false);
        }

        /// <summary>
        /// 重写OnPaint方法以自定义绘制页面内容
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
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

            // 离屏缓存路径：仅绘制已完成的位图
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
            if (!IsHandleCreated || Width <= 0 || Height <= 0) { 
                Invalidate(); 
                return; 
            }

            // Check cache validity - avoid render if content hasn't changed
            var cacheKey = GenerateRenderCacheKey();
            if (cacheKey == _lastRenderCacheKey && 
                Width == _lastRenderWidth && 
                Height == _lastRenderHeight &&
                _backBuffer != null &&
                reason != RenderRequestReason.External)
            {
                if (_enableDebugLogs) {
                    System.Diagnostics.Trace.WriteLine($"[PageViewport] Skipping render - cache valid for {reason}");
                }
                return; // Use cached result
            }

            lock (_renderLock)
            {
                _renderDirty = true;
                bool wasRendering = _isRendering;

                if (wasRendering)
                {
                    if (reason > _pendingReason) _pendingReason = reason;
                    if (_enableDebugLogs) {
                        System.Diagnostics.Trace.WriteLine($"[PageViewport] RequestRender(queued) - {reason}, pending={_pendingReason}");
                    }
                    return;
                }

                _pendingReason = RenderRequestReason.None;
                _renderCts?.Cancel();
                _renderCts = new CancellationTokenSource();
                
                // Use ConfigureAwait(false) to avoid deadlocks
                _ = Task.Run(() => StartRenderLoopAsync(_renderCts.Token));
            }
        }

        private async Task StartRenderLoopAsync(CancellationToken token)
        {
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
                        return;
                    }
                    _isRendering = true;
                    _renderDirty = false;
                    localCts = _renderCts;
                    objs = _pageObjects;
                    engine = _renderingEngine;
                    ctx = _renderContext.Clone();
                    ctx.ScaleFactor = 1.0; // Force no additional scaling
                    w = Width; h = Height;
                }

                if (objs == null || engine == null || w <= 0 || h <= 0)
                {
                    lock (_renderLock) { _isRendering = false; }
                    return;
                }

                Bitmap? bmp = null;
                try
                {
                    var startTime = DateTime.UtcNow;
                    
                    // Pre-allocate bitmap with better performance settings
                    bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        // Validate Graphics before setting properties
                        try
                        {
                            _ = g.IsClipEmpty; // Test Graphics validity
                        }
                        catch (Exception gex)
                        {
                            throw new InvalidOperationException($"Graphics from bitmap is invalid: {gex.Message}");
                        }

                        // Optimize graphics settings for performance
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                        
                        g.Clear(Color.White);

                        // Render with timeout protection
                        var renderTask = engine.RenderPageAsync(objs, g, ctx);
                        var timeoutTask = Task.Delay(5000, token); // 5 second timeout
                        
                        var completedTask = await Task.WhenAny(renderTask, timeoutTask);
                        
                        RenderResult renderResult;
                        if (completedTask == timeoutTask)
                        {
                            throw new TimeoutException("Rendering timeout after 5 seconds");
                        }
                        else
                        {
                            renderResult = await renderTask;
                        }

                        var renderTime = DateTime.UtcNow - startTime;
                        
                        // Log only significant events or errors
                        if (!renderResult.Success || renderTime.TotalMilliseconds > 1000)
                        {
                            System.Diagnostics.Trace.WriteLine($"[PageViewport] Render completed - Success: {renderResult.Success}, Time: {renderTime.TotalMilliseconds:F0}ms, Objects: {objs.Count()}");
                        }

                        if (!renderResult.Success && !string.IsNullOrEmpty(renderResult.ErrorMessage))
                        {
                            _lastError = renderResult.ErrorMessage;
                        }
                        else
                        {
                            _lastError = null;
                        }
                    }

                    // Check if superseded
                    bool wasSuperseded;
                    lock (_renderLock)
                    {
                        wasSuperseded = _renderCts != localCts;
                    }

                    if (!wasSuperseded)
                    {
                        // Update cache info
                        _lastRenderCacheKey = GenerateRenderCacheKey();
                        _lastRenderWidth = w;
                        _lastRenderHeight = h;
                        _lastRenderTime = DateTime.UtcNow;
                        
                        // Swap buffers
                        var old = Interlocked.Exchange(ref _backBuffer, bmp);
                        old?.Dispose();
                        bmp = null; // Prevent disposal in finally block
                        
                        if (!IsDisposed && !token.IsCancellationRequested)
                        {
                            try { 
                                if (InvokeRequired) {
                                    BeginInvoke((Action)Invalidate);
                                } else {
                                    Invalidate();
                                }
                            } catch { }
                        }
                    }
                    else
                    {
                        if (!IsDisposed && !token.IsCancellationRequested)
                        {
                            try { 
                                if (InvokeRequired) {
                                    BeginInvoke((Action)Invalidate);
                                } else {
                                    Invalidate();
                                }
                            } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                    try { 
                        if (!IsDisposed) {
                            if (InvokeRequired) {
                                BeginInvoke((Action)Invalidate);
                            } else {
                                Invalidate();
                            }
                        }
                    } catch { }
                }
                finally
                {
                    // Clean up bitmap if it wasn't transferred to _backBuffer
                    bmp?.Dispose();
                }

                // Check for pending work
                lock (_renderLock)
                {
                    if (!_renderDirty)
                    {
                        if (_pendingReason != RenderRequestReason.None)
                        {
                            _pendingReason = RenderRequestReason.None;
                            _renderDirty = true;
                        }
                        else
                        {
                            _isRendering = false;
                            return;
                        }
                    }
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
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

        private string GenerateRenderCacheKey()
        {
            var objsCount = _pageObjects?.Count() ?? 0;
            var engineHash = _renderingEngine?.GetHashCode() ?? 0;
            var ctxHash = _renderContext?.GetHashCode() ?? 0;
            return $"{objsCount}_{engineHash}_{ctxHash}_{_zoom:F3}";
        }

        private bool IsSameRenderContext(RenderContext newCtx)
        {
            if (_renderContext == null) return false;
            return Math.Abs(_renderContext.ScaleFactor - newCtx.ScaleFactor) < 0.001 &&
                   _renderContext.DpiX == newCtx.DpiX &&
                   _renderContext.DpiY == newCtx.DpiY &&
                   _renderContext.ViewPort.Equals(newCtx.ViewPort);
        }
    }
}

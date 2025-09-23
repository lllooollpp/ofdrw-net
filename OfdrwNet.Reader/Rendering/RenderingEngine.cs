using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using OfdrwNet.Reader.Model;

namespace OfdrwNet.Reader.Rendering
{
    /// <summary>
    /// 主渲染引擎
    /// 协调各种渲染器完成页面渲染
    /// </summary>
    public class RenderingEngine : IDisposable
    {
        private readonly TextRenderer _textRenderer;
        private readonly ImageRenderer _imageRenderer;
        private readonly VectorRenderer _vectorRenderer;
        private readonly IResourceManager _resourceManager;
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceManager">资源管理器</param>
        public RenderingEngine(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            _textRenderer = new TextRenderer(_resourceManager);
            _imageRenderer = new ImageRenderer(_resourceManager);
            _vectorRenderer = new VectorRenderer(_resourceManager);
        }

        /// <summary>
        /// 异步渲染整个页面
        /// </summary>
        /// <param name="pageObjects">页面对象列表</param>
        /// <param name="graphics">图形上下文</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>渲染结果</returns>
        public async Task<RenderResult> RenderPageAsync(IEnumerable<RenderObject> pageObjects, Graphics graphics, RenderContext renderContext)
        {
            if (pageObjects == null || graphics == null || renderContext == null)
            {
                return new RenderResult { Success = false, ErrorMessage = "参数不能为空" };
            }

            var startTime = DateTime.Now;
            var result = new RenderResult();
            var renderStats = new RenderStatistics();

            try
            {
                // 保存原始图形状态
                var originalState = graphics.Save();

                // 设置全局渲染质量
                ConfigureGraphicsQuality(graphics, renderContext);

                // 应用视口变换
                ApplyViewportTransform(graphics, renderContext);

                // 按层级排序对象
                var sortedObjects = SortObjectsByZIndex(pageObjects);

                // 逐个渲染对象
                foreach (var obj in sortedObjects)
                {
                    try
                    {
                        var objectStartTime = DateTime.Now;
                        var success = await RenderObjectAsync(obj, graphics, renderContext);
                        var objectEndTime = DateTime.Now;

                        // 更新统计信息
                        renderStats.ObjectCount++;
                        renderStats.TotalRenderTime += (objectEndTime - objectStartTime);

                        if (success)
                        {
                            renderStats.SuccessfulObjects++;
                        }
                        else
                        {
                            renderStats.FailedObjects++;
                            result.AddWarning($"对象渲染失败: {obj.Id}");
                        }
                    }
                    catch (Exception ex)
                    {
                        renderStats.FailedObjects++;
                        result.AddError($"对象渲染异常: {obj.Id} - {ex.Message}");
                    }
                }

                // 恢复原始图形状态
                graphics.Restore(originalState);

                var endTime = DateTime.Now;
                renderStats.TotalPageRenderTime = endTime - startTime;

                result.Success = renderStats.FailedObjects == 0;
                result.Statistics = renderStats;

                return result;
            }
            catch (Exception ex)
            {
                return new RenderResult
                {
                    Success = false,
                    ErrorMessage = $"页面渲染失败: {ex.Message}",
                    Statistics = renderStats
                };
            }
        }

        /// <summary>
        /// 异步渲染单个对象
        /// </summary>
        /// <param name="renderObject">渲染对象</param>
        /// <param name="graphics">图形上下文</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>渲染是否成功</returns>
        public async Task<bool> RenderObjectAsync(RenderObject renderObject, Graphics graphics, RenderContext renderContext)
        {
            if (renderObject == null || !renderObject.Visible)
                return true;

            return renderObject switch
            {
                TextObject textObj => await _textRenderer.RenderAsync(textObj, graphics, renderContext),
                ImageObject imageObj => await _imageRenderer.RenderAsync(imageObj, graphics, renderContext),
                VectorObject vectorObj => await _vectorRenderer.RenderAsync(vectorObj, graphics, renderContext),
                _ => false
            };
        }

        /// <summary>
        /// 批量命中测试
        /// </summary>
        /// <param name="pageObjects">页面对象列表</param>
        /// <param name="point">测试点</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>命中的对象列表</returns>
        public async Task<List<RenderObject>> HitTestAsync(IEnumerable<RenderObject> pageObjects, Point point, RenderContext renderContext)
        {
            var hitObjects = new List<RenderObject>();

            if (pageObjects == null)
                return hitObjects;

            // 按Z-Index倒序检测（从顶层到底层）
            var sortedObjects = SortObjectsByZIndex(pageObjects, descending: true);

            foreach (var obj in sortedObjects)
            {
                var hit = await HitTestObjectAsync(obj, point, renderContext);
                if (hit)
                {
                    hitObjects.Add(obj);
                }
            }

            return hitObjects;
        }

        /// <summary>
        /// 单个对象命中测试
        /// </summary>
        /// <param name="renderObject">渲染对象</param>
        /// <param name="point">测试点</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>是否命中</returns>
        public async Task<bool> HitTestObjectAsync(RenderObject renderObject, Point point, RenderContext renderContext)
        {
            if (renderObject == null || !renderObject.Visible)
                return false;

            return renderObject switch
            {
                TextObject textObj => await _textRenderer.HitTestAsync(textObj, point, renderContext),
                ImageObject imageObj => await _imageRenderer.HitTestAsync(imageObj, point, renderContext),
                VectorObject vectorObj => await _vectorRenderer.HitTestAsync(vectorObj, point, renderContext),
                _ => false
            };
        }

        /// <summary>
        /// 获取对象边界框
        /// </summary>
        /// <param name="renderObject">渲染对象</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>边界框</returns>
        public async Task<Rectangle> GetObjectBoundsAsync(RenderObject renderObject, RenderContext renderContext)
        {
            if (renderObject == null)
                return Rectangle.Empty;

            return renderObject switch
            {
                TextObject textObj => await _textRenderer.GetBoundsAsync(textObj, renderContext),
                ImageObject imageObj => await _imageRenderer.GetBoundsAsync(imageObj, renderContext),
                VectorObject vectorObj => await _vectorRenderer.GetBoundsAsync(vectorObj, renderContext),
                _ => renderObject.Boundary
            };
        }

        /// <summary>
        /// 预加载页面资源
        /// </summary>
        /// <param name="pageObjects">页面对象列表</param>
        /// <param name="renderContext">渲染上下文</param>
        /// <returns>预加载任务</returns>
        public async Task PreloadResourcesAsync(IEnumerable<RenderObject> pageObjects, RenderContext renderContext)
        {
            if (pageObjects == null)
                return;

            var imageObjects = new List<ImageObject>();

            foreach (var obj in pageObjects)
            {
                if (obj is ImageObject imageObj)
                {
                    imageObjects.Add(imageObj);
                }
            }

            if (imageObjects.Count > 0)
            {
                await _imageRenderer.PreloadImagesAsync(imageObjects, renderContext);
            }
        }

        // 私有辅助方法

        /// <summary>
        /// 配置图形质量设置
        /// </summary>
        private void ConfigureGraphicsQuality(Graphics graphics, RenderContext renderContext)
        {
            graphics.SmoothingMode = renderContext.SmoothingMode;
            graphics.InterpolationMode = renderContext.InterpolationMode;
            graphics.CompositingQuality = renderContext.CompositingQuality;
            graphics.TextRenderingHint = renderContext.TextRenderingHint;
        }

        /// <summary>
        /// 应用视口变换
        /// </summary>
        private void ApplyViewportTransform(Graphics graphics, RenderContext renderContext)
        {
            // 应用缩放
            if (renderContext.ScaleFactor != 1.0)
            {
                graphics.ScaleTransform((float)renderContext.ScaleFactor, (float)renderContext.ScaleFactor);
            }

            // 应用变换矩阵
            if (renderContext.TransformMatrix != null)
            {
                graphics.MultiplyTransform(renderContext.TransformMatrix);
            }

            // 应用剪切区域
            if (!renderContext.ClipRegion.IsEmpty)
            {
                graphics.SetClip(renderContext.ClipRegion);
            }
        }

        /// <summary>
        /// 按Z-Index排序对象
        /// </summary>
        private List<RenderObject> SortObjectsByZIndex(IEnumerable<RenderObject> objects, bool descending = false)
        {
            var list = new List<RenderObject>(objects);

            if (descending)
            {
                list.Sort((a, b) => b.ZIndex.CompareTo(a.ZIndex));
            }
            else
            {
                list.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
            }

            return list;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _textRenderer?.Dispose();
                _imageRenderer?.Dispose();
                _vectorRenderer?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 渲染结果
    /// </summary>
    public class RenderResult
    {
        /// <summary>渲染是否成功</summary>
        public bool Success { get; set; }

        /// <summary>错误消息</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>警告消息列表</summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>错误消息列表</summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>渲染统计信息</summary>
        public RenderStatistics? Statistics { get; set; }

        /// <summary>添加警告</summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }

        /// <summary>添加错误</summary>
        public void AddError(string error)
        {
            Errors.Add(error);
        }

        /// <summary>是否有警告或错误</summary>
        public bool HasIssues => Warnings.Count > 0 || Errors.Count > 0;
    }
}

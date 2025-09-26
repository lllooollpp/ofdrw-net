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
            System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 开始页面渲染");

            if (pageObjects == null || graphics == null || renderContext == null)
            {
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 参数验证失败");
                return new RenderResult { Success = false, ErrorMessage = "参数不能为空" };
            }

            var pageObjectsList = pageObjects.ToList();
            System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 页面对象数量: {pageObjectsList.Count}");

            var startTime = DateTime.Now;
            var result = new RenderResult();
            var renderStats = new RenderStatistics();

            try
            {
                // 保存原始图形状态
                var originalState = graphics.Save();
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 保存图形状态");

                // 设置全局渲染质量
                ConfigureGraphicsQuality(graphics, renderContext);
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 配置图形质量");

                // 应用视口变换
                ApplyViewportTransform(graphics, renderContext);
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 应用视口变换，缩放: {renderContext.ScaleFactor}");

                // 按层级排序对象
                var sortedObjects = SortObjectsByZIndex(pageObjectsList);
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 对象排序完成");

                // 逐个渲染对象
                foreach (var obj in sortedObjects)
                {
                    try
                    {
                        System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 开始渲染对象: {obj.Id} (类型: {obj.GetType().Name})");
                        var objectStartTime = DateTime.Now;
                        var success = await RenderObjectAsync(obj, graphics, renderContext);
                        var objectEndTime = DateTime.Now;

                        // 更新统计信息
                        renderStats.ObjectCount++;
                        renderStats.TotalRenderTime += (objectEndTime - objectStartTime);

                        if (success)
                        {
                            renderStats.SuccessfulObjects++;
                            System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 对象渲染成功: {obj.Id}");
                        }
                        else
                        {
                            renderStats.FailedObjects++;
                            var warning = $"对象渲染失败: {obj.Id} (Type: {obj.GetType().Name})";
                            result.AddWarning(warning);
                            System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 对象渲染失败: {obj.Id} (类型: {obj.GetType().Name})");
                        }
                    }
                    catch (Exception ex)
                    {
                        renderStats.FailedObjects++;
                        var error = $"对象渲染异常: {obj.Id} - {ex.Message}";
                        result.AddError(error);
                        System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 对象渲染异常: {obj.Id} - {ex.Message}");
                        System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 异常详情: {ex}");
                    }
                }

                // 恢复原始图形状态
                graphics.Restore(originalState);
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 恢复图形状态");

                var endTime = DateTime.Now;
                renderStats.TotalPageRenderTime = endTime - startTime;

                result.Success = renderStats.FailedObjects == 0;
                result.Statistics = renderStats;

                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 页面渲染完成 - 总时间: {renderStats.TotalPageRenderTime.TotalMilliseconds}ms, 成功: {result.Success}");

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 页面渲染严重异常: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 异常详情: {ex}");
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
            {
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 对象为空或不可见，跳过");
                return true;
            }

            System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 渲染对象类型: {renderObject.GetType().FullName}");

            try
            {
                bool success = renderObject switch
                {
                    TextObject textObj => await _textRenderer.RenderAsync(textObj, graphics, renderContext),
                    ImageObject imageObj => await _imageRenderer.RenderAsync(imageObj, graphics, renderContext),
                    VectorObject vectorObj => await _vectorRenderer.RenderAsync(vectorObj, graphics, renderContext),
                    _ => HandleUnknownObjectType(renderObject)
                };

                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 对象 {renderObject.Id} 渲染结果: {success}");
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 对象 {renderObject.Id} 渲染异常: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 异常详情: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 处理未知对象类型
        /// </summary>
        private bool HandleUnknownObjectType(RenderObject renderObject)
        {
            System.Diagnostics.Trace.WriteLine($"[RenderingEngine] 未知对象类型，无法渲染: {renderObject.GetType().FullName}");
            return false;
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
                _ => Rectangle.Round(renderObject.Boundary)
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
            bool unified = false;
            try
            {
                var t = Type.GetType("OfdrwNet.Reader.Rendering.RenderingConfig");
                if (t != null)
                {
                    var p = t.GetProperty("UnifiedScalingMode");
                    if (p != null) unified = (bool)p.GetValue(null)!;
                }
            }
            catch { }

            if (unified)
            {
                var factor = (float)(renderContext.Ppm * renderContext.ScaleFactor);
                if (Math.Abs(factor - 1f) > 0.0001f)
                {
                    graphics.ScaleTransform(factor, factor);
                }
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
}

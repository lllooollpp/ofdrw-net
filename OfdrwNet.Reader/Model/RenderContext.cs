using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace OfdrwNet.Reader.Model
{
    /// <summary>
    /// 渲染上下文
    /// 管理渲染参数、图形状态和性能指标
    /// </summary>
    public class RenderContext
    {
        /// <summary>
        /// 缩放因子，默认为1.0 (100%)
        /// </summary>
        public double ScaleFactor { get; set; } = 1.0;

        /// <summary>
        /// 水平DPI，默认为96.0
        /// </summary>
        public double DpiX { get; set; } = 96.0;

        /// <summary>
        /// 垂直DPI，默认为96.0
        /// </summary>
        public double DpiY { get; set; } = 96.0;

        /// <summary>
        /// 每毫米像素数，默认为7.874 (约200 DPI)
        /// </summary>
        public double Ppm { get; set; } = 7.874;

        /// <summary>
        /// 视口区域
        /// </summary>
        public Rectangle ViewPort { get; set; }

        /// <summary>
        /// 变换矩阵
        /// </summary>
        public Matrix? TransformMatrix { get; set; }

        /// <summary>
        /// 剪切区域
        /// </summary>
        public Rectangle ClipRegion { get; set; }

        /// <summary>
        /// 文本渲染提示
        /// </summary>
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.AntiAlias;

        /// <summary>
        /// 平滑模式
        /// </summary>
        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.HighQuality;

        /// <summary>
        /// 插值模式
        /// </summary>
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.HighQualityBicubic;

        /// <summary>
        /// 图像插值模式
        /// </summary>
        public InterpolationMode ImageInterpolationMode { get; set; } = InterpolationMode.HighQualityBicubic;

        /// <summary>
        /// 合成质量
        /// </summary>
        public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.HighQuality;

        /// <summary>
        /// 图像质量
        /// </summary>
        public Rendering.ImageQuality ImageQuality { get; set; } = Rendering.ImageQuality.High;

        /// <summary>
        /// 性能指标
        /// </summary>
        public RenderMetrics? Metrics { get; set; }

        /// <summary>
        /// 自定义属性
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public RenderContext()
        {
            ViewPort = Rectangle.Empty;
            ClipRegion = Rectangle.Empty;
        }

        /// <summary>
        /// 创建默认渲染上下文
        /// </summary>
        /// <returns>默认渲染上下文</returns>
        public static RenderContext CreateDefault()
        {
            return new RenderContext
            {
                ScaleFactor = 1.0,
                DpiX = 96.0,
                DpiY = 96.0,
                Ppm = 7.874,
                TextRenderingHint = TextRenderingHint.AntiAlias,
                SmoothingMode = SmoothingMode.HighQuality,
                InterpolationMode = InterpolationMode.HighQualityBicubic
            };
        }

        /// <summary>
        /// 创建高质量渲染上下文
        /// </summary>
        /// <returns>高质量渲染上下文</returns>
        public static RenderContext CreateHighQuality()
        {
            return new RenderContext
            {
                ScaleFactor = 1.0,
                DpiX = 300.0, // 高DPI
                DpiY = 300.0,
                Ppm = 11.811, // 300 DPI对应的PPM
                TextRenderingHint = TextRenderingHint.ClearTypeGridFit,
                SmoothingMode = SmoothingMode.HighQuality,
                InterpolationMode = InterpolationMode.HighQualityBicubic
            };
        }

        /// <summary>
        /// 创建快速渲染上下文
        /// </summary>
        /// <returns>快速渲染上下文</returns>
        public static RenderContext CreateFast()
        {
            return new RenderContext
            {
                ScaleFactor = 1.0,
                DpiX = 96.0,
                DpiY = 96.0,
                Ppm = 7.874,
                TextRenderingHint = TextRenderingHint.SingleBitPerPixel,
                SmoothingMode = SmoothingMode.HighSpeed,
                InterpolationMode = InterpolationMode.Low
            };
        }

        /// <summary>
        /// 计算毫米到像素的转换
        /// </summary>
        /// <param name="millimeters">毫米值</param>
        /// <returns>像素值</returns>
        public double MillimetersToPixels(double millimeters)
        {
            return millimeters * Ppm * ScaleFactor;
        }

        /// <summary>
        /// 计算像素到毫米的转换
        /// </summary>
        /// <param name="pixels">像素值</param>
        /// <returns>毫米值</returns>
        public double PixelsToMillimeters(double pixels)
        {
            return pixels / (Ppm * ScaleFactor);
        }

        /// <summary>
        /// 更新DPI设置
        /// </summary>
        /// <param name="dpiX">水平DPI</param>
        /// <param name="dpiY">垂直DPI</param>
        public void UpdateDpi(double dpiX, double dpiY)
        {
            DpiX = dpiX;
            DpiY = dpiY;
            Ppm = dpiX / 25.4; // 1英寸 = 25.4毫米
        }

        /// <summary>
        /// 应用变换矩阵
        /// </summary>
        /// <param name="matrix">变换矩阵</param>
        public void ApplyTransform(Matrix matrix)
        {
            if (TransformMatrix == null)
            {
                TransformMatrix = matrix.Clone();
            }
            else
            {
                TransformMatrix.Multiply(matrix);
            }
        }

        /// <summary>
        /// 重置变换矩阵
        /// </summary>
        public void ResetTransform()
        {
            TransformMatrix?.Dispose();
            TransformMatrix = null;
        }

        /// <summary>
        /// 克隆渲染上下文
        /// </summary>
        /// <returns>克隆的渲染上下文</returns>
        public RenderContext Clone()
        {
            var cloned = new RenderContext
            {
                ScaleFactor = ScaleFactor,
                DpiX = DpiX,
                DpiY = DpiY,
                Ppm = Ppm,
                ViewPort = ViewPort,
                ClipRegion = ClipRegion,
                TextRenderingHint = TextRenderingHint,
                SmoothingMode = SmoothingMode,
                InterpolationMode = InterpolationMode,
                Properties = new Dictionary<string, object>(Properties)
            };

            if (TransformMatrix != null)
            {
                cloned.TransformMatrix = TransformMatrix.Clone();
            }

            return cloned;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            TransformMatrix?.Dispose();
            TransformMatrix = null;
        }
    }

    /// <summary>
    /// 渲染性能指标
    /// </summary>
    public class RenderMetrics
    {
        /// <summary>
        /// 解析耗时
        /// </summary>
        public TimeSpan ParseDuration { get; set; }

        /// <summary>
        /// 渲染耗时
        /// </summary>
        public TimeSpan RenderDuration { get; set; }

        /// <summary>
        /// 对象数量
        /// </summary>
        public int ObjectCount { get; set; }

        /// <summary>
        /// 内存使用量（字节）
        /// </summary>
        public long MemoryUsed { get; set; }

        /// <summary>
        /// 自定义性能计数器
        /// </summary>
        public Dictionary<string, double> PerformanceCounters { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public RenderMetrics()
        {
        }

        /// <summary>
        /// 获取每秒渲染对象数
        /// </summary>
        /// <returns>每秒对象数</returns>
        public double GetObjectsPerSecond()
        {
            if (RenderDuration.TotalSeconds <= 0)
                return 0;

            return ObjectCount / RenderDuration.TotalSeconds;
        }

        /// <summary>
        /// 获取每对象平均渲染时间（毫秒）
        /// </summary>
        /// <returns>平均时间</returns>
        public double GetAverageTimePerObjectMs()
        {
            if (ObjectCount <= 0)
                return 0;

            return RenderDuration.TotalMilliseconds / ObjectCount;
        }

        /// <summary>
        /// 添加性能计数器
        /// </summary>
        /// <param name="name">计数器名称</param>
        /// <param name="value">计数器值</param>
        public void AddCounter(string name, double value)
        {
            PerformanceCounters[name] = value;
        }

        /// <summary>
        /// 获取性能摘要
        /// </summary>
        /// <returns>性能摘要字符串</returns>
        public string GetSummary()
        {
            return $"Objects: {ObjectCount}, " +
                   $"Parse: {ParseDuration.TotalMilliseconds:F1}ms, " +
                   $"Render: {RenderDuration.TotalMilliseconds:F1}ms, " +
                   $"Memory: {MemoryUsed / 1024.0:F1}KB, " +
                   $"OPS: {GetObjectsPerSecond():F1}";
        }
    }
}

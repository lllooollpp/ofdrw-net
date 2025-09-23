using System;
using System.Collections.Generic;
using System.Linq;

namespace OfdrwNet.Reader
{
    /// <summary>
    /// 渲染统计信息
    /// </summary>
    public class RenderStatistics
    {
        private readonly List<TimeSpan> _renderTimes = new List<TimeSpan>();
        private int _renderedPages = 0;
        private int _failedRenders = 0;
        private int _cancelledRenders = 0;
        private DateTime _startTime = DateTime.UtcNow;

        /// <summary>
        /// 已渲染页面数
        /// </summary>
        public int RenderedPages => _renderedPages;

        /// <summary>
        /// 失败渲染数
        /// </summary>
        public int FailedRenders => _failedRenders;

        /// <summary>
        /// 取消渲染数
        /// </summary>
        public int CancelledRenders => _cancelledRenders;

        /// <summary>
        /// 平均渲染时间
        /// </summary>
        public TimeSpan AverageRenderTime => _renderTimes.Count > 0 ?
            TimeSpan.FromMilliseconds(_renderTimes.Average(t => t.TotalMilliseconds)) :
            TimeSpan.Zero;

        /// <summary>
        /// 总渲染时间
        /// </summary>
        public TimeSpan TotalRenderTime => _renderTimes.Count > 0 ?
            TimeSpan.FromMilliseconds(_renderTimes.Sum(t => t.TotalMilliseconds)) :
            TimeSpan.Zero;

        /// <summary>
        /// 最快渲染时间
        /// </summary>
        public TimeSpan FastestRenderTime => _renderTimes.Count > 0 ? _renderTimes.Min() : TimeSpan.Zero;

        /// <summary>
        /// 最慢渲染时间
        /// </summary>
        public TimeSpan SlowestRenderTime => _renderTimes.Count > 0 ? _renderTimes.Max() : TimeSpan.Zero;

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate
        {
            get
            {
                var total = _renderedPages + _failedRenders + _cancelledRenders;
                return total > 0 ? (double)_renderedPages / total : 0.0;
            }
        }

        /// <summary>
        /// 记录渲染时间
        /// </summary>
        /// <param name="renderTime">渲染时间</param>
        public void RecordRenderTime(TimeSpan renderTime)
        {
            _renderTimes.Add(renderTime);

            // 保持最近的1000条记录
            if (_renderTimes.Count > 1000)
            {
                _renderTimes.RemoveAt(0);
            }
        }

        /// <summary>
        /// 增加已渲染页面数
        /// </summary>
        public void IncrementRenderedPages()
        {
            _renderedPages++;
        }

        /// <summary>
        /// 增加失败渲染数
        /// </summary>
        public void IncrementFailedRenders()
        {
            _failedRenders++;
        }

        /// <summary>
        /// 增加取消渲染数
        /// </summary>
        public void IncrementCancelledRenders()
        {
            _cancelledRenders++;
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void Reset()
        {
            _renderTimes.Clear();
            _renderedPages = 0;
            _failedRenders = 0;
            _cancelledRenders = 0;
            _startTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 获取统计摘要
        /// </summary>
        /// <returns>统计摘要字符串</returns>
        public string GetSummary()
        {
            var uptime = DateTime.UtcNow - _startTime;
            return $"渲染统计: 成功={RenderedPages}, 失败={FailedRenders}, 取消={CancelledRenders}, " +
                   $"成功率={SuccessRate:P2}, 平均耗时={AverageRenderTime.TotalMilliseconds:F1}ms, " +
                   $"运行时间={uptime.TotalMinutes:F1}分钟";
        }

        /// <summary>
        /// 获取详细统计信息
        /// </summary>
        /// <returns>详细统计信息</returns>
        public RenderStatisticsDetails GetDetails()
        {
            return new RenderStatisticsDetails
            {
                RenderedPages = RenderedPages,
                FailedRenders = FailedRenders,
                CancelledRenders = CancelledRenders,
                AverageRenderTime = AverageRenderTime,
                TotalRenderTime = TotalRenderTime,
                FastestRenderTime = FastestRenderTime,
                SlowestRenderTime = SlowestRenderTime,
                SuccessRate = SuccessRate,
                Uptime = DateTime.UtcNow - _startTime,
                RenderTimeHistory = new List<TimeSpan>(_renderTimes)
            };
        }
    }

    /// <summary>
    /// 详细渲染统计信息
    /// </summary>
    public class RenderStatisticsDetails
    {
        /// <summary>
        /// 已渲染页面数
        /// </summary>
        public int RenderedPages { get; set; }

        /// <summary>
        /// 失败渲染数
        /// </summary>
        public int FailedRenders { get; set; }

        /// <summary>
        /// 取消渲染数
        /// </summary>
        public int CancelledRenders { get; set; }

        /// <summary>
        /// 平均渲染时间
        /// </summary>
        public TimeSpan AverageRenderTime { get; set; }

        /// <summary>
        /// 总渲染时间
        /// </summary>
        public TimeSpan TotalRenderTime { get; set; }

        /// <summary>
        /// 最快渲染时间
        /// </summary>
        public TimeSpan FastestRenderTime { get; set; }

        /// <summary>
        /// 最慢渲染时间
        /// </summary>
        public TimeSpan SlowestRenderTime { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// 运行时间
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// 渲染时间历史记录
        /// </summary>
        public List<TimeSpan> RenderTimeHistory { get; set; } = new List<TimeSpan>();
    }
}

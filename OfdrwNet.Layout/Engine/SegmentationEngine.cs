using System.Collections.Generic;
using System.Linq;
using OfdrwNet.Layout.Element;

namespace OfdrwNet.Layout.Engine
{
    /// <summary>
    /// 分段引擎
    /// <para>
    /// 用于将流式文档中的元素划分为段。
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-02-29 11:39:29
    /// </summary>
    public class SegmentationEngine
    {
        private readonly PageLayout pageLayout;

        /// <summary>
        /// 创建分段引擎
        /// </summary>
        /// <param name="pageLayout">页面布局</param>
        public SegmentationEngine(PageLayout pageLayout)
        {
            this.pageLayout = pageLayout;
        }

        /// <summary>
        /// 将输入的流式布局元素队列分段
        /// </summary>
        /// <param name="streamLayoutQueue">流式布局元素队列</param>
        /// <returns>分完段的布局队列</returns>
        public List<Segment> Process(List<Div> streamLayoutQueue)
        {
            // 可用于布局的宽度
            var width = pageLayout.ContentWidth();
            if (streamLayoutQueue == null || !streamLayoutQueue.Any())
            {
                return new List<Segment>();
            }

            var res = new List<Segment>();
            var seq = new Queue<Div>(streamLayoutQueue);
            var segment = new Segment(width);

            while (seq.Count > 0)
            {
                var div = seq.Dequeue();
                if (div.GetPosition() == Position.Absolute)
                {
                    continue;
                }

                if (div is BR)
                {
                    if (segment.IsEmpty())
                    {
                        // 如果段为空，那么不需要换行
                        continue;
                    }
                    // 换行符
                    res.Add(segment);
                    segment = new Segment(width);
                    continue;
                }

                // 尝试将元素加入段中
                var addSuccess = segment.TryAdd(div);
                // 如果段已经满了，那么加入了段队列中
                if (!addSuccess && !segment.IsEmpty())
                {
                    // 段已经无法再容纳元素： 无法加入元素且不为空
                    res.Add(segment);
                    segment = new Segment(width);
                    // 重新处理当前元素
                    var tempList = new List<Div> { div };
                    tempList.AddRange(seq);
                    seq = new Queue<Div>(tempList);
                }
            }

            // 处理最后一个段的情况
            if (!segment.IsEmpty())
            {
                res.Add(segment);
            }

            return res;
        }
    }
}
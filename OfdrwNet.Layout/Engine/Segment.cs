using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OfdrwNet.Layout.Element;

namespace OfdrwNet.Layout.Engine
{
    /// <summary>
    /// 段对象
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-02-29 11:41:28
    /// </summary>
    public class Segment : IEnumerable<KeyValuePair<Div, Rectangle>>
    {
        /// <summary>
        /// 段中的内容
        /// </summary>
        private readonly List<Div> content;
        private readonly List<Rectangle> sizeList;

        /// <summary>
        /// 段高度
        /// </summary>
        private double height = 0d;

        /// <summary>
        /// 段最大宽度
        /// </summary>
        private readonly double width;

        /// <summary>
        /// 段剩余宽度
        /// </summary>
        private double remainWidth;

        /// <summary>
        /// 段是否可以分块
        /// <para>
        /// true - 可拆分； false - 不可拆分
        /// </para>
        /// <para>
        /// 默认值： false 不可拆分
        /// </para>
        /// </summary>
        private bool blockable = false;

        /// <summary>
        /// 剩余页面空间填充段
        /// <para>
        /// true - 填充剩余页面空间； false - 非填充段
        /// </para>
        /// </summary>
        private bool isRemainAreaFiller = false;

        /// <summary>
        /// 创建段对象
        /// </summary>
        /// <param name="width">段宽度</param>
        public Segment(double width)
        {
            this.width = width;
            remainWidth = width;
            content = new List<Div>(5);
            sizeList = new List<Rectangle>(5);
        }

        /// <summary>
        /// 获取段高度
        /// </summary>
        /// <returns>段高度</returns>
        public double GetHeight()
        {
            return height;
        }

        /// <summary>
        /// 向段中添加元素
        /// </summary>
        /// <param name="div">元素</param>
        /// <returns>元素是否能加入段中 true - 可加入；false - 不可加入</returns>
        public bool TryAdd(Div div)
        {
            if (div == null)
            {
                return true;
            }

            // 剩余空间已经不足
            // 可是是应为: 段内已经有元素，并且新加入的元素为独占，那么不能加入段中
            //            也可能是刚好空间耗尽
            if (Math.Abs(remainWidth) < 0.001)
            {
                return false;
            }

            // 根据段宽度重置加入元素尺寸
            var blockSize = div.DoPrepare(width);
            if (blockSize.GetWidth() > remainWidth)
            {
                // 段剩余宽度不足无法放入元素，舍弃
                return false;
            }

            // 上面过程保证了元素一定可以加入到段中
            /*
            独占段的元素类型
            1. 独占
            2. 浮动 + Clear 对立
             */
            if (div.IsBlockElement())
            {
                if (!IsEmpty())
                {
                    // 独占类型如果已经存在元素那么则无法加入
                    return false;
                }
                remainWidth = 0;
                Add(div, blockSize);
                return true;
            }

            if (!IsEmpty())
            {
                if (IsCenterFloat())
                {
                    // 段内含有居中元素:
                    // 1. 新加入元素非居中元素
                    // 2. 新加入元素设置 clear left
                    // 3. 最后一个元素设置 clear right
                    if (div.GetFloat() != AFloat.Center)
                    {
                        return false;
                    }
                    if (Clear.Left == div.GetClear())
                    {
                        return false;
                    }
                    if (Clear.Right == content[^1].GetClear())
                    {
                        return false;
                    }
                }
                else
                {
                    // 段内不含居中元素:
                    // 1. Float == Clear Left: 段内已经含有左浮动元素
                    // 2. Float == Clear Right: 段内已经含有右浮动元素
                    // 3. 新加入元素为居中元素
                    if (AFloat.Left == div.GetFloat() && Clear.Left == div.GetClear())
                    {
                        foreach (var existDiv in content)
                        {
                            if (existDiv.GetFloat() == AFloat.Left)
                            {
                                return false;
                            }
                        }
                    }
                    if (AFloat.Right == div.GetFloat() && Clear.Right == div.GetClear())
                    {
                        foreach (var existDiv in content)
                        {
                            if (existDiv.GetFloat() == AFloat.Right)
                            {
                                return false;
                            }
                        }
                    }
                    if (AFloat.Center == div.GetFloat())
                    {
                        return false;
                    }
                }
            }
            // 段内不含元素时直接加入

            remainWidth -= blockSize.GetWidth();
            Add(div, blockSize);
            return true;
        }

        /// <summary>
        /// 加入元素
        /// </summary>
        /// <param name="div">元素本身</param>
        /// <param name="blockSize">元素尺寸</param>
        private void Add(Div div, Rectangle blockSize)
        {
            if (height < blockSize.GetHeight())
            {
                height = blockSize.GetHeight();
            }
            if (div.IsIntegrity() == false)
            {
                // 判断是否可以拆分段，只要出现了一个可拆分的，那么该段就是可以拆分
                blockable = true;
            }
            if (div is PageAreaFiller)
            {
                isRemainAreaFiller = true;
            }
            content.Add(div);
            sizeList.Add(blockSize);
        }

        /// <summary>
        /// 段是否可拆分
        /// </summary>
        /// <returns>true - 可拆分；false - 不可拆分</returns>
        public bool IsBlockable()
        {
            return blockable;
        }

        /// <summary>
        /// 判断段内是否为居中布局
        /// </summary>
        /// <returns>true - 居中布局；false - 不居中</returns>
        private bool IsCenterFloat()
        {
            if (IsEmpty())
            {
                return true;
            }
            return content.All(d => d.GetFloat() == AFloat.Center);
        }

        /// <summary>
        /// 判断段是否为空
        /// </summary>
        /// <returns>true - 空段；false - 非空段</returns>
        public bool IsEmpty()
        {
            return content.Count == 0;
        }

        /// <summary>
        /// 获取段中元素数量
        /// </summary>
        /// <returns>元素数量</returns>
        public int Size()
        {
            return content.Count;
        }

        /// <summary>
        /// 获取段宽度
        /// </summary>
        /// <returns>段宽度</returns>
        public double GetWidth()
        {
            return width;
        }

        /// <summary>
        /// 获取剩余宽度
        /// </summary>
        /// <returns>剩余宽度</returns>
        public double GetRemainWidth()
        {
            return remainWidth;
        }

        /// <summary>
        /// 是否为剩余区域填充段
        /// </summary>
        /// <returns>true - 填充段；false - 非填充段</returns>
        public bool IsRemainAreaFiller()
        {
            return isRemainAreaFiller;
        }

        /// <summary>
        /// 获取内容列表
        /// </summary>
        /// <returns>内容列表</returns>
        public List<Div> GetContent()
        {
            return new List<Div>(content);
        }

        /// <summary>
        /// 获取尺寸列表
        /// </summary>
        /// <returns>尺寸列表</returns>
        public List<Rectangle> GetSizeList()
        {
            return new List<Rectangle>(sizeList);
        }

        /// <summary>
        /// 实现可枚举接口
        /// </summary>
        /// <returns>枚举器</returns>
        public IEnumerator<KeyValuePair<Div, Rectangle>> GetEnumerator()
        {
            for (var i = 0; i < content.Count; i++)
            {
                yield return new KeyValuePair<Div, Rectangle>(content[i], sizeList[i]);
            }
        }

        /// <summary>
        /// 实现可枚举接口
        /// </summary>
        /// <returns>枚举器</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
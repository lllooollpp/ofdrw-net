using System;
using System.Collections.Generic;
using System.Xml.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Graph.Tight.Method;

namespace OfdrwNet.Core.Graph.Tight;

/// <summary>
/// 区域由一系列的分路径（Area）组成，每个路径都是闭合的
/// 
/// 图 49 区域结构
/// 
/// 对应Java版本的 org.ofdrw.core.graph.tight.CT_Area
/// </summary>
public class CtArea : OfdElement
{
    public CtArea(XElement element) : base(element)
    {
    }

    public CtArea() : base("Area")
    {
    }

    /// <summary>
    /// 【必选 属性】
    /// 设置定义字图形的起始点坐标
    /// </summary>
    /// <param name="start">定义字图形的起始点坐标</param>
    /// <returns>this</returns>
    public CtArea SetStart(StPos start)
    {
        SetAttribute("Start", start.ToString());
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取定义字图形的起始点坐标
    /// </summary>
    /// <returns>定义字图形的起始点坐标</returns>
    public StPos GetStart()
    {
        var value = GetAttributeValue("Start");
        return string.IsNullOrEmpty(value) ? throw new InvalidOperationException("Start is required") : StPos.Parse(value);
    }

    /// <summary>
    /// 【必选】
    /// 增加绘制指令
    /// 
    /// 移动点、画线、画圆弧等
    /// </summary>
    /// <param name="command">绘制指令</param>
    /// <returns>this</returns>
    public CtArea AddCommand(ICommand command)
    {
        if (command is OfdElement element)
        {
            Add(element);
        }
        return this;
    }

    /// <summary>
    /// 连续绘制
    /// </summary>
    /// <param name="command">绘制指令</param>
    /// <returns>this</returns>
    public CtArea Next(ICommand command)
    {
        return AddCommand(command);
    }

    /// <summary>
    /// 【必选】
    /// 获取绘制指令序列（顺序决定了绘制图形）
    /// 
    /// 移动点、画线、画圆弧等
    /// </summary>
    /// <returns>绘制指令序列</returns>
    public List<ICommand> GetCommands()
    {
        var elementList = Element.Elements().ToList();
        var res = new List<ICommand>(elementList.Count);
        
        foreach (var item in elementList)
        {
            var qName = item.Name.LocalName;
            switch (qName)
            {
                case "Move":
                    res.Add(new MoveCommand(item));
                    break;
                case "Line":
                    res.Add(new LineCommand(item));
                    break;
                case "QuadraticBezier":
                    res.Add(new QuadraticBezierCommand(item));
                    break;
                case "CubicBezier":
                    res.Add(new CubicBezierCommand(item));
                    break;
                case "Arc":
                    res.Add(new ArcCommand(item));
                    break;
                case "Close":
                    res.Add(new CloseCommand(item));
                    break;
                default:
                    throw new ArgumentException($"未知类型：{qName}");
            }
        }
        
        return res;
    }
}
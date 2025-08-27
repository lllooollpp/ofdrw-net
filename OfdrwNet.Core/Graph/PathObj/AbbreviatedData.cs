using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.Graph.PathObj;

/// <summary>
/// 图形轮廓数据
/// 
/// 由一系列的紧缩的操作符和操作数构成
/// 
/// 9.1 表 35 36
/// 
/// 对应Java版本的 org.ofdrw.core.graph.pathObj.AbbreviatedData
/// </summary>
public class AbbreviatedData : OfdElement, ICloneable, IEnumerable<OptVal>
{
    /// <summary>
    /// 绘制数据队列
    /// </summary>
    private LinkedList<OptVal> _dataQueue;

    /// <summary>
    /// 从XML元素构造
    /// </summary>
    /// <param name="element">XML元素</param>
    public AbbreviatedData(XElement element) : base(element)
    {
        _dataQueue = Parse(element.Value);
    }

    /// <summary>
    /// 构造新实例
    /// </summary>
    public AbbreviatedData() : base("AbbreviatedData")
    {
        _dataQueue = new LinkedList<OptVal>();
    }

    /// <summary>
    /// 从列表构造
    /// </summary>
    /// <param name="list">操作符和操作数列表</param>
    public AbbreviatedData(List<OptVal> list) : this()
    {
        _dataQueue = new LinkedList<OptVal>(list);
    }

    /// <summary>
    /// 解析字符串构造数据队列
    /// 
    /// 数组中的两个元素间多个空格将会被理解成一个空格。
    /// 无法解析转换的数字将会被当做0
    /// </summary>
    /// <param name="dataStr">紧缩字符串</param>
    /// <returns>数据队列</returns>
    public static LinkedList<OptVal> Parse(string? dataStr)
    {
        if (string.IsNullOrEmpty(dataStr))
        {
            return new LinkedList<OptVal>();
        }

        var arr = dataStr.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var res = new LinkedList<OptVal>();
        string? opt = null;
        var values = new List<string>();

        foreach (var s in arr)
        {
            switch (s)
            {
                case "S":
                case "M":
                case "L":
                case "Q":
                case "B":
                case "A":
                case "C":
                case "CM":
                    if (opt != null)
                    {
                        res.AddLast(new OptVal(opt, values));
                        values.Clear();
                        opt = null;
                    }
                    opt = s;
                    break;
                default:
                    values.Add(s);
                    break;
            }
        }

        if (opt != null)
        {
            res.AddLast(new OptVal(opt, values));
        }

        return res;
    }

    /// <summary>
    /// 追加整个图形轮廓数据
    /// </summary>
    /// <param name="another">图形轮廓数据</param>
    /// <returns>this</returns>
    public AbbreviatedData Append(AbbreviatedData another)
    {
        if (another != null)
        {
            foreach (var item in another._dataQueue)
            {
                _dataQueue.AddLast(item);
            }
        }
        return this;
    }

    /// <summary>
    /// 获取原始的操作符和操作数
    /// </summary>
    /// <returns>操作符和操作数的结合</returns>
    public LinkedList<OptVal> GetRawOptVal()
    {
        return _dataQueue;
    }

    /// <summary>
    /// 刷新元素
    /// 
    /// 默认情况下，每次调用C都将会刷新元素内容
    /// </summary>
    /// <returns>this</returns>
    public AbbreviatedData Flush()
    {
        SetText(ToString());
        return this;
    }

    /// <summary>
    /// 定义自绘制图形边线的起始点坐标 (x，y)
    /// </summary>
    /// <param name="x">目标点 x</param>
    /// <param name="y">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData DefineStart(double x, double y)
    {
        _dataQueue.AddLast(new OptVal("S", new double[] { x, y }));
        return this;
    }

    /// <summary>
    /// 定义自绘制图形边线的起始点坐标
    /// </summary>
    /// <param name="start">起始点</param>
    /// <returns>this</returns>
    public AbbreviatedData S(StPos start)
    {
        return DefineStart(start.X, start.Y);
    }

    /// <summary>
    /// 定义自绘制图形边线的起始点坐标
    /// </summary>
    /// <param name="x">目标点 x</param>
    /// <param name="y">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData S(double x, double y)
    {
        return DefineStart(x, y);
    }

    /// <summary>
    /// 当前点移动到指定点（x，y）
    /// </summary>
    /// <param name="x">目标点 x</param>
    /// <param name="y">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData MoveTo(double x, double y)
    {
        _dataQueue.AddLast(new OptVal("M", new double[] { x, y }));
        return this;
    }

    /// <summary>
    /// 当前点移动到指定点
    /// </summary>
    /// <param name="x">目标点 x</param>
    /// <param name="y">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData M(double x, double y)
    {
        return MoveTo(x, y);
    }

    /// <summary>
    /// 当前点移动到指定点
    /// </summary>
    /// <param name="target">目标点</param>
    /// <returns>this</returns>
    public AbbreviatedData M(StPos target)
    {
        return MoveTo(target.X, target.Y);
    }

    /// <summary>
    /// 从当前点连接一条到指定点（x，y）的线段，并将当前点移动到指定点
    /// </summary>
    /// <param name="x">目标点 x</param>
    /// <param name="y">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData LineTo(double x, double y)
    {
        _dataQueue.AddLast(new OptVal("L", new double[] { x, y }));
        return this;
    }

    /// <summary>
    /// 从当前点连接一条线段到指定点
    /// </summary>
    /// <param name="x">目标点 x</param>
    /// <param name="y">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData L(double x, double y)
    {
        return LineTo(x, y);
    }

    /// <summary>
    /// 从当前点连接一条线段到指定点
    /// </summary>
    /// <param name="p">目标点</param>
    /// <returns>this</returns>
    public AbbreviatedData L(StPos p)
    {
        return LineTo(p.X, p.Y);
    }

    /// <summary>
    /// 从当前点连接一条到点（x2，y2）的二次贝塞尔曲线，
    /// 并将当前点移动到点（x2，y2），此贝塞尔曲线使用
    /// 点（x1，y1）作为其控制点
    /// </summary>
    /// <param name="x1">控制点 x</param>
    /// <param name="y1">控制点 y</param>
    /// <param name="x2">目标点 x</param>
    /// <param name="y2">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData QuadraticBezier(double x1, double y1, double x2, double y2)
    {
        _dataQueue.AddLast(new OptVal("Q", new double[] { x1, y1, x2, y2 }));
        return this;
    }

    /// <summary>
    /// 二次贝塞尔曲线
    /// </summary>
    /// <param name="x1">控制点 x</param>
    /// <param name="y1">控制点 y</param>
    /// <param name="x2">目标点 x</param>
    /// <param name="y2">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData Q(double x1, double y1, double x2, double y2)
    {
        return QuadraticBezier(x1, y1, x2, y2);
    }

    /// <summary>
    /// 二次贝塞尔曲线
    /// </summary>
    /// <param name="control">控制点</param>
    /// <param name="target">目标点</param>
    /// <returns>this</returns>
    public AbbreviatedData Q(StPos control, StPos target)
    {
        return QuadraticBezier(control.X, control.Y, target.X, target.Y);
    }

    /// <summary>
    /// 从当前点连接一条到点（x3，y3）的三次贝塞尔曲线，
    /// 并将当前点移动到点（x3，y3），此贝塞尔曲线使用点
    /// （x1，y1）和点（x2，y2）作为控制点
    /// </summary>
    /// <param name="x1">控制点 x1</param>
    /// <param name="y1">控制点 y1</param>
    /// <param name="x2">控制点 x2</param>
    /// <param name="y2">控制点 y2</param>
    /// <param name="x3">目标点 x3</param>
    /// <param name="y3">目标点 y3</param>
    /// <returns>this</returns>
    public AbbreviatedData CubicBezier(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        _dataQueue.AddLast(new OptVal("B", new double[] { x1, y1, x2, y2, x3, y3 }));
        return this;
    }

    /// <summary>
    /// 三次贝塞尔曲线
    /// </summary>
    /// <param name="x1">控制点 x1</param>
    /// <param name="y1">控制点 y1</param>
    /// <param name="x2">控制点 x2</param>
    /// <param name="y2">控制点 y2</param>
    /// <param name="x3">目标点 x3</param>
    /// <param name="y3">目标点 y3</param>
    /// <returns>this</returns>
    public AbbreviatedData B(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        return CubicBezier(x1, y1, x2, y2, x3, y3);
    }

    /// <summary>
    /// 三次贝塞尔曲线
    /// </summary>
    /// <param name="control1">控制点1</param>
    /// <param name="control2">控制点2</param>
    /// <param name="target">目标点</param>
    /// <returns>this</returns>
    public AbbreviatedData B(StPos control1, StPos control2, StPos target)
    {
        return CubicBezier(control1.X, control1.Y, control2.X, control2.Y, target.X, target.Y);
    }

    /// <summary>
    /// 从当前点连接到点（x，y）的圆弧，并将当前点移动到点（x，y）
    /// </summary>
    /// <param name="rx">椭圆长轴长度</param>
    /// <param name="ry">椭圆短轴长度</param>
    /// <param name="angle">旋转角度，正值顺时针，负值逆时针</param>
    /// <param name="large">1 时表示对应度数大于 180°的弧，0 时表示对应度数小于 180°的弧</param>
    /// <param name="sweep">1 时表示由圆弧起始点到结束点是顺时针旋转，0 时表示逆时针旋转</param>
    /// <param name="x">目标点 x</param>
    /// <param name="y">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData Arc(double rx, double ry, double angle, int large, int sweep, double x, double y)
    {
        if (large != 0 && large != 1)
        {
            throw new ArgumentException("large 只接受 0 或 1");
        }
        if (sweep != 0 && sweep != 1)
        {
            throw new ArgumentException("sweep 只接受 0 或 1");
        }

        _dataQueue.AddLast(new OptVal("A", new double[] { rx, ry, angle, large, sweep, x, y }));
        return this;
    }

    /// <summary>
    /// 椭圆弧
    /// </summary>
    /// <param name="rx">椭圆长轴长度</param>
    /// <param name="ry">椭圆短轴长度</param>
    /// <param name="angle">旋转角度</param>
    /// <param name="large">大弧标志</param>
    /// <param name="sweep">扫描方向</param>
    /// <param name="x">目标点 x</param>
    /// <param name="y">目标点 y</param>
    /// <returns>this</returns>
    public AbbreviatedData A(double rx, double ry, double angle, int large, int sweep, double x, double y)
    {
        return Arc(rx, ry, angle, large, sweep, x, y);
    }

    /// <summary>
    /// 椭圆弧
    /// </summary>
    /// <param name="rx">椭圆长轴长度</param>
    /// <param name="ry">椭圆短轴长度</param>
    /// <param name="angle">旋转角度</param>
    /// <param name="large">大弧标志</param>
    /// <param name="sweep">扫描方向</param>
    /// <param name="target">目标点</param>
    /// <returns>this</returns>
    public AbbreviatedData A(double rx, double ry, double angle, int large, int sweep, StPos target)
    {
        return Arc(rx, ry, angle, large, sweep, target.X, target.Y);
    }

    /// <summary>
    /// SubPath 自动闭合，表示将当前点和 SubPath 的起始点用线段直连连接
    /// </summary>
    /// <returns>this</returns>
    public AbbreviatedData Close()
    {
        _dataQueue.AddLast(new OptVal("C"));
        return this;
    }

    /// <summary>
    /// 路径闭合
    /// </summary>
    /// <returns>this</returns>
    public AbbreviatedData C()
    {
        return Close();
    }

    /// <summary>
    /// 撤销上一步操作
    /// </summary>
    /// <returns>this</returns>
    public AbbreviatedData Undo()
    {
        if (_dataQueue.Count > 0)
        {
            _dataQueue.RemoveLast();
        }
        return this;
    }

    /// <summary>
    /// 绘制操作元素数量
    /// </summary>
    /// <returns>元素数量</returns>
    public int Size()
    {
        return _dataQueue.Count;
    }

    /// <summary>
    /// 序列化为操作序列
    /// </summary>
    /// <returns>操作序列</returns>
    public override string ToString()
    {
        if (_dataQueue.Count == 0)
        {
            return "";
        }

        var dataBuilder = new StringBuilder();
        bool isFirst = true;
        
        foreach (var operatorItem in _dataQueue)
        {
            if (!isFirst)
            {
                dataBuilder.Append(" ");
            }
            dataBuilder.Append(operatorItem.ToString());
            isFirst = false;
        }
        
        return dataBuilder.ToString();
    }

    /// <summary>
    /// 复制路径对象
    /// </summary>
    /// <returns>复制之后的路径对象</returns>
    public new object Clone()
    {
        var clone = new AbbreviatedData();
        clone._dataQueue = new LinkedList<OptVal>();
        
        foreach (var item in _dataQueue)
        {
            clone._dataQueue.AddLast(item.CloneOptVal());
        }
        
        clone.Flush();
        return clone;
    }

    /// <summary>
    /// 强类型克隆
    /// </summary>
    /// <returns>克隆后的对象</returns>
    public AbbreviatedData CloneData()
    {
        return (AbbreviatedData)Clone();
    }

    /// <summary>
    /// 获取枚举器
    /// </summary>
    /// <returns>枚举器</returns>
    public IEnumerator<OptVal> GetEnumerator()
    {
        return _dataQueue.GetEnumerator();
    }

    /// <summary>
    /// 获取枚举器
    /// </summary>
    /// <returns>枚举器</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// 判断两个路径是否完全一致
    /// </summary>
    /// <param name="obj">待比较对象</param>
    /// <returns>true - 路径一致；false - 路径不一致</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not AbbreviatedData other)
        {
            return false;
        }
        return ToString().Equals(other.ToString());
    }

    /// <summary>
    /// 获取哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}
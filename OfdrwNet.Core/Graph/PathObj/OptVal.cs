using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Graph.Tight.Method;

namespace OfdrwNet.Core.Graph.PathObj;

/// <summary>
/// 操作符和操作数
/// 
/// 对应Java版本的 org.ofdrw.core.graph.pathObj.OptVal
/// </summary>
public class OptVal : ICloneable
{
    /// <summary>
    /// 操作符
    /// </summary>
    public string Opt { get; set; }

    /// <summary>
    /// 操作数
    /// </summary>
    public double[] Values { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="opt">操作符</param>
    public OptVal(string opt)
    {
        Opt = opt;
        Values = Array.Empty<double>();
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="opt">操作符</param>
    /// <param name="values">操作数序列</param>
    public OptVal(string opt, double[] values)
    {
        Opt = opt;
        Values = values ?? Array.Empty<double>();
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="opt">操作符</param>
    /// <param name="valueStrList">操作数字符序列</param>
    public OptVal(string opt, List<string> valueStrList)
    {
        Opt = opt;
        if (valueStrList != null)
        {
            Values = new double[valueStrList.Count];
            for (int i = 0; i < valueStrList.Count; i++)
            {
                try
                {
                    Values[i] = double.Parse(valueStrList[i], CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    Values[i] = 0;
                }
            }
        }
        else
        {
            Values = Array.Empty<double>();
        }
    }

    /// <summary>
    /// 在数组长度不足时填充数组
    /// 
    /// 不足时，填充0到新的数组中
    /// </summary>
    /// <param name="arr">数组</param>
    /// <param name="num">期待的长度</param>
    /// <returns>新数组或元素的复制</returns>
    public static double[] Filling(double[]? arr, int num)
    {
        if (arr == null)
        {
            return new double[num];
        }
        
        if (arr.Length < num)
        {
            var newArr = new double[num];
            Array.Copy(arr, newArr, arr.Length);
            return newArr;
        }
        
        return arr;
    }

    /// <summary>
    /// 转换为非紧缩标识
    /// 
    /// 如果无法识别操作符，那么返回null
    /// </summary>
    /// <returns>非紧缩对象或null</returns>
    public ICommand? ToCmd()
    {
        return Opt switch
        {
            "S" or "M" => new Move(Filling(Values, 2)[0], Filling(Values, 2)[1]),
            "L" => new Line(Filling(Values, 2)[0], Filling(Values, 2)[1]),
            "Q" => new QuadraticBezier(
                Filling(Values, 4)[0], Filling(Values, 4)[1],
                Filling(Values, 4)[2], Filling(Values, 4)[3]),
            "B" => new CubicBezier(
                Filling(Values, 6)[0], Filling(Values, 6)[1],
                Filling(Values, 6)[2], Filling(Values, 6)[3],
                Filling(Values, 6)[4], Filling(Values, 6)[5]),
            "A" => new Arc(
                Filling(Values, 7)[0], Filling(Values, 7)[1],
                Filling(Values, 7)[2], Filling(Values, 7)[3],
                Filling(Values, 7)[4], Filling(Values, 7)[5],
                Filling(Values, 7)[6]),
            "C" => new Close(),
            _ => null
        };
    }

    /// <summary>
    /// 获取期待数量的参数值
    /// 
    /// 参数不足时补0
    /// </summary>
    /// <returns>参数值</returns>
    public double[] ExpectValues()
    {
        return Opt switch
        {
            "S" or "M" or "L" => Filling(Values, 2),
            "Q" => Filling(Values, 4),
            "B" => Filling(Values, 6),
            "A" => Filling(Values, 7),
            "C" => Array.Empty<double>(),
            _ => Array.Empty<double>()
        };
    }

    /// <summary>
    /// 克隆对象
    /// </summary>
    /// <returns>克隆后的对象</returns>
    public object Clone()
    {
        return new OptVal(Opt, (double[])Values.Clone());
    }

    /// <summary>
    /// 克隆对象（强类型）
    /// </summary>
    /// <returns>克隆后的对象</returns>
    public OptVal CloneOptVal()
    {
        return new OptVal(Opt, (double[])Values.Clone());
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return Opt switch
        {
            "S" or "M" or "L" => FormatWithValues(Opt, 2),
            "Q" => FormatWithValues("Q", 4),
            "B" => FormatWithValues("B", 6),
            "A" => FormatWithValues("A", 7),
            "C" => "C",
            _ => Opt
        };
    }

    /// <summary>
    /// 格式化操作符和数值
    /// </summary>
    /// <param name="operation">操作符</param>
    /// <param name="expectedCount">期望的数值数量</param>
    /// <returns>格式化字符串</returns>
    private string FormatWithValues(string operation, int expectedCount)
    {
        var arr = Filling(Values, expectedCount);
        var formattedValues = arr.Select(v => StBase.Fmt(v));
        return $"{operation} {string.Join(" ", formattedValues)}";
    }
}
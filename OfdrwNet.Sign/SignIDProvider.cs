using System.Globalization;
using System.Text;

namespace OfdrwNet.Sign;

/// <summary>
/// 签名ID提供者接口
/// 对应 Java 版本的 org.ofdrw.sign.SignIDProvider
/// </summary>
public interface ISignIDProvider
{
    /// <summary>
    /// 获取签名ID
    /// </summary>
    /// <returns>签名ID</returns>
    string Get();

    /// <summary>
    /// 递增
    /// 为下一次提供新的ID做准备
    /// </summary>
    void Increment();
}

/// <summary>
/// 数字格式原子签名ID提供者
/// 对应 Java 版本的 org.ofdrw.sign.NumberFormatAtomicSignID
/// </summary>
public class NumberFormatAtomicSignID : ISignIDProvider
{
    /// <summary>
    /// 当前ID值
    /// </summary>
    private long _current;

    /// <summary>
    /// ID是否使用补零格式
    /// </summary>
    private readonly bool _padding;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="padding">是否使用补零格式，如：Sign_001、Sign_002</param>
    public NumberFormatAtomicSignID(bool padding)
    {
        _padding = padding;
        _current = 0;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="base">基础ID，之后递增</param>
    /// <param name="padding">是否使用补零格式</param>
    public NumberFormatAtomicSignID(long @base, bool padding)
    {
        _current = @base;
        _padding = padding;
    }

    /// <summary>
    /// 获取签名ID
    /// </summary>
    /// <returns>签名ID</returns>
    public string Get()
    {
        if (_padding)
        {
            return $"Sign_{_current:D3}";
        }
        else
        {
            return $"Sign_{_current}";
        }
    }

    /// <summary>
    /// 递增
    /// </summary>
    public void Increment()
    {
        Interlocked.Increment(ref _current);
    }

    /// <summary>
    /// 获取当前计数
    /// </summary>
    /// <returns>当前计数</returns>
    public long GetCurrent()
    {
        return _current;
    }

    /// <summary>
    /// 设置计数
    /// </summary>
    /// <param name="current">计数值</param>
    public void SetCurrent(long current)
    {
        _current = current;
    }
}

/// <summary>
/// 标准格式原子签名ID提供者
/// 对应 Java 版本的 org.ofdrw.sign.StandFormatAtomicSignID
/// </summary>
public class StandFormatAtomicSignID : ISignIDProvider
{
    /// <summary>
    /// 当前ID值
    /// </summary>
    private long _current;

    /// <summary>
    /// 构造函数
    /// </summary>
    public StandFormatAtomicSignID()
    {
        _current = 0;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="base">基础ID，之后递增</param>
    public StandFormatAtomicSignID(long @base)
    {
        _current = @base;
    }

    /// <summary>
    /// 获取签名ID
    /// 格式：sgn{N}，如：sgn1、sgn2
    /// </summary>
    /// <returns>签名ID</returns>
    public string Get()
    {
        return $"sgn{_current}";
    }

    /// <summary>
    /// 递增
    /// </summary>
    public void Increment()
    {
        Interlocked.Increment(ref _current);
    }

    /// <summary>
    /// 获取当前计数
    /// </summary>
    /// <returns>当前计数</returns>
    public long GetCurrent()
    {
        return _current;
    }

    /// <summary>
    /// 设置计数
    /// </summary>
    /// <param name="current">计数值</param>
    public void SetCurrent(long current)
    {
        _current = current;
    }
}
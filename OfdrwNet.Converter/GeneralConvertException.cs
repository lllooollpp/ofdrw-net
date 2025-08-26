namespace OfdrwNet.Converter;

/// <summary>
/// 通用转换异常
/// 对应 Java 版本的 org.ofdrw.converter.GeneralConvertException
/// </summary>
public class GeneralConvertException : Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误信息</param>
    public GeneralConvertException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="innerException">内部异常</param>
    public GeneralConvertException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
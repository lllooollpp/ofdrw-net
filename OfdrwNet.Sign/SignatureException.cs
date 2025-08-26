namespace OfdrwNet.Sign;

/// <summary>
/// 数字签名异常
/// 对应 Java 版本的 org.ofdrw.sign.SignatureException
/// </summary>
public class SignatureException : Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常消息</param>
    public SignatureException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public SignatureException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// 数字签名终止异常
/// 对应 Java 版本的 org.ofdrw.sign.SignatureTerminateException
/// </summary>
public class SignatureTerminateException : SignatureException
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常消息</param>
    public SignatureTerminateException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public SignatureTerminateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
using System;

namespace OfdrwNet.Reader;

/// <summary>
/// 错误路径异常
/// 
/// 对应Java版本的 org.ofdrw.reader.ErrorPathException
/// </summary>
public class ErrorPathException : Exception
{
    public ErrorPathException()
    {
    }

    public ErrorPathException(string message) : base(message)
    {
    }

    public ErrorPathException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
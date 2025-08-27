using System;

namespace OfdrwNet.Reader;

/// <summary>
/// 错误OFD文件结构和文档格式异常
/// 
/// 对应Java版本的 org.ofdrw.reader.BadOFDException
/// </summary>
public class BadOfdException : Exception
{
    public BadOfdException()
    {
    }

    public BadOfdException(string message) : base(message)
    {
    }

    public BadOfdException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
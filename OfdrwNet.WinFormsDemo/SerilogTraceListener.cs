using System.Diagnostics;
using Serilog;

namespace OfdrwNet.WinFormsDemo;

/// <summary>
/// 将 Trace（以及在大多数运行时配置下的 Debug）输出转发到 Serilog。
/// </summary>
public class SerilogTraceListener : TraceListener
{
    /// <summary>
    /// 写入不带行终止符的消息到 Serilog（Debug 级别）。
    /// </summary>
    public override void Write(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Log.Debug("{TraceMessage}", message);
        }
    }

    /// <summary>
    /// 写入带行终止符的消息到 Serilog（Debug 级别）。
    /// </summary>
    public override void WriteLine(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Log.Debug("{TraceMessage}", message);
        }
    }

    /// <summary>
    /// 写入 Fail 级别消息（映射为 Error）。
    /// </summary>
    public override void Fail(string? message, string? detailMessage)
    {
        Log.Error("Trace Fail: {Message} {Detail}", message, detailMessage);
    }
}

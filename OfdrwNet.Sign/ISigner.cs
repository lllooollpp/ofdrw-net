using System;
using System.Collections.Generic;

namespace OfdrwNet.Sign;

/// <summary>
/// 签名器接口。
/// </summary>
/// <remarks>
/// 定义签名提供者的统一契约,支持 SM2/RSA 等多种算法。
///
/// 实现要求:
/// - 线程安全
/// - 支持批量签名(可选)
/// - 支持时间戳(可选)
/// - 分离式签名(可选)
/// </remarks>
public interface ISigner
{
    /// <summary>
    /// 签名器唯一标识。
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 签名能力标志。
    /// </summary>
    SignerCapabilities Capabilities { get; }

    /// <summary>
    /// 对摘要进行签名。
    /// </summary>
    /// <param name="digest">待签名的摘要数据</param>
    /// <param name="context">签名上下文</param>
    /// <returns>签名值</returns>
    byte[] Sign(byte[] digest, SignerContext context);
}

/// <summary>
/// 签名上下文。
/// </summary>
public sealed class SignerContext
{
    /// <summary>
    /// 证书标识。
    /// </summary>
    public string CertId { get; init; } = string.Empty;

    /// <summary>
    /// 签名算法(如 "SM2", "RSA")。
    /// </summary>
    public string Algorithm { get; init; } = "SM2";

    /// <summary>
    /// 扩展属性。
    /// </summary>
    public IReadOnlyDictionary<string, string> Extra { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// 签名器能力标志。
/// </summary>
[Flags]
public enum SignerCapabilities
{
    /// <summary>
    /// 无特殊能力。
    /// </summary>
    None = 0,

    /// <summary>
    /// 支持分离式签名。
    /// </summary>
    Detached = 1,

    /// <summary>
    /// 支持时间戳。
    /// </summary>
    Timestamp = 2,

    /// <summary>
    /// 支持批量签名。
    /// </summary>
    Batch = 4
}

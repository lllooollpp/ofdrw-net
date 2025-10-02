using System.IO;

namespace OfdrwNet.Converter.Security;

/// <summary>
/// 加密提供者接口。
/// </summary>
/// <remarks>
/// 定义流式加密/解密的统一契约。
/// 支持 SM4/AES 等对称加密算法。
/// </remarks>
public interface IEncryptionProvider
{
    /// <summary>
    /// 加密算法名称(如 "SM4", "AES-256")。
    /// </summary>
    string Algorithm { get; }

    /// <summary>
    /// 包装写入流(加密)。
    /// </summary>
    /// <param name="raw">原始流</param>
    /// <returns>加密流</returns>
    Stream WrapWrite(Stream raw);

    /// <summary>
    /// 包装读取流(解密)。
    /// </summary>
    /// <param name="raw">原始流</param>
    /// <returns>解密流</returns>
    Stream WrapRead(Stream raw);
}

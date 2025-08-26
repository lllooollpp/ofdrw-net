namespace OfdrwNet.Sign;

/// <summary>
/// 数字签名模式
/// 对应 Java 版本的 org.ofdrw.sign.SignMode
/// </summary>
public enum SignMode
{
    /// <summary>
    /// 继续数字签名模式
    /// 该模式下不会保护签名列表文件（Signatures.xml），
    /// 但是用户可以继续对OFD文件添加新的数字签名
    /// </summary>
    ContinueSign,

    /// <summary>
    /// 完整保护模式
    /// 该模式将会保护OFD中的所有文件，
    /// 但是一旦使用该模式数字签名那么该文档不允许继续添加其他签名
    /// </summary>
    WholeProtect
}
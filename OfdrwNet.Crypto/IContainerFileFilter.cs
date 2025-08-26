namespace OfdrwNet.Crypto;

/// <summary>
/// 容器文件过滤器
/// 对应 Java 版本的 org.ofdrw.crypto.ContainerFileFilter
/// 用于判断文件是否需要被加密，返回false表示不需要加密
/// </summary>
public interface IContainerFileFilter
{
    /// <summary>
    /// 判断指定的容器路径是否需要加密
    /// </summary>
    /// <param name="containerPath">容器路径</param>
    /// <returns>true表示需要加密，false表示不需要加密</returns>
    bool ShouldEncrypt(ContainerPath containerPath);
}
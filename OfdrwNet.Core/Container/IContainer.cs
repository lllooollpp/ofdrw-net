using OfdrwNet.Core;

namespace OfdrwNet.Core.Container;

/// <summary>
/// OFD容器接口
/// 提供文件和对象管理的抽象接口，避免对具体实现的依赖
/// </summary>
public interface IContainer : IDisposable
{
    /// <summary>
    /// 获取系统绝对路径
    /// </summary>
    /// <returns>绝对路径</returns>
    string GetSysAbsPath();

    /// <summary>
    /// 获取或创建子容器
    /// </summary>
    /// <param name="dirName">目录名称</param>
    /// <param name="containerFactory">容器工厂函数</param>
    /// <returns>子容器</returns>
    IContainer ObtainContainer(string dirName, Func<IContainer> containerFactory);

    /// <summary>
    /// 添加OFD对象到容器
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="obj">OFD对象</param>
    /// <returns>当前容器</returns>
    IContainer PutObj(string fileName, OfdElement obj);

    /// <summary>
    /// 添加原始文件到容器
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="inputStream">输入流</param>
    /// <returns>当前容器</returns>
    IContainer AddRaw(string fileName, Stream inputStream);

    /// <summary>
    /// 刷新容器到文件系统
    /// </summary>
    /// <returns>异步任务</returns>
    Task FlushAsync();

    /// <summary>
    /// 清理容器内容
    /// </summary>
    void Clean();
}
namespace OfdrwNet.Layout.Handler;

/// <summary>
/// 虚拟页面处理器
/// 对应 Java 版本的 org.ofdrw.layout.handler.VPageHandler
/// 可以通过该处理器，在生成OFD页面Content.xml前对虚拟页面进行处理，例如页头、页脚等
/// </summary>
public interface IVPageHandler
{
    /// <summary>
    /// 执行处理
    /// </summary>
    /// <param name="page">虚拟页面</param>
    void Handle(VirtualPage page);
}

/// <summary>
/// 委托形式的虚拟页面处理器
/// 提供更灵活的使用方式
/// </summary>
/// <param name="page">虚拟页面</param>
public delegate void VPageHandler(VirtualPage page);
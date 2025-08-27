namespace OfdrwNet.Core.Graph.Tight.Method;

/// <summary>
/// 路径闭合命令
/// 
/// SubPath 自动闭合，表示将当前点和 SubPath 的起始点用线段直连连接
/// 
/// 对应Java版本的 org.ofdrw.core.graph.tight.method.Close
/// </summary>
public class Close : ICommand
{
    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return "C";
    }
}
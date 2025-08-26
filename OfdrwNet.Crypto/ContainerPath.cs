using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Crypto;

/// <summary>
/// 文件在容器中的路径
/// 对应 Java 版本的 org.ofdrw.crypto.ContainerPath
/// 用于管理加密文件在OFD容器中的路径映射
/// </summary>
public class ContainerPath
{
    /// <summary>
    /// 容器内绝对路径
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// 文件系统内绝对路径
    /// </summary>
    public string AbsolutePath { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="path">容器内绝对路径</param>
    /// <param name="absolutePath">文件系统内绝对路径</param>
    public ContainerPath(string path, string absolutePath)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        AbsolutePath = absolutePath ?? throw new ArgumentNullException(nameof(absolutePath));
    }

    /// <summary>
    /// 创建加密后的文件路径（空）
    /// 输出加密文件命名规则：原文件名全小写 + .dat 后缀，如果重复，那么在文件名后面增加后缀
    /// </summary>
    /// <returns>创建加密后输出文件</returns>
    public ContainerPath CreateEncryptedFile()
    {
        var parentDir = System.IO.Path.GetDirectoryName(AbsolutePath);
        if (string.IsNullOrEmpty(parentDir))
        {
            throw new InvalidOperationException("无法确定父目录");
        }
        
        return CreateDatFile(Path, parentDir);
    }

    /// <summary>
    /// 创建后缀为.dat的文件
    /// 加密文件为原文件后缀改为.dat
    /// 若文件存在，则在文件名后追加下划线序号_N
    /// </summary>
    /// <param name="containerAbsPath">容器内绝对路径</param>
    /// <param name="parentDirectory">生成的加密文件存储目录</param>
    /// <returns>容器路径对象</returns>
    public static ContainerPath CreateDatFile(string containerAbsPath, string parentDirectory)
    {
        if (string.IsNullOrEmpty(containerAbsPath))
        {
            throw new ArgumentException("容器路径不能为空", nameof(containerAbsPath));
        }
        
        if (!containerAbsPath.StartsWith("/"))
        {
            containerAbsPath = "/" + containerAbsPath;
        }

        if (!Directory.Exists(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        var containerLoc = new StLoc(containerAbsPath);
        var originalName = containerLoc.GetFileName();
        
        // 获取不含扩展名的文件名
        var name = System.IO.Path.GetFileNameWithoutExtension(originalName);
        if (string.IsNullOrEmpty(name))
        {
            name = originalName;
        }

        var encryptedFileName = name.ToLowerInvariant() + ".dat";
        var resultPath = System.IO.Path.Combine(parentDirectory, encryptedFileName);
        
        var counter = 1;
        // 输出加密文件命名规则：原文件名全小写 + .dat 后缀，如果重复，那么在文件名后面增加后缀
        while (File.Exists(resultPath))
        {
            encryptedFileName = name.ToLowerInvariant() + "_" + counter + ".dat";
            resultPath = System.IO.Path.Combine(parentDirectory, encryptedFileName);
            counter++;
        }

        // 创建文件
        File.Create(resultPath).Dispose();
        
        var containerParent = containerLoc.Parent().ToString();
        if (!containerParent.EndsWith("/"))
        {
            containerParent += "/";
        }
        
        return new ContainerPath(containerParent + encryptedFileName, resultPath);
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"ContainerPath(Path={Path}, Abs={AbsolutePath})";
    }
}
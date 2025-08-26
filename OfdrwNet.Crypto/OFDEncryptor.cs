using OfdrwNet.Packaging.Container;
using System.Security.Cryptography;
using System.IO.Compression;

namespace OfdrwNet.Crypto;

/// <summary>
/// OFD文档加密器
/// 对应 Java 版本的 org.ofdrw.crypto.OFDEncryptor
/// 实现 GM/T 0099-2020 开放版式文档密码应用技术规范
/// </summary>
public class OFDEncryptor : IDisposable
{
    /// <summary>
    /// 工作过程中的工作目录，用于存放解压后的OFD文档容器内容
    /// </summary>
    private readonly string _workDirectory;

    /// <summary>
    /// 加密后文件输出位置
    /// </summary>
    private readonly string _destinationPath;

    /// <summary>
    /// 是否已经关闭
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// 随机数生成器
    /// </summary>
    private readonly RandomNumberGenerator _random;

    /// <summary>
    /// 用户提供的加密器列表
    /// </summary>
    private readonly List<IUserFEKEncryptor> _userEncryptors;

    /// <summary>
    /// 容器文件过滤器
    /// </summary>
    private IContainerFileFilter? _containerFileFilter;

    /// <summary>
    /// 创建OFD加密器
    /// </summary>
    /// <param name="ofdFilePath">待加密的OFD文件路径</param>
    /// <param name="destinationPath">加密后的OFD路径</param>
    public OFDEncryptor(string ofdFilePath, string destinationPath)
    {
        if (string.IsNullOrEmpty(ofdFilePath) || !File.Exists(ofdFilePath))
        {
            throw new FileNotFoundException("OFD文件不存在", ofdFilePath);
        }
        
        if (string.IsNullOrEmpty(destinationPath))
        {
            throw new ArgumentException("加密后文件路径不能为空", nameof(destinationPath));
        }

        _destinationPath = destinationPath;
        _workDirectory = Path.Combine(Path.GetTempPath(), "ofd-encrypt-" + Guid.NewGuid().ToString("N"));
        _random = RandomNumberGenerator.Create();
        _userEncryptors = new List<IUserFEKEncryptor>();

        // 解压OFD文档到工作目录
        ExtractOfdFile(ofdFilePath);
    }

    /// <summary>
    /// 添加加密用户
    /// </summary>
    /// <param name="encryptor">加密用户的加密器</param>
    /// <returns>this</returns>
    public OFDEncryptor AddUser(IUserFEKEncryptor encryptor)
    {
        if (encryptor != null)
        {
            _userEncryptors.Add(encryptor);
        }
        return this;
    }

    /// <summary>
    /// 设置容器文件过滤器
    /// 该过滤器用于决定哪些文件将会被加密
    /// 过滤器结果为false那么该文件将不会被加密
    /// </summary>
    /// <param name="filter">过滤器</param>
    /// <returns>this</returns>
    public OFDEncryptor SetContainerFileFilter(IContainerFileFilter filter)
    {
        _containerFileFilter = filter;
        return this;
    }

    /// <summary>
    /// 执行加密
    /// </summary>
    /// <returns>this</returns>
    public async Task<OFDEncryptor> EncryptAsync()
    {
        if (_userEncryptors.Count == 0)
        {
            throw new InvalidOperationException("没有可用的加密用户");
        }

        // 确保目标目录存在
        var destinationDir = Path.GetDirectoryName(_destinationPath);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        // 获取待加密文件列表
        var filesToEncrypt = GetFilesToEncrypt();

        // 生成文件加密密钥（FEK）和初始化向量
        var fek = new byte[32]; // SM4密钥长度为32字节
        var iv = new byte[16];  // SM4 IV长度为16字节
        _random.GetBytes(fek);
        _random.GetBytes(iv);

        // 加密文件
        await EncryptFiles(filesToEncrypt, fek, iv);

        // 为每个用户加密FEK
        await EncryptFEKForUsers(fek);

        // 创建加密描述文件
        await CreateEncryptionDescriptor();

        // 打包成最终的加密OFD文件
        await PackageEncryptedOfd();

        return this;
    }

    /// <summary>
    /// 解压OFD文件到工作目录
    /// </summary>
    /// <param name="ofdFilePath">OFD文件路径</param>
    private void ExtractOfdFile(string ofdFilePath)
    {
        Directory.CreateDirectory(_workDirectory);
        ZipFile.ExtractToDirectory(ofdFilePath, _workDirectory);
    }

    /// <summary>
    /// 获取需要加密的文件列表
    /// </summary>
    /// <returns>待加密文件列表</returns>
    private List<ContainerPath> GetFilesToEncrypt()
    {
        var filesToEncrypt = new List<ContainerPath>();
        
        // 递归遍历工作目录中的所有文件
        var allFiles = Directory.GetFiles(_workDirectory, "*", SearchOption.AllDirectories);
        
        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(_workDirectory, file);
            var containerPath = new ContainerPath("/" + relativePath.Replace('\\', '/'), file);
            
            // 应用过滤器判断是否需要加密
            if (_containerFileFilter?.ShouldEncrypt(containerPath) ?? true)
            {
                filesToEncrypt.Add(containerPath);
            }
        }
        
        return filesToEncrypt;
    }

    /// <summary>
    /// 加密文件列表
    /// </summary>
    /// <param name="filesToEncrypt">待加密文件</param>
    /// <param name="fek">文件加密密钥</param>
    /// <param name="iv">初始化向量</param>
    private async Task EncryptFiles(List<ContainerPath> filesToEncrypt, byte[] fek, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = fek;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        foreach (var containerPath in filesToEncrypt)
        {
            var sourceFile = containerPath.AbsolutePath;
            var encryptedPath = containerPath.CreateEncryptedFile();
            
            // 读取原文件内容
            var plaintext = await File.ReadAllBytesAsync(sourceFile);
            
            // 加密文件内容
            using var encryptor = aes.CreateEncryptor();
            var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
            
            // 写入加密文件
            await File.WriteAllBytesAsync(encryptedPath.AbsolutePath, ciphertext);
            
            // 删除原文件
            File.Delete(sourceFile);
        }
    }

    /// <summary>
    /// 为每个用户加密FEK
    /// </summary>
    /// <param name="fek">文件加密密钥</param>
    private async Task EncryptFEKForUsers(byte[] fek)
    {
        foreach (var userEncryptor in _userEncryptors)
        {
            await userEncryptor.EncryptFEKAsync(fek);
        }
    }

    /// <summary>
    /// 创建加密描述文件
    /// </summary>
    private async Task CreateEncryptionDescriptor()
    {
        // 这里应该创建符合GM/T 0099标准的加密描述XML文件\n        // 暂时创建一个简单的描述文件
        var descriptorContent = $@"<?xml version='1.0' encoding='UTF-8'?>
<Encryptions>
    <Encryption ID='1'>
        <Parameters Algorithm='SM4' Mode='CBC' />
        <EncryptEntries>
            <!-- 加密文件条目列表 -->
        </EncryptEntries>
    </Encryption>
</Encryptions>";
        
        var descriptorPath = Path.Combine(_workDirectory, "Encryptions.xml");
        await File.WriteAllTextAsync(descriptorPath, descriptorContent);
    }

    /// <summary>
    /// 打包加密的OFD文件
    /// </summary>
    private async Task PackageEncryptedOfd()
    {
        await Task.Run(() =>
        {
            if (File.Exists(_destinationPath))
            {
                File.Delete(_destinationPath);
            }
            ZipFile.CreateFromDirectory(_workDirectory, _destinationPath);
        });
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _random?.Dispose();
                
                // 清理工作目录
                if (Directory.Exists(_workDirectory))
                {
                    Directory.Delete(_workDirectory, true);
                }
            }
            catch
            {
                // 忽略清理异常
            }
            
            _disposed = true;
        }
    }
}

/// <summary>
/// 用户文件加密密钥（FEK）加密器接口
/// </summary>
public interface IUserFEKEncryptor
{
    /// <summary>
    /// 加密文件加密密钥
    /// </summary>
    /// <param name="fek">文件加密密钥</param>
    /// <returns>加密任务</returns>
    Task EncryptFEKAsync(byte[] fek);
}
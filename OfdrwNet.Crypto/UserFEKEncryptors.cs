using System.Security.Cryptography;
using System.Text;

namespace OfdrwNet.Crypto;

/// <summary>
/// 基于密码的用户FEK加密器
/// 对应 Java 版本的密码加密功能
/// 使用PBKDF2派生密钥，然后用AES加密FEK
/// </summary>
public class PasswordUserFEKEncryptor : IUserFEKEncryptor
{
    private readonly string _password;
    private readonly byte[] _salt;
    private readonly int _iterations;
    
    /// <summary>
    /// 加密后的FEK数据
    /// </summary>
    public byte[]? EncryptedFEK { get; private set; }
    
    /// <summary>
    /// 用于加密的盐值
    /// </summary>
    public byte[] Salt => _salt;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="password">用户密码</param>
    /// <param name="salt">盐值，如果为null则自动生成</param>
    /// <param name="iterations">PBKDF2迭代次数，默认100000</param>
    public PasswordUserFEKEncryptor(string password, byte[]? salt = null, int iterations = 100000)
    {
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _iterations = iterations;
        
        if (salt != null)
        {
            _salt = salt;
        }
        else
        {
            _salt = new byte[16]; // 128位盐值
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(_salt);
        }
    }

    /// <summary>
    /// 加密文件加密密钥
    /// </summary>
    /// <param name="fek">文件加密密钥</param>
    /// <returns>加密任务</returns>
    public async Task EncryptFEKAsync(byte[] fek)
    {
        if (fek == null)
            throw new ArgumentNullException(nameof(fek));

        await Task.Run(() =>
        {
            // 使用PBKDF2从密码派生密钥
            using var pbkdf2 = new Rfc2898DeriveBytes(_password, _salt, _iterations, HashAlgorithmName.SHA256);
            var derivedKey = pbkdf2.GetBytes(32); // 256位密钥

            // 使用AES加密FEK
            using var aes = Aes.Create();
            aes.Key = derivedKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(fek, 0, fek.Length);

            // 将IV和加密数据组合
            EncryptedFEK = new byte[aes.IV.Length + encrypted.Length];
            Array.Copy(aes.IV, 0, EncryptedFEK, 0, aes.IV.Length);
            Array.Copy(encrypted, 0, EncryptedFEK, aes.IV.Length, encrypted.Length);
        });
    }

    /// <summary>
    /// 解密文件加密密钥
    /// </summary>
    /// <param name="encryptedFEK">加密的FEK数据</param>
    /// <returns>解密后的FEK</returns>
    public byte[] DecryptFEK(byte[] encryptedFEK)
    {
        if (encryptedFEK == null)
            throw new ArgumentNullException(nameof(encryptedFEK));

        // 使用PBKDF2从密码派生密钥
        using var pbkdf2 = new Rfc2898DeriveBytes(_password, _salt, _iterations, HashAlgorithmName.SHA256);
        var derivedKey = pbkdf2.GetBytes(32);

        // 分离IV和密文
        using var aes = Aes.Create();
        aes.Key = derivedKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        var iv = new byte[16]; // AES IV长度
        var ciphertext = new byte[encryptedFEK.Length - 16];
        
        Array.Copy(encryptedFEK, 0, iv, 0, 16);
        Array.Copy(encryptedFEK, 16, ciphertext, 0, ciphertext.Length);
        
        aes.IV = iv;

        // 解密
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }
}

/// <summary>
/// 基于证书的用户FEK加密器
/// 使用RSA或SM2公钥加密FEK
/// </summary>
public class CertificateUserFEKEncryptor : IUserFEKEncryptor
{
    private readonly RSA _publicKey;
    
    /// <summary>
    /// 加密后的FEK数据
    /// </summary>
    public byte[]? EncryptedFEK { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="publicKey">RSA公钥</param>
    public CertificateUserFEKEncryptor(RSA publicKey)
    {
        _publicKey = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
    }

    /// <summary>
    /// 加密文件加密密钥
    /// </summary>
    /// <param name="fek">文件加密密钥</param>
    /// <returns>加密任务</returns>
    public async Task EncryptFEKAsync(byte[] fek)
    {
        if (fek == null)
            throw new ArgumentNullException(nameof(fek));

        await Task.Run(() =>
        {
            // 使用RSA公钥加密FEK
            EncryptedFEK = _publicKey.Encrypt(fek, RSAEncryptionPadding.OaepSHA256);
        });
    }

    /// <summary>
    /// 使用私钥解密FEK
    /// </summary>
    /// <param name="privateKey">RSA私钥</param>
    /// <param name="encryptedFEK">加密的FEK</param>
    /// <returns>解密后的FEK</returns>
    public byte[] DecryptFEK(RSA privateKey, byte[] encryptedFEK)
    {
        if (privateKey == null)
            throw new ArgumentNullException(nameof(privateKey));
        if (encryptedFEK == null)
            throw new ArgumentNullException(nameof(encryptedFEK));

        return privateKey.Decrypt(encryptedFEK, RSAEncryptionPadding.OaepSHA256);
    }
}
using System.Numerics;

namespace OfdrwNet.Crypto;

/// <summary>
/// SM2密码数据结构
/// 对应 Java 版本的 org.ofdrw.gm.sm2strut.SM2Cipher
/// 基于 GBT 35276-2017 7.2 加密数据格式
/// </summary>
public class SM2Cipher
{
    /// <summary>
    /// X 坐标
    /// </summary>
    public BigInteger XCoordinate { get; set; }

    /// <summary>
    /// Y 坐标
    /// </summary>
    public BigInteger YCoordinate { get; set; }

    /// <summary>
    /// 明文SM3哈希值 (32字节)
    /// </summary>
    public byte[] Hash { get; set; } = new byte[32];

    /// <summary>
    /// 密文数据
    /// </summary>
    public byte[] CipherText { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public SM2Cipher()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="xCoordinate">X坐标</param>
    /// <param name="yCoordinate">Y坐标</param>
    /// <param name="hash">哈希值</param>
    /// <param name="cipherText">密文</param>
    public SM2Cipher(BigInteger xCoordinate, BigInteger yCoordinate, byte[] hash, byte[] cipherText)
    {
        XCoordinate = xCoordinate;
        YCoordinate = yCoordinate;
        Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        CipherText = cipherText ?? throw new ArgumentNullException(nameof(cipherText));
    }

    /// <summary>
    /// 转换为C1C3C2格式
    /// C1为椭圆曲线点(65字节：0x04+32字节X+32字节Y)
    /// C3为SM3哈希值(32字节)
    /// C2为密文数据
    /// </summary>
    /// <returns>C1C3C2格式数据</returns>
    public byte[] ConvertToC1C3C2()
    {
        using var stream = new MemoryStream();
        
        // 准备32字节的X和Y坐标
        var x = new byte[32];
        var y = new byte[32];
        
        var xBytes = XCoordinate.ToByteArray(isUnsigned: true, isBigEndian: true);
        var yBytes = YCoordinate.ToByteArray(isUnsigned: true, isBigEndian: true);
        
        // 复制到32字节数组，右对齐
        Array.Copy(xBytes, 0, x, 32 - xBytes.Length, xBytes.Length);
        Array.Copy(yBytes, 0, y, 32 - yBytes.Length, yBytes.Length);
        
        // C1: 椭圆曲线点
        stream.WriteByte(0x04); // 非压缩点前缀
        stream.Write(x);        // X坐标 32字节
        stream.Write(y);        // Y坐标 32字节
        
        // C3: SM3哈希值
        stream.Write(Hash);     // 32字节哈希
        
        // C2: 密文数据
        stream.Write(CipherText);
        
        return stream.ToArray();
    }

    /// <summary>
    /// 从C1C3C2格式创建SM2Cipher对象
    /// </summary>
    /// <param name="c1c3c2Data">C1C3C2格式数据</param>
    /// <returns>SM2Cipher对象</returns>
    public static SM2Cipher FromC1C3C2(byte[] c1c3c2Data)
    {
        if (c1c3c2Data == null)
            throw new ArgumentNullException(nameof(c1c3c2Data));
        
        if (c1c3c2Data.Length < 97) // 最小长度：1 + 32 + 32 + 32 = 97
            throw new ArgumentException("C1C3C2数据长度不足");
            
        using var stream = new MemoryStream(c1c3c2Data);
        
        // 读取并验证非压缩点前缀
        var prefix = stream.ReadByte();
        if (prefix != 0x04)
            throw new ArgumentException("无效的椭圆曲线点前缀");
            
        // 读取X坐标 (32字节)
        var x = new byte[32];
        stream.Read(x, 0, 32);
        
        // 读取Y坐标 (32字节)  
        var y = new byte[32];
        stream.Read(y, 0, 32);
        
        // 读取哈希值 (32字节)
        var hash = new byte[32];
        stream.Read(hash, 0, 32);
        
        // 读取密文数据（剩余部分）
        var cipherTextLength = c1c3c2Data.Length - 97;
        var cipherText = new byte[cipherTextLength];
        stream.Read(cipherText, 0, cipherTextLength);
        
        return new SM2Cipher(
            new BigInteger(x, isUnsigned: true, isBigEndian: true),
            new BigInteger(y, isUnsigned: true, isBigEndian: true),
            hash,
            cipherText
        );
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"SM2Cipher(X={XCoordinate:X}, Y={YCoordinate:X}, HashLen={Hash.Length}, CipherLen={CipherText.Length})";
    }
}
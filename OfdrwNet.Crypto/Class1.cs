﻿namespace OfdrwNet.Crypto;

/// <summary>
/// 国密算法常量定义
/// 对应 Java 版本的 org.ofdrw.gm.sm2strut.OIDs
/// 基于 GM/T 35275-2017 和 GM/T 33560-2017 标准
/// </summary>
public static class GMOIDs
{
    /// <summary>
    /// SM2密码算法加密签名消息语法规范
    /// </summary>
    public const string Gmt35275Sm2 = "1.2.156.10197.6.1.4.2";

    /// <summary>
    /// 数据类型
    /// </summary>
    public const string Data = "1.2.156.10197.6.1.4.2.1";

    /// <summary>
    /// 签名数据类型
    /// </summary>
    public const string SignedData = "1.2.156.10197.6.1.4.2.2";

    /// <summary>
    /// 数字信封类型
    /// </summary>
    public const string EnvelopedData = "1.2.156.10197.6.1.4.2.3";

    /// <summary>
    /// 签名及数字信封类型
    /// </summary>
    public const string SignedAndEnvelopedData = "1.2.156.10197.6.1.4.2.4";

    /// <summary>
    /// 加密数据类型
    /// </summary>
    public const string EncryptedData = "1.2.156.10197.6.1.4.2.5";

    /// <summary>
    /// 密钥协商类型
    /// </summary>
    public const string KeyAgreementInfo = "1.2.156.10197.6.1.4.2.6";

    /// <summary>
    /// SM4分组密码算法
    /// </summary>
    public const string SM4 = "1.2.156.10197.1.100";

    /// <summary>
    /// SM2椭圆曲线公钥密码算法
    /// </summary>
    public const string SM2 = "1.2.156.10197.1.301";

    /// <summary>
    /// SM2-1 数字签名算法
    /// </summary>
    public const string SM2Sign = "1.2.156.10197.1.301.1";

    /// <summary>
    /// SM2-2 密钥交换协议
    /// </summary>
    public const string SM2KeyExchange = "1.2.156.10197.1.301.2";

    /// <summary>
    /// SM2-3 公钥加密算法
    /// </summary>
    public const string SM2Encrypt = "1.2.156.10197.1.301.3";

    /// <summary>
    /// SM3 密码杂凑算法
    /// </summary>
    public const string SM3 = "1.2.156.10197.1.401";

    /// <summary>
    /// SM3 密码杂凑算法，无密钥使用
    /// </summary>
    public const string SM3_1 = "1.2.156.10197.1.401.1";

    /// <summary>
    /// SM3 密码杂凑算法，有密钥使用
    /// </summary>
    public const string SM3_2 = "1.2.156.10197.1.401.2";

    /// <summary>
    /// 基于SM2算法和SM3算法的签名
    /// </summary>
    public const string SM3WithSM2 = "1.2.156.10197.501";
}

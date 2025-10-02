using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OfdrwNet.Sign.Providers;

/// <summary>
/// SM2 签名提供者。
/// </summary>
/// <remarks>
/// 实现国密 SM2 椭圆曲线数字签名算法。
///
/// 功能:
/// - SM2 密钥对生成
/// - SM2 签名计算(使用 BouncyCastle)
/// - SM3 哈希摘要
/// - 证书加载和验证
///
/// 标准:
/// - GM/T 0003-2012: SM2 椭圆曲线公钥密码算法
/// - GM/T 0004-2012: SM3 密码杂凑算法
/// - GB/T 33190-2016: OFD 签名规范
///
/// 依赖:
/// - Portable.BouncyCastle (1.9.0+)
/// </remarks>
public sealed class Sm2SignatureProvider : ISigner
{
    private readonly ILogger<Sm2SignatureProvider> _logger;
    private AsymmetricKeyParameter? _privateKey;
    private X509Certificate? _certificate;

    /// <summary>
    /// SM2 曲线名称。
    /// </summary>
    private const string _sm2CurveName = "sm2p256v1";

    /// <summary>
    /// SM2 签名算法标识。
    /// </summary>
    private const string _sm2SignatureAlgorithm = "SM3withSM2";

    /// <summary>
    /// 签名器唯一标识。
    /// </summary>
    public string Id { get; } = "SM2-Provider";

    /// <summary>
    /// 签名能力标志。
    /// </summary>
    public SignerCapabilities Capabilities { get; } = SignerCapabilities.None;

    /// <summary>
    /// 初始化 Sm2SignatureProvider 实例。
    /// </summary>
    public Sm2SignatureProvider(ILogger<Sm2SignatureProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 对摘要进行签名(ISigner接口实现)。
    /// </summary>
    public byte[] Sign(byte[] digest, SignerContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        _logger.LogDebug("Signing with algorithm: {Algorithm}", context.Algorithm);
        return Sign(digest);
    }

    /// <summary>
    /// 加载私钥和证书。
    /// </summary>
    /// <param name="privateKeyPath">私钥文件路径(PEM 格式)</param>
    /// <param name="certificatePath">证书文件路径(PEM/DER 格式)</param>
    /// <param name="password">私钥密码(可选)</param>
    public void LoadKeyAndCertificate(string privateKeyPath, string certificatePath, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPath))
        {
            throw new ArgumentException("Private key path cannot be null or empty", nameof(privateKeyPath));
        }

        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            throw new ArgumentException("Certificate path cannot be null or empty", nameof(certificatePath));
        }

        try
        {
            // 加载私钥
            _privateKey = LoadPrivateKey(privateKeyPath, password);
            _logger.LogInformation("Loaded SM2 private key from '{Path}'", privateKeyPath);

            // 加载证书
            _certificate = LoadCertificate(certificatePath);
            _logger.LogInformation("Loaded certificate from '{Path}' (Subject: {Subject})",
                certificatePath, _certificate.SubjectDN);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load key and certificate");
            throw;
        }
    }

    /// <summary>
    /// 加载私钥。
    /// </summary>
    private AsymmetricKeyParameter LoadPrivateKey(string path, string? password)
    {
        using var reader = new StreamReader(path);
        var pemReader = password != null
            ? new PemReader(reader, new PasswordFinder(password))
            : new PemReader(reader);

        var keyObject = pemReader.ReadObject();

        return keyObject switch
        {
            AsymmetricCipherKeyPair keyPair => keyPair.Private,
            AsymmetricKeyParameter keyParam => keyParam,
            _ => throw new InvalidOperationException($"Unsupported key object type: {keyObject?.GetType().Name}")
        };
    }

    /// <summary>
    /// 加载证书。
    /// </summary>
    private X509Certificate LoadCertificate(string path)
    {
        var parser = new X509CertificateParser();

        using var stream = File.OpenRead(path);
        var cert = parser.ReadCertificate(stream);

        if (cert == null)
        {
            throw new InvalidOperationException($"Failed to parse certificate from '{path}'");
        }

        return cert;
    }

    /// <summary>
    /// 生成 SM2 密钥对。
    /// </summary>
    /// <returns>密钥对(公钥, 私钥)</returns>
    public AsymmetricCipherKeyPair GenerateKeyPair()
    {
        try
        {
            var ecParams = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName(_sm2CurveName);
            if (ecParams == null)
            {
                throw new InvalidOperationException($"SM2 curve '{_sm2CurveName}' not found");
            }

            var domainParams = new ECDomainParameters(
                ecParams.Curve,
                ecParams.G,
                ecParams.N,
                ecParams.H,
                ecParams.GetSeed());

            var keyGenParams = new ECKeyGenerationParameters(domainParams, new SecureRandom());
            var generator = new ECKeyPairGenerator();
            generator.Init(keyGenParams);

            var keyPair = generator.GenerateKeyPair();
            _logger.LogInformation("Generated SM2 key pair");

            return keyPair;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SM2 key pair");
            throw;
        }
    }

    /// <summary>
    /// 对数据进行 SM2 签名。
    /// </summary>
    /// <param name="data">待签名数据</param>
    /// <returns>签名值</returns>
    public byte[] Sign(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data to sign cannot be null or empty", nameof(data));
        }

        if (_privateKey == null)
        {
            throw new InvalidOperationException("Private key not loaded. Call LoadKeyAndCertificate first.");
        }

        try
        {
            // 使用 SM3 计算摘要
            var digest = ComputeSm3Hash(data);

            // SM2 签名
            var signer = SignerUtilities.GetSigner(_sm2SignatureAlgorithm);
            signer.Init(true, _privateKey);
            signer.BlockUpdate(digest, 0, digest.Length);

            var signature = signer.GenerateSignature();
            _logger.LogDebug("Generated SM2 signature (Length: {Length} bytes)", signature.Length);

            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign data with SM2");
            throw;
        }
    }

    /// <summary>
    /// 验证 SM2 签名。
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <param name="signature">签名值</param>
    /// <returns>验证成功返回 true</returns>
    public bool Verify(byte[] data, byte[] signature)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        }

        if (signature == null || signature.Length == 0)
        {
            throw new ArgumentException("Signature cannot be null or empty", nameof(signature));
        }

        if (_certificate == null)
        {
            throw new InvalidOperationException("Certificate not loaded. Call LoadKeyAndCertificate first.");
        }

        try
        {
            var publicKey = _certificate.GetPublicKey();
            var digest = ComputeSm3Hash(data);

            var verifier = SignerUtilities.GetSigner(_sm2SignatureAlgorithm);
            verifier.Init(false, publicKey);
            verifier.BlockUpdate(digest, 0, digest.Length);

            var isValid = verifier.VerifySignature(signature);
            _logger.LogDebug("SM2 signature verification: {Result}", isValid ? "Valid" : "Invalid");

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify SM2 signature");
            return false;
        }
    }

    /// <summary>
    /// 计算 SM3 哈希。
    /// </summary>
    /// <param name="data">输入数据</param>
    /// <returns>SM3 摘要(32 字节)</returns>
    public byte[] ComputeSm3Hash(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
        }

        try
        {
            var digest = DigestUtilities.GetDigest("SM3");
            digest.BlockUpdate(data, 0, data.Length);

            var hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);

            _logger.LogDebug("Computed SM3 hash (Length: {Length} bytes)", hash.Length);
            return hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute SM3 hash");
            throw;
        }
    }

    /// <summary>
    /// 获取证书信息。
    /// </summary>
    public CertificateInfo GetCertificateInfo()
    {
        if (_certificate == null)
        {
            throw new InvalidOperationException("Certificate not loaded");
        }

        return new CertificateInfo
        {
            Subject = _certificate.SubjectDN.ToString(),
            Issuer = _certificate.IssuerDN.ToString(),
            SerialNumber = _certificate.SerialNumber.ToString(),
            NotBefore = _certificate.NotBefore,
            NotAfter = _certificate.NotAfter,
            SignatureAlgorithm = _certificate.SigAlgName
        };
    }

    /// <summary>
    /// 密码查找器(用于加载加密私钥)。
    /// </summary>
    private sealed class PasswordFinder : IPasswordFinder
    {
        private readonly string _password;

        public PasswordFinder(string password)
        {
            _password = password;
        }

        public char[] GetPassword()
        {
            return _password.ToCharArray();
        }
    }
}

/// <summary>
/// 证书信息。
/// </summary>
public sealed class CertificateInfo
{
    /// <summary>
    /// 证书主体。
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 证书颁发者。
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 证书序列号。
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 生效时间。
    /// </summary>
    public DateTime NotBefore { get; set; }

    /// <summary>
    /// 过期时间。
    /// </summary>
    public DateTime NotAfter { get; set; }

    /// <summary>
    /// 签名算法。
    /// </summary>
    public string SignatureAlgorithm { get; set; } = string.Empty;
}

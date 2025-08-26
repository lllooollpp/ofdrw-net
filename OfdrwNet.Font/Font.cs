using System.Drawing;
using System.Drawing.Text;

namespace OfdrwNet.Font;

/// <summary>
/// 字体类
/// 对应 Java 版本的 org.ofdrw.font.Font
/// 包含字体名称、族名称、字体文件路径、字符宽度映射等信息
/// </summary>
public class Font
{
    /// <summary>
    /// 字体名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 字体族名称
    /// </summary>
    public string FamilyName { get; set; }

    /// <summary>
    /// 字体文件路径
    /// </summary>
    public string? FontFilePath { get; set; }

    /// <summary>
    /// 可打印字符宽度映射
    /// </summary>
    public double[]? PrintableAsciiWidthMap { get; set; }

    /// <summary>
    /// 缓存的系统字体对象
    /// </summary>
    private System.Drawing.Font? _systemFont;

    /// <summary>
    /// 是否可嵌入OFD文件包
    /// </summary>
    public bool Embeddable { get; set; } = true;

    /// <summary>
    /// 字体加载器的私有字体集合
    /// </summary>
    private PrivateFontCollection? _privateFontCollection;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">字体名称</param>
    /// <param name="familyName">字体族名称</param>
    public Font(string name, string familyName)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FamilyName = familyName ?? throw new ArgumentNullException(nameof(familyName));
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">字体名称</param>
    /// <param name="familyName">字体族名称</param>
    /// <param name="fontFilePath">字体文件路径</param>
    public Font(string name, string familyName, string? fontFilePath)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FamilyName = familyName ?? throw new ArgumentNullException(nameof(familyName));
        FontFilePath = fontFilePath;
        
        if (!string.IsNullOrEmpty(fontFilePath))
        {
            if (!File.Exists(fontFilePath))
            {
                throw new FileNotFoundException("字体文件不存在", fontFilePath);
            }
            LoadFontFromFile(fontFilePath);
        }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">字体名称</param>
    /// <param name="familyName">字体族名称</param>
    /// <param name="fontFilePath">字体文件路径</param>
    /// <param name="printableAsciiWidthMap">可打印字符映射表，用于处理字符的宽度</param>
    public Font(string name, string familyName, string? fontFilePath, double[]? printableAsciiWidthMap)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FamilyName = familyName ?? throw new ArgumentNullException(nameof(familyName));
        FontFilePath = fontFilePath;
        PrintableAsciiWidthMap = printableAsciiWidthMap;
        
        if (!string.IsNullOrEmpty(fontFilePath))
        {
            if (!File.Exists(fontFilePath))
            {
                throw new FileNotFoundException("字体文件不存在", fontFilePath);
            }
            LoadFontFromFile(fontFilePath);
        }
    }

    /// <summary>
    /// 获取默认字体（宋体）
    /// </summary>
    /// <returns>默认字体</returns>
    public static Font GetDefault()
    {
        return FontName.SimSun.ToFont();
    }

    /// <summary>
    /// 从文件创建字体
    /// </summary>
    /// <param name="name">字体名称</param>
    /// <param name="fontFilePath">字体文件路径</param>
    /// <returns>字体对象</returns>
    public static Font FromFile(string name, string fontFilePath)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("字体名称不能为空", nameof(name));
        if (string.IsNullOrWhiteSpace(fontFilePath))
            throw new ArgumentException("字体文件路径不能为空", nameof(fontFilePath));
        if (!File.Exists(fontFilePath))
            throw new FileNotFoundException("字体文件不存在", fontFilePath);
            
        var font = new Font(name, name, fontFilePath);
        return font;
    }

    /// <summary>
    /// 字体是否存在预设的字符宽度映射表
    /// </summary>
    /// <returns>true - 存在；false - 不存在</returns>
    public bool HasWidthMap()
    {
        return PrintableAsciiWidthMap != null && PrintableAsciiWidthMap.Length > 0;
    }

    /// <summary>
    /// 获取字符占比
    /// </summary>
    /// <param name="character">字符</param>
    /// <returns>0~1 占比</returns>
    public double GetCharWidthScale(char character)
    {
        // 如果存在字符映射那么从字符映射中获取宽度占比
        if (PrintableAsciiWidthMap != null)
        {
            // 所有 ASCII码均采用半角
            if (character >= 32 && character <= 126)
            {
                // 根据可打印宽度比例映射表计算
                return PrintableAsciiWidthMap[character - 32];
            }
            else
            {
                // 非英文字符
                return 1.0;
            }
        }
        else
        {
            // 不存在字符映射，那么认为是等宽度比例 ASCII 为 0.5 其他为 1
            return (character >= 32 && character <= 126) ? 0.5 : 1.0;
        }
    }

    /// <summary>
    /// 设置可打印字符宽度映射表
    /// 在使用操作系统字体时，默认采用ASCII 0.5 其余1的比例计算宽度，因此可能需要手动设置宽度比例才可以达到相应的效果
    /// </summary>
    /// <param name="map">映射比例表</param>
    /// <returns>this</returns>
    public Font SetPrintableAsciiWidthMap(double[] map)
    {
        PrintableAsciiWidthMap = map;
        return this;
    }

    /// <summary>
    /// 获取字体全名
    /// </summary>
    /// <returns>字体全名</returns>
    public string GetCompleteFontName()
    {
        if (string.IsNullOrEmpty(FamilyName))
        {
            return Name;
        }
        return $"{Name}-{FamilyName}";
    }

    /// <summary>
    /// 设置字体名称
    /// </summary>
    /// <param name="name">字体名称</param>
    /// <returns>this</returns>
    public Font SetName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// 设置字体族名称
    /// </summary>
    /// <param name="familyName">字体族名称</param>
    /// <returns>this</returns>
    public Font SetFamilyName(string familyName)
    {
        FamilyName = familyName ?? throw new ArgumentNullException(nameof(familyName));
        return this;
    }

    /// <summary>
    /// 获取字体文件名称
    /// </summary>
    /// <returns>字体文件名称</returns>
    public string? GetFontFileName()
    {
        return string.IsNullOrEmpty(FontFilePath) ? null : Path.GetFileName(FontFilePath);
    }

    /// <summary>
    /// 设置字体文件路径
    /// </summary>
    /// <param name="fontFilePath">字体文件路径</param>
    /// <returns>this</returns>
    public Font SetFontFile(string fontFilePath)
    {
        if (!File.Exists(fontFilePath))
        {
            throw new FileNotFoundException("字体文件不存在", fontFilePath);
        }
        
        FontFilePath = fontFilePath;
        LoadFontFromFile(fontFilePath);
        return this;
    }

    /// <summary>
    /// 获取系统字体对象，该对象可能为空（当前没有提供字体路径时为空）
    /// </summary>
    /// <returns>字体对象或null</returns>
    public System.Drawing.Font? GetSystemFont()
    {
        return _systemFont;
    }

    /// <summary>
    /// 设置字体嵌入标识
    /// </summary>
    /// <param name="embeddable">true-代表字体文件会被嵌入到OFD文件中，false-表示不嵌入</param>
    /// <returns>this</returns>
    public Font SetEmbeddable(bool embeddable)
    {
        Embeddable = embeddable;
        return this;
    }

    /// <summary>
    /// 从文件加载字体
    /// </summary>
    /// <param name="fontFilePath">字体文件路径</param>
    private void LoadFontFromFile(string fontFilePath)
    {
        try
        {
            _privateFontCollection = new PrivateFontCollection();
            _privateFontCollection.AddFontFile(fontFilePath);
            
            if (_privateFontCollection.Families.Length > 0)
            {
                var fontFamily = _privateFontCollection.Families[0];
                _systemFont = new System.Drawing.Font(fontFamily, 12); // 默认12号字体
                
                // 如果没有设置FamilyName，使用从字体文件中获取的族名称
                if (string.IsNullOrEmpty(FamilyName))
                {
                    FamilyName = fontFamily.Name;
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("字体文件格式错误或无法加载", ex);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _systemFont?.Dispose();
        _privateFontCollection?.Dispose();
    }

    /// <summary>
    /// 转换为字符串表示
    /// </summary>
    /// <returns>字符串表示</returns>
    public override string ToString()
    {
        return $"Font(Name={Name}, Family={FamilyName}, File={GetFontFileName()}, Embeddable={Embeddable})";
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using OfdrwNet.Core;
using OfdrwNet.Core.BasicStructure.Doc;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Core.PageDescription.DrawParam;
using OfdrwNet.Core.Text.Font;

namespace OfdrwNet.Reader;

/// <summary>
/// 资源管理器（只读）
/// 
/// 使用ID随机访问文档中出现的资源对象
/// 包括公共资源序列（PublicRes）和文档资源序列（DocumentRes）
/// 
/// 注意：资源管理器提供的资源对象均为只读对象（副本），不允许对资源进行修改,
/// 所有提供的对象中文档的相对路径均在加载时转换为绝对路径。
/// 
/// 对应Java版本的 org.ofdrw.reader.ResourceManage
/// </summary>
public class ResourceManage
{
    /// <summary>
    /// 颜色空间映射表
    /// </summary>
    private readonly Dictionary<string, ColorSpace> _colorSpaceMap = new();

    /// <summary>
    /// 绘制参数映射表
    /// </summary>
    private readonly Dictionary<string, CtDrawParam> _drawParamMap = new();

    /// <summary>
    /// 字形映射表
    /// </summary>
    private readonly Dictionary<string, CtFont> _fontMap = new();

    /// <summary>
    /// 多媒体对象映射表
    /// </summary>
    private readonly Dictionary<string, OfdResource> _multiMediaMap = new();

    /// <summary>
    /// 矢量图像映射表
    /// </summary>
    private readonly Dictionary<string, OfdElement> _compositeGraphicUnitMap = new();

    /// <summary>
    /// 所有资源和ID的映射表
    /// </summary>
    private readonly Dictionary<string, OfdElement> _allResMap = new();

    /// <summary>
    /// 文档公共数据结构
    /// </summary>
    private CtCommonData? _commonData;

    /// <summary>
    /// OFD阅读器引用
    /// </summary>
    private readonly OfdReader _ofdReader;

    /// <summary>
    /// 创建资源管理器
    /// 
    /// 选择默认文档（Doc_0）进行资源的加载
    /// </summary>
    /// <param name="ofdReader">OFD解析器</param>
    public ResourceManage(OfdReader ofdReader)
    {
        _ofdReader = ofdReader ?? throw new ArgumentNullException(nameof(ofdReader));
        try
        {
            LoadDefaultDoc();
        }
        catch (Exception e)
        {
            throw new BadOfdException("文档结构解析异常", e);
        }
    }

    /// <summary>
    /// 指定文档创建资源管理器
    /// </summary>
    /// <param name="ofdReader">OFD解析器</param>
    /// <param name="docNum">文档序号，从0起</param>
    public ResourceManage(OfdReader ofdReader, int docNum)
    {
        _ofdReader = ofdReader ?? throw new ArgumentNullException(nameof(ofdReader));
        try
        {
            LoadDoc(docNum);
        }
        catch (Exception e)
        {
            throw new BadOfdException("文档结构解析异常", e);
        }
    }

    /// <summary>
    /// 获取绘制参数
    /// 
    /// 注意：资源管理器提供的资源对象均为只读对象（副本），不允许对资源进行修改。
    /// </summary>
    /// <param name="id">资源ID</param>
    /// <returns>绘制参数，不存在返回null</returns>
    public CtDrawParam? GetDrawParam(string id)
    {
        return _drawParamMap.TryGetValue(id, out var param) ? param : null;
    }

    /// <summary>
    /// 递归的解析绘制参数并覆盖配置参数内容
    /// </summary>
    /// <param name="id">资源ID</param>
    /// <returns>绘制参数，不存在返回null</returns>
    public CtDrawParam? GetDrawParamFinal(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var current = GetDrawParam(id);
        return SuperDrawParam(current);
    }

    /// <summary>
    /// 获取字体资源
    /// </summary>
    /// <param name="id">资源ID</param>
    /// <returns>字体资源，不存在返回null</returns>
    public CtFont? GetFont(string id)
    {
        return _fontMap.TryGetValue(id, out var font) ? font : null;
    }

    /// <summary>
    /// 获取颜色空间
    /// </summary>
    /// <param name="id">资源ID</param>
    /// <returns>颜色空间，不存在返回null</returns>
    public ColorSpace? GetColorSpace(string id)
    {
        return _colorSpaceMap.TryGetValue(id, out var colorSpace) ? colorSpace : null;
    }

    /// <summary>
    /// 获取多媒体资源
    /// </summary>
    /// <param name="id">资源ID</param>
    /// <returns>多媒体资源，不存在返回null</returns>
    public OfdResource? GetMultiMedia(string id)
    {
        return _multiMediaMap.TryGetValue(id, out var media) ? media : null;
    }

    /// <summary>
    /// 寻找继承属性用于覆盖当前为空的属性
    /// </summary>
    /// <param name="current">当前需要子节点</param>
    /// <returns>补全后的子节点副本</returns>
    public CtDrawParam? SuperDrawParam(CtDrawParam? current)
    {
        if (current == null)
        {
            return null;
        }

        // 复制为副本防止造成污染
        current = current.Clone();
        var relative = current.GetRelative();
        if (relative == null)
        {
            return current;
        }

        // 递归的寻找上一级继承的参数的最终参数
        var parent = GetDrawParamFinal(relative.ToString());
        if (parent == null)
        {
            return current;
        }

        // 本次绘制属性将覆盖其引用的绘制参数中的同名属性
        if (current.GetLineWidth() == null && parent.GetLineWidth() != null)
        {
            current.SetLineWidth(parent.GetLineWidth());
        }
        
        if (current.GetJoin() == null && parent.GetJoin() != null)
        {
            current.SetJoin(parent.GetJoin());
        }
        
        if (current.GetCap() == null && parent.GetCap() != null)
        {
            current.SetCap(parent.GetCap());
        }
        
        if (current.GetDashOffset() == null && parent.GetDashOffset() != null)
        {
            current.SetDashOffset(parent.GetDashOffset());
        }
        
        if (current.GetDashPattern() == null && parent.GetDashPattern() != null)
        {
            current.SetDashPattern(parent.GetDashPattern());
        }
        
        if (current.GetMiterLimit() == null && parent.GetMiterLimit() != null)
        {
            current.SetMiterLimit(parent.GetMiterLimit());
        }
        
        if (current.GetFillColor() == null && parent.GetFillColor() != null)
        {
            current.SetFillColor(parent.GetFillColor());
        }
        
        if (current.GetStrokeColor() == null && parent.GetStrokeColor() != null)
        {
            current.SetStrokeColor(parent.GetStrokeColor());
        }

        return current;
    }

    /// <summary>
    /// 加载默认文档
    /// </summary>
    public void LoadDefaultDoc()
    {
        LoadDoc(0);
    }

    /// <summary>
    /// 加载指定文档
    /// </summary>
    /// <param name="docNum">文档序号</param>
    public void LoadDoc(int docNum)
    {
        // 基础实现 - 这里需要解析OFD文档结构，加载各种资源
        // 1. 解析OFD.xml获取文档路径
        // 2. 解析Document.xml获取公共数据和资源
        // 3. 加载各种资源类型到对应的映射表中
        
        // 简单实现，实际中需要从OFD文件中解析资源
        try
        {
            // 这里是一个占位符实现
            // 实际实现需要从_ofdReader中获取文档目录和资源文件
            
            // 清空现有资源
            _colorSpaceMap.Clear();
            _drawParamMap.Clear();
            _fontMap.Clear();
            _multiMediaMap.Clear();
            _compositeGraphicUnitMap.Clear();
            _allResMap.Clear();
            
            // TODO: 实际解析逻辑
        }
        catch (Exception e)
        {
            throw new BadOfdException($"加载文档 {docNum} 失败", e);
        }
    }

    /// <summary>
    /// 加载文档资源
    /// </summary>
    /// <param name="docReader">文档阅读器</param>
    public void LoadDocRes(OfdReader docReader)
    {
        // 加载文档资源的实现
        // 这是从文档级别资源文件中加载资源
    }

    /// <summary>
    /// 加载资源文件
    /// </summary>
    /// <param name="resFilePath">资源文件路径</param>
    public void LoadResFile(string resFilePath)
    {
        // 从指定的资源文件路径加载资源
        if (string.IsNullOrEmpty(resFilePath))
            return;
            
        // TODO: 实际的资源文件解析逻辑
    }

    /// <summary>
    /// 获取资源流
    /// </summary>
    /// <param name="resourcePath">资源路径</param>
    /// <returns>资源流</returns>
    public Stream? GetResourceStream(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
            return null;
            
        try
        {
            // 通过OFD阅读器获取资源流
            var ofdDir = _ofdReader.GetOFDDir();
            if (ofdDir != null)
            {
                // 从OFD目录中获取资源文件路径
                var filePath = ofdDir.GetInner().GetFile(resourcePath);
                // 打开文件流
                return File.OpenRead(filePath);
            }
        }
        catch (Exception)
        {
            // 忽略异常，返回null
        }
        
        return null;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _colorSpaceMap.Clear();
        _drawParamMap.Clear();
        _fontMap.Clear();
        _multiMediaMap.Clear();
        _compositeGraphicUnitMap.Clear();
        _allResMap.Clear();
    }
}

/// <summary>
/// OFD资源表示类
/// </summary>
public class OfdResource : OfdElement
{
    /// <summary>
    /// 资源类型
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 资源文件路径
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 资源内容流
    /// </summary>
    public Stream? Content { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">资源ID</param>
    /// <param name="type">资源类型</param>
    /// <param name="filePath">资源文件路径</param>
    public OfdResource(string id, string type, string filePath) : base("Resource")
    {
        this.SetObjId(StId.Parse(id));
        Type = type;
        FilePath = filePath;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public OfdResource() : base("Resource")
    {
    }

    /// <summary>
    /// 从XElement构造
    /// </summary>
    /// <param name="element">XML元素</param>
    public OfdResource(System.Xml.Linq.XElement element) : base(element)
    {
        Type = element.Attribute("Type")?.Value;
        FilePath = element.Attribute("FilePath")?.Value;
    }
}

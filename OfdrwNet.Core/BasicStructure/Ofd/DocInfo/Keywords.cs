using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace OfdrwNet.Core.BasicStructure.Ofd.DocInfo;

/// <summary>
/// 关键词集合，每一个关键词用一个"Keyword"子节点来表达
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.ofd.docInfo.Keywords
/// 表 4 文档元数据属性
/// </summary>
public class Keywords : OfdElement
{
    /// <summary>
    /// 从现有元素构造关键词集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public Keywords(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的关键词集合
    /// </summary>
    public Keywords() : base("Keywords")
    {
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:Keywords";

    /// <summary>
    /// 【必选】
    /// 增加关键字
    /// </summary>
    /// <param name="keyword">关键字</param>
    /// <returns>this</returns>
    public Keywords AddKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            throw new ArgumentException("关键字不能为空", nameof(keyword));

        var keywordElement = new XElement("Keyword", keyword);
        Element.Add(keywordElement);
        return this;
    }

    /// <summary>
    /// 获取关键字列表
    /// </summary>
    /// <returns>关键字列表</returns>
    public List<string> GetKeywords()
    {
        return Element.Elements("Keyword")
            .Select(e => e.Value.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();
    }

    /// <summary>
    /// 移除关键字
    /// </summary>
    /// <param name="keyword">要移除的关键字</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        var keywordElements = Element.Elements("Keyword")
            .Where(e => e.Value.Trim().Equals(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var element in keywordElements)
        {
            element.Remove();
        }

        return keywordElements.Any();
    }

    /// <summary>
    /// 清空所有关键字
    /// </summary>
    /// <returns>this</returns>
    public Keywords ClearKeywords()
    {
        Element.Elements("Keyword").Remove();
        return this;
    }

    /// <summary>
    /// 检查是否包含指定关键字
    /// </summary>
    /// <param name="keyword">要检查的关键字</param>
    /// <returns>是否包含</returns>
    public bool ContainsKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        return Element.Elements("Keyword")
            .Any(e => e.Value.Trim().Equals(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取关键字数量
    /// </summary>
    /// <returns>关键字数量</returns>
    public int Count => Element.Elements("Keyword").Count();
}

/// <summary>
/// 用户自定义元数据集合。其子节点为 CustomData
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.ofd.docInfo.CustomDatas
/// </summary>
public class CustomDatas : OfdElement
{
    /// <summary>
    /// 从现有元素构造自定义数据集合
    /// </summary>
    /// <param name="element">XML元素</param>
    public CustomDatas(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的自定义数据集合
    /// </summary>
    public CustomDatas() : base("CustomDatas")
    {
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:CustomDatas";

    /// <summary>
    /// 【必选】
    /// 增加用户自定义元数据
    /// </summary>
    /// <param name="name">用户自定义元数据名称</param>
    /// <param name="value">用户自定义元数据值</param>
    /// <returns>this</returns>
    public CustomDatas AddCustomData(string name, string value)
    {
        var customData = new CustomData(name, value);
        return AddCustomData(customData);
    }

    /// <summary>
    /// 【必选】
    /// 增加用户自定义元数据
    /// </summary>
    /// <param name="customData">用户自定义元数据</param>
    /// <returns>this</returns>
    public CustomDatas AddCustomData(CustomData customData)
    {
        if (customData == null)
            throw new ArgumentNullException(nameof(customData));

        Element.Add(customData.Element);
        return this;
    }

    /// <summary>
    /// 【必选】
    /// 获取自定义元数据集合
    /// </summary>
    /// <returns>自定义元数据集合</returns>
    public List<CustomData> GetCustomDatas()
    {
        return Element.Elements("CustomData")
            .Select(e => new CustomData(e))
            .ToList();
    }

    /// <summary>
    /// 获取用户自定义元数据值
    /// </summary>
    /// <param name="name">元数据名称</param>
    /// <returns>元数据值</returns>
    public string? GetCustomDataValue(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var customData = GetCustomDatas()
            .FirstOrDefault(cd => cd.GetDataName().Equals(name, StringComparison.OrdinalIgnoreCase));

        return customData?.GetValue();
    }

    /// <summary>
    /// 移除指定名称的自定义数据
    /// </summary>
    /// <param name="name">要移除的元数据名称</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveCustomData(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var elementsToRemove = Element.Elements("CustomData")
            .Where(e => {
                var customData = new CustomData(e);
                return customData.GetDataName().Equals(name, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        foreach (var element in elementsToRemove)
        {
            element.Remove();
        }

        return elementsToRemove.Any();
    }

    /// <summary>
    /// 清空所有自定义数据
    /// </summary>
    /// <returns>this</returns>
    public CustomDatas ClearCustomDatas()
    {
        Element.Elements("CustomData").Remove();
        return this;
    }

    /// <summary>
    /// 获取自定义数据数量
    /// </summary>
    /// <returns>自定义数据数量</returns>
    public int Count => Element.Elements("CustomData").Count();
}

/// <summary>
/// 用户自定义元数据
/// 
/// 对应Java版本的 org.ofdrw.core.basicStructure.ofd.docInfo.CustomData
/// </summary>
public class CustomData : OfdElement
{
    /// <summary>
    /// 从现有元素构造自定义数据
    /// </summary>
    /// <param name="element">XML元素</param>
    public CustomData(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的自定义数据
    /// </summary>
    public CustomData() : base("CustomData")
    {
    }

    /// <summary>
    /// 构造带名称和值的自定义数据
    /// </summary>
    /// <param name="name">数据名称</param>
    /// <param name="value">数据值</param>
    public CustomData(string name, string value) : this()
    {
        SetDataName(name);
        SetValue(value);
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:CustomData";

    /// <summary>
    /// 【必选 属性】
    /// 设置自定义元数据名称
    /// </summary>
    /// <param name="name">元数据名称</param>
    /// <returns>this</returns>
    public CustomData SetDataName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("元数据名称不能为空", nameof(name));

        SetAttribute("Name", name);
        return this;
    }

    /// <summary>
    /// 【必选 属性】
    /// 获取自定义元数据名称
    /// </summary>
    /// <returns>元数据名称</returns>
    public string GetDataName()
    {
        return GetAttributeValue("Name") ?? "";
    }

    /// <summary>
    /// 【必选】
    /// 设置自定义元数据值
    /// </summary>
    /// <param name="value">元数据值</param>
    /// <returns>this</returns>
    public CustomData SetValue(string value)
    {
        Element.Value = value ?? "";
        return this;
    }

    /// <summary>
    /// 【必选】
    /// 获取自定义元数据值
    /// </summary>
    /// <returns>元数据值</returns>
    public string GetValue()
    {
        return Element.Value ?? "";
    }

    /// <summary>
    /// 验证自定义数据是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(GetDataName());
    }
}

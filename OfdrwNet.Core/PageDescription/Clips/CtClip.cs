using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace OfdrwNet.Core.PageDescription.Clips;

/// <summary>
/// 裁剪区
/// 
/// 裁剪区由一组路径或文字构成，用以指定页面上的一个有效绘制区域，落在裁剪区
/// 以外的部分不受绘制指令的影响。
/// 
/// 一个裁剪区可由多个分路径（Area）组成，最终的裁剪范围是各个部分路径的并集。
/// 裁剪区中的数据均相对于所修饰图元对象的外界矩形。
/// 
/// 8.4 裁剪区 图 44 表 33
/// 
/// 对应 Java 版本的 org.ofdrw.core.pageDescription.clips.CT_Clip
/// </summary>
public class CtClip : OfdElement
{
    /// <summary>
    /// 从现有XML元素构造裁剪区
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtClip(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的裁剪区
    /// </summary>
    public CtClip() : base("Clip")
    {
    }

    /// <summary>
    /// 【必选】增加裁剪区域
    /// 
    /// 用一个图形对象或文字对象来描述裁剪区的一个组成部分，
    /// 最终裁剪区是这些区域的并集。
    /// </summary>
    /// <param name="area">裁剪区域</param>
    /// <returns>this</returns>
    public CtClip AddArea(Area area)
    {
        if (area != null)
        {
            Add(area);
        }
        return this;
    }

    /// <summary>
    /// 【必选】获取裁剪区域列表
    /// 
    /// 用一个图形对象或文字对象来描述裁剪区的一个组成部分，
    /// 最终裁剪区是这些区域的并集。
    /// </summary>
    /// <returns>裁剪区域列表</returns>
    public List<Area> GetAreas()
    {
        return GetOfdElements("Area").Select(e => new Area(e)).ToList();
    }

    /// <summary>
    /// 获取限定名称
    /// </summary>
    public override string QualifiedName => "ofd:Clip";
}
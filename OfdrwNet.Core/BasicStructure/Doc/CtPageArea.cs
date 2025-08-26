using System.Xml.Linq;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Core.BasicStructure.Doc;

/// <summary>
/// 页面区域结构
/// 
/// ————《GB/T 33190-2016》 图 7
/// 
/// 对应 Java 版本的 org.ofdrw.core.basicStructure.doc.CT_PageArea
/// </summary>
public class CtPageArea : OfdElement
{
    /// <summary>
    /// 从现有元素构造页面区域
    /// </summary>
    /// <param name="element">XML元素</param>
    public CtPageArea(XElement element) : base(element)
    {
    }

    /// <summary>
    /// 构造新的页面区域元素
    /// </summary>
    public CtPageArea() : base("PageArea")
    {
    }

    /// <summary>
    /// 页面物理区域创建区域
    /// </summary>
    /// <param name="topLeftX">页面物理区域左上角X坐标</param>
    /// <param name="topLeftY">页面物理区域左上角Y坐标</param>
    /// <param name="width">页面物理区域宽度</param>
    /// <param name="height">页面物理区域高度</param>
    public CtPageArea(double topLeftX, double topLeftY, double width, double height) : this()
    {
        SetPhysicalBox(topLeftX, topLeftY, width, height);
    }

    /// <summary>
    /// 【必选】
    /// 设置页面物理区域
    /// 左上角为页面坐标系的原点
    /// </summary>
    /// <param name="physicalBox">页面物理区域</param>
    /// <returns>this</returns>
    public CtPageArea SetPhysicalBox(StBox physicalBox)
    {
        SetOfdEntity("PhysicalBox", physicalBox.ToString());
        return this;
    }

    /// <summary>
    /// 【必选】
    /// 设置页面物理区域
    /// 左上角为页面坐标系的原点
    /// </summary>
    /// <param name="topLeftX">左上角X坐标</param>
    /// <param name="topLeftY">左上角Y坐标</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>this</returns>
    public CtPageArea SetPhysicalBox(double topLeftX, double topLeftY, double width, double height)
    {
        var physicalBox = new StBox(topLeftX, topLeftY, width, height);
        return SetPhysicalBox(physicalBox);
    }

    /// <summary>
    /// 【必选】
    /// 获取页面物理区域，左上角为页面坐标系的原点
    /// </summary>
    /// <returns>页面物理区域</returns>
    public StBox? GetPhysicalBox()
    {
        var text = GetOfdElementText("PhysicalBox");
        return string.IsNullOrEmpty(text) ? null : StBox.Parse(text);
    }

    /// <summary>
    /// 【可选】
    /// 设置显示区域
    /// 页面内容实际显示或打印输出的区域，位于页面物理区域内，
    /// 包含页眉、页脚、页心等内容
    /// [例外处理] 如果显示区域不完全位于页面物理区域内，
    /// 页面物理区域外的部分则被忽略。如果显示区域完全位于页面物理区域外，
    /// 则设置该页为空白页。
    /// </summary>
    /// <param name="applicationBox">显示区域</param>
    /// <returns>this</returns>
    public CtPageArea SetApplicationBox(StBox applicationBox)
    {
        SetOfdEntity("ApplicationBox", applicationBox.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取显示区域
    /// 页面内容实际显示或打印输出的区域，位于页面物理区域内，
    /// 包含页眉、页脚、页心等内容
    /// </summary>
    /// <returns>显示区域</returns>
    public StBox? GetApplicationBox()
    {
        var text = GetOfdElementText("ApplicationBox");
        return string.IsNullOrEmpty(text) ? null : StBox.Parse(text);
    }

    /// <summary>
    /// 【可选】
    /// 设置内容区域
    /// 页面除页眉、页脚之外的可编辑区域
    /// </summary>
    /// <param name="contentBox">内容区域</param>
    /// <returns>this</returns>
    public CtPageArea SetContentBox(StBox contentBox)
    {
        SetOfdEntity("ContentBox", contentBox.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取内容区域
    /// 页面除页眉、页脚之外的可编辑区域
    /// </summary>
    /// <returns>内容区域</returns>
    public StBox? GetContentBox()
    {
        var text = GetOfdElementText("ContentBox");
        return string.IsNullOrEmpty(text) ? null : StBox.Parse(text);
    }

    /// <summary>
    /// 【可选】
    /// 设置出血区域
    /// 超出页面物理区域的额外区域，用于印刷时的出血处理
    /// </summary>
    /// <param name="bleedBox">出血区域</param>
    /// <returns>this</returns>
    public CtPageArea SetBleedBox(StBox bleedBox)
    {
        SetOfdEntity("BleedBox", bleedBox.ToString());
        return this;
    }

    /// <summary>
    /// 【可选】
    /// 获取出血区域
    /// 超出页面物理区域的额外区域，用于印刷时的出血处理
    /// </summary>
    /// <returns>出血区域</returns>
    public StBox? GetBleedBox()
    {
        var text = GetOfdElementText("BleedBox");
        return string.IsNullOrEmpty(text) ? null : StBox.Parse(text);
    }
}

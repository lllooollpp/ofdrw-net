using System.Collections.Generic;

namespace OfdrwNet.Reader.Model;

/// <summary>
/// OFD文档值对象
/// 
/// 对应Java版本的 org.ofdrw.reader.model.OFDDocumentVo
/// </summary>
public class OfdDocumentVo
{
    /// <summary>
    /// 文档路径
    /// </summary>
    public string DocPath { get; }

    /// <summary>
    /// 页面宽度
    /// </summary>
    public double PageWidth { get; }

    /// <summary>
    /// 页面高度
    /// </summary>
    public double PageHeight { get; }

    /// <summary>
    /// 页面列表
    /// </summary>
    public List<OfdPageVo> OfdPageVoList { get; set; }

    /// <summary>
    /// 印章注释列表
    /// </summary>
    public List<StampAnnotVo> StampAnnotVos { get; }

    /// <summary>
    /// 注释实体列表
    /// </summary>
    public List<AnnotationEntity> Annotations { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="docPath">文档路径</param>
    /// <param name="pageWidth">页面宽度</param>
    /// <param name="pageHeight">页面高度</param>
    /// <param name="ofdPageVoList">页面列表</param>
    /// <param name="stampAnnotVos">印章注释列表</param>
    /// <param name="annotations">注释列表</param>
    public OfdDocumentVo(
        string docPath,
        double pageWidth,
        double pageHeight,
        List<OfdPageVo> ofdPageVoList,
        List<StampAnnotVo> stampAnnotVos,
        List<AnnotationEntity> annotations)
    {
        DocPath = docPath;
        PageWidth = pageWidth;
        PageHeight = pageHeight;
        OfdPageVoList = ofdPageVoList;
        StampAnnotVos = stampAnnotVos;
        Annotations = annotations;
    }
}
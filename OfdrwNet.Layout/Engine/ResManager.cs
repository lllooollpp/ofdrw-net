using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using OfdrwNet.Core.BasicStructure.Doc;
using OfdrwNet.Core.BasicStructure.Ofd;
using OfdrwNet.Core.BasicStructure.Res;
using OfdrwNet.Core.BasicStructure.Res.Resources;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.CompositeObj;
using OfdrwNet.Core;
using OfdrwNet.Core.PageDescription.Color;
using OfdrwNet.Core.PageDescription.DrawParam;
using OfdrwNet.Core.Text.Font;
using OfdrwNet.Font;
using OfdrwNet.Reader;
using OFDReader = OfdrwNet.Reader.OfdReader;
using OFDDir = System.String;
using DocDir = System.String;
using ExistCtFont = System.Object;
using IOFDElement = OfdrwNet.Core.OfdElement;

namespace OfdrwNet.Layout.Engine
{
    /// <summary>
    /// 公共资源管理器
    /// <para>
    /// 管理待加入文档中所有资源
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-03-22 16:20:07
    /// </summary>
    public class ResManager
    {
        /// <summary>
        /// 自增的ID生成器
        /// </summary>
        private readonly Func<int> maxUnitIDProvider;

        /// <summary>
        /// OFD文档对象
        /// </summary>
        private OFDDir? root;

        /// <summary>
        /// 文档容器
        /// </summary>
        private DocDir docDir;

        /// <summary>
        /// 媒体资源列表
        /// <para>
        /// 位于文档资源列表
        /// </para>
        /// </summary>
        private MultiMedias? medias;

        /// <summary>
        /// 绘制参数列表
        /// <para>
        /// 位于文档资源列表
        /// </para>
        /// </summary>
        private DrawParams? drawParams;

        /// <summary>
        /// 字体资源列表
        /// <para>
        /// 位于公共资源列表
        /// </para>
        /// </summary>
        private OfdrwNet.Core.BasicStructure.Res.Fonts? fonts;

        /// <summary>
        /// 颜色空间的描述列表
        /// <para>
        /// 位于公共资源列表
        /// </para>
        /// </summary>
        private OfdrwNet.Core.BasicStructure.Res.ColorSpaces? colorSpaces;

        /// <summary>
        /// 矢量图像列表
        /// <para>
        /// 位于文档资源列表
        /// </para>
        /// </summary>
        private CompositeGraphicUnits? compositeGraphicUnits;

        /// <summary>
        /// 文档对象
        /// </summary>
        private Document document;

        /// <summary>
        /// 文档资源
        /// </summary>
        private Res? documentRes;

        /// <summary>
        /// 公共资源
        /// </summary>
        private Res? publicRes;

        /// <summary>
        /// 新增资源对象ID
        /// </summary>
        public List<StId> NewResIds { get; } = new();

        /// <summary>
        /// 绘制参数Hash
        /// <para>
        /// KEY: 资源对象的去除ID后的XML字符串的hashCode
        /// VALUE: 文档中的对象ID。
        /// </para>
        /// <para>
        /// 该缓存表用于解决绘制参数冗余造成的资源浪费。
        /// </para>
        /// </summary>
        private readonly Dictionary<int, StId> resObjHash = new();

        /// <summary>
        /// 创建资源管理器
        /// <para>
        /// 要求文档 Doc_N 路径中存在 Document.xml
        /// </para>
        /// </summary>
        /// <param name="root">文档根目录</param>
        /// <param name="docDir">文档虚拟容器，请确保文档容器中存在 Document.xml</param>
        /// <param name="maxUnitIDProvider">自增最大ID提供者</param>
        /// <exception cref="InvalidOperationException">文档解析异常</exception>
        public ResManager(OFDDir root, DocDir docDir, Func<int> maxUnitIDProvider)
        {
            this.root = root;
            this.docDir = docDir;
            this.maxUnitIDProvider = maxUnitIDProvider;

            try
            {
                document = docDir.GetDocument();
            }
            catch (FileNotFoundException e)
            {
                throw new InvalidOperationException("文档解析失败未能找到 Document.xml", e);
            }
            catch (XmlException e)
            {
                throw new InvalidOperationException("文档解析失败未能找到 Document.xml", e);
            }

            // 如果存在公共资源，尝试加载
            if (docDir.Exist(DocDir.PublicResFileName))
            {
                try
                {
                    publicRes = docDir.GetPublicRes();
                    ReloadRes(publicRes);
                }
                catch (FileNotFoundException)
                {
                    // ignore 文件不存在，不解析
                }
                catch (XmlException)
                {
                    throw new InvalidOperationException("已有 PublicRes.xml 资源文件解析失败");
                }
            }

            // 如果存在文档资源，尝试加载
            if (docDir.Exist(DocDir.DocumentResFileName))
            {
                try
                {
                    documentRes = docDir.GetDocumentRes();
                    ReloadRes(documentRes);
                }
                catch (FileNotFoundException)
                {
                    // ignore 文件不存在，不解析
                }
                catch (XmlException)
                {
                    throw new InvalidOperationException("已有 DocumentRes.xml 资源文件解析失败");
                }
            }
        }

        /// <summary>
        /// 创建资源管理器，
        /// <para>
        /// 要求文档 Doc_N 路径中存在 Document.xml
        /// </para>
        /// </summary>
        /// <param name="reader">OFD解析器</param>
        /// <exception cref="XmlException">文档解析异常</exception>
        /// <exception cref="FileNotFoundException">OFD文档结构非法</exception>
        public ResManager(OFDReader reader)
        {
            var ofdDir = reader.GetOFDDir();
            var ofd = ofdDir.GetOfd();
            // 资源定位器
            var resourceLocator = reader.GetResourceLocator();
            // 找到 Document.xml文件并且序列化
            var docRoot = ofd.GetDocBody().GetDocRoot();
            var document = resourceLocator.Get(docRoot, () => new Document());
            var commonData = document.GetCommonData();

            root = ofdDir;
            docDir = ofdDir.ObtainDocDefault();
            this.document = document;
            maxUnitIDProvider = () => commonData.GetMaxUnitID()?.GetId() ?? 0;

            try
            {
                resourceLocator.Save();
                resourceLocator.Cd(docDir);
                foreach (var loc in commonData.GetPublicResList())
                {
                    publicRes = resourceLocator.Get(loc, () => new Res());
                    ReloadRes(publicRes);
                }
                foreach (var loc in commonData.GetDocumentResList())
                {
                    documentRes = resourceLocator.Get(loc, () => new Res());
                    ReloadRes(documentRes);
                }
            }
            catch (Exception)
            {
                // 忽略异常
            }
            finally
            {
                resourceLocator.Restore();
            }
        }

        /// <summary>
        /// 重载公共资源缓存
        /// </summary>
        private void ReloadRes(Res res)
        {
            var resources = res.GetResources();
            if (resources == null || !resources.Any())
            {
                return;
            }
            
            foreach (var resource in resources)
            {
                // 获取各个集合下的资源对象
                var elements = resource.GetElements();
                if (elements == null || !elements.Any())
                {
                    continue;
                }
                
                foreach (var ctResObj in elements)
                {
                    // 获取原来资源对象的ID
                    var id = StId.GetInstance(ctResObj.GetAttribute("ID")?.Value);
                    if (id == null)
                    {
                        return;
                    }
                    
                    // 遍历每一个资源对象，复制对象，删除对象ID，序列化为XML字符串
                    var copy = new XElement(ctResObj);
                    copy.Attribute("ID")?.Remove();
                    var key = copy.ToString();
                    resObjHash[key.GetHashCode()] = id;
                }
            }
        }

        /// <summary>
        /// 增加字体资源
        /// <para>
        /// 如果字体已经被加入，那么不会重复加入
        /// </para>
        /// </summary>
        /// <param name="font">字体描述对象</param>
        /// <returns>字体的对象ID</returns>
        /// <exception cref="IOException">文件复制异常</exception>
        public StId AddFont(OfdrwNet.Font.Font font)
        {
            return AddFontRet(font).GetID();
        }

        /// <summary>
        /// 增加字体资源 并获取 添加的字体对象
        /// <para>
        /// 如果字体已经被加入，那么不会重复加入
        /// </para>
        /// </summary>
        /// <param name="font">字体描述对象</param>
        /// <returns>字体的对象</returns>
        /// <exception cref="IOException">文件复制异常</exception>
        public CtFont AddFontRet(OfdrwNet.Font.Font font)
        {
            // 获取字体全名
            var familyName = font.GetFamilyName();
            // 新建一个OFD字体对象
            var ctFont = new CtFont()
                .SetFontName(font.GetName())
                .SetFamilyName(familyName);
                
            var fontFile = font.GetFontFile();
            if (!string.IsNullOrEmpty(fontFile) && font.IsEmbeddable())
            {
                // 将字体文件加入到文档容器中
                fontFile = docDir.AddResourceWithPath(fontFile);

                var filename = Path.GetFileName(fontFile);
                // 若资源文件中的相对路径不是Res，那么采用绝对路径
                if (publicRes != null && !StLoc.Equal("Res", publicRes.GetBaseLoc()))
                {
                    filename = docDir.GetAbsLoc().Cat("Res").Cat(filename).ToString();
                }
                ctFont.SetFontFile(filename);
            }

            // 设置特殊字族属性
            if (familyName != null)
            {
                switch (familyName.ToLower())
                {
                    case "serif":
                        ctFont.SetSerif(true);
                        break;
                    case "bold":
                        ctFont.SetBold(true);
                        break;
                    case "italic":
                        ctFont.SetItalic(true);
                        break;
                    case "fixedwidth":
                        ctFont.SetFixedWidth(true);
                        break;
                }
            }
            AddRawWithCache(ctFont);
            return ctFont;
        }

        /// <summary>
        /// 加入一个图片资源
        /// <para>
        /// 如果图片已经存在那么不会重复加入
        /// </para>
        /// </summary>
        /// <param name="imgPath">图片路径，请避免资源和文档中已经存在的资源重复</param>
        /// <returns>资源ID</returns>
        /// <exception cref="IOException">文件复制异常</exception>
        public StId AddImage(string imgPath)
        {
            // 将文件加入资源容器中，并获取资源在文件中的绝对路径
            var imgCtnPath = docDir.AddResourceWithPath(imgPath);
            // 获取在容器中的文件名称
            var filename = Path.GetFileName(imgCtnPath);
            // 若资源文件中的相对路径不是Res，那么采用绝对路径
            if (documentRes != null && !StLoc.Equal("Res", documentRes.GetBaseLoc()))
            {
                filename = docDir.GetAbsLoc().Cat("Res").Cat(filename).ToString();
            }

            // 获取图片文件后缀名称
            var fileSuffix = PictureFormat(filename);
            // 创建图片对象
            var multiMedia = new CtMultiMedia()
                .SetType(MediaType.Image)
                .SetFormat(fileSuffix)
                .SetMediaFile(StLoc.GetInstance(filename));
            // 添加到资源列表中
            return AddRawWithCache(multiMedia);
        }

        /// <summary>
        /// 加入一个绘制参数
        /// <para>
        /// 如果存在相同或类似的绘制参数则不会重复添加。
        /// </para>
        /// </summary>
        /// <param name="param">绘制参数</param>
        /// <returns>资源ID</returns>
        public StId? AddDrawParam(CtDrawParam? param)
        {
            if (param == null)
            {
                return null;
            }
            // 复制绘制参数，防止出现节点重复添加问题。
            param = param.Clone();

            return AddRawWithCache(param.Clone());
        }

        /// <summary>
        /// 根据图片名称推断图片格式
        /// </summary>
        /// <param name="fileName">图片文件名称</param>
        /// <returns>图片格式字符串</returns>
        private string PictureFormat(string fileName)
        {
            var fileSuffix = Path.GetExtension(fileName).TrimStart('.').ToUpper();
            return fileSuffix switch
            {
                "JPG" => "JPEG",
                "TIF" => "TIFF",
                _ => fileSuffix
            };
        }

        /// <summary>
        /// 获取公共资源清单
        /// <para>
        /// 如： 图形、字体等需要共用的资源
        /// </para>
        /// </summary>
        /// <returns>公共资源清单</returns>
        public Res PubRes()
        {
            if (publicRes != null)
            {
                return publicRes;
            }
            // 如果不存在那么创建一个公共资源清单，容器目录为文档根目录下的Res目录
            var pubRes = new Res().SetBaseLoc(StLoc.GetInstance("Res"));
            docDir.SetPublicRes(pubRes);
            var commonData = document.GetCommonData();
            if (commonData == null)
            {
                commonData = new CtCommonData();
                document.SetCommonData(commonData);
            }
            commonData.AddPublicRes(StLoc.GetInstance("PublicRes.xml"));
            publicRes = pubRes;
            return publicRes;
        }

        /// <summary>
        /// 文档资源清单
        /// <para>
        /// 与文档相关的资源：图片、视频等
        /// </para>
        /// </summary>
        /// <returns>文档资源清单</returns>
        public Res DocRes()
        {
            if (documentRes != null)
            {
                return documentRes;
            }

            // 如果不存在那么创建一个公共资源清单，容器目录为文档根目录下的Res目录
            var docRes = new Res().SetBaseLoc(StLoc.GetInstance("Res"));
            docDir.SetDocumentRes(docRes);
            var commonData = document.GetCommonData();
            if (commonData == null)
            {
                commonData = new CtCommonData();
                document.SetCommonData(commonData);
            }
            commonData.AddDocumentRes(StLoc.GetInstance("DocumentRes.xml"));
            documentRes = docRes;
            return documentRes;
        }

        /// <summary>
        /// 直接向资源列表中加入资源对象
        /// <para>
        /// 加入资源时将优先检查缓存是否存在完全一致的资源，如果存在则复用对象。
        /// </para>
        /// <para>
        /// 注意：加入对象的ID将被忽略，对象ID有资源管理器生成并设置。
        /// </para>
        /// </summary>
        /// <param name="resObj">资源对象</param>
        /// <returns>对象在文档中的资源ID</returns>
        public StId? AddRawWithCache(IOFDElement? resObj)
        {
            if (resObj == null)
            {
                return null;
            }

            // 移除对象上已经存在的用于基于资源本身的Hash值
            resObj.RemoveAttr("ID");
            var key = resObj.AsXML().GetHashCode();

            if (resObjHash.TryGetValue(key, out var objId))
            {
                // 文档中已经存在相同资源，则复用该资源。
                resObj.SetObjID(objId);
                return objId;
            }
            else
            {
                // 文档中不存在该资源则资源ID，并缓存
                objId = new StId(maxUnitIDProvider());
                resObj.SetObjID(objId);
                // 记录资源ID
                NewResIds.Add(objId);
                resObjHash[key] = objId;
            }

            // 判断资源类型加入到合适的资源列表中
            switch (resObj)
            {
                case CtColorSpace colorSpace:
                    {
                        var resMenu = PubRes();
                        if (colorSpaces == null)
                        {
                            colorSpaces = new ColorSpaces();
                            resMenu.AddResource(colorSpaces);
                        }
                        colorSpaces.AddColorSpace(colorSpace);
                        break;
                    }
                case CtFont font:
                    {
                        var resMenu = PubRes();
                        if (fonts == null)
                        {
                            fonts = new Fonts();
                            resMenu.AddResource(fonts);
                        }
                        fonts.AddFont(font);
                        break;
                    }
                case CtDrawParam drawParam:
                    {
                        var resMenu = DocRes();
                        if (drawParams == null)
                        {
                            drawParams = new DrawParams();
                            resMenu.AddResource(drawParams);
                        }
                        drawParams.AddDrawParam(drawParam);
                        break;
                    }
                case CtMultiMedia multiMedia:
                    {
                        var resMenu = DocRes();
                        if (medias == null)
                        {
                            medias = new MultiMedias();
                            resMenu.AddResource(medias);
                        }
                        medias.AddMultiMedia(multiMedia);
                        break;
                    }
                case CtVectorG vectorG:
                    {
                        var resMenu = DocRes();
                        if (compositeGraphicUnits == null)
                        {
                            compositeGraphicUnits = new CompositeGraphicUnits();
                            resMenu.AddResource(compositeGraphicUnits);
                        }
                        compositeGraphicUnits.AddCompositeGraphicUnit(vectorG);
                        break;
                    }
            }
            return objId;
        }

        /// <summary>
        /// 通过字族名获取字体对象，如果无法找到则返还null
        /// </summary>
        /// <param name="name">字体名称</param>
        /// <returns>字体对象 或 null</returns>
        public ExistCtFont? GetFont(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            name = name.ToLower();
            CtFont? res = null;
            
            // 尝试从公共资源中获取 字体清单
            var resMenu = PubRes();
            var fontsList = resMenu.GetFonts();
            foreach (var fonts in fontsList)
            {
                var arr = fonts.GetFonts();
                foreach (var ctFont in arr)
                {
                    // 忽略大小写的比较
                    var fontName = ctFont.GetFontName()?.ToLower();
                    var familyName = ctFont.GetFamilyName()?.ToLower();

                    if (name.Equals(fontName) || name.Equals(familyName))
                    {
                        // 找到最后一个匹配的字体
                        res = ctFont;
                    }
                }
            }
            
            if (res == null)
            {
                // 尝试从文档资源中获取 字体清单
                resMenu = DocRes();
                fontsList = resMenu.GetFonts();
                foreach (var fonts in fontsList)
                {
                    var arr = fonts.GetFonts();
                    foreach (var ctFont in arr)
                    {
                        // 忽略大小写的比较
                        var fontName = ctFont.GetFontName()?.ToLower();
                        var familyName = ctFont.GetFamilyName()?.ToLower();
                        
                        if (name.Equals(fontName) || name.Equals(familyName))
                        {
                            res = ctFont;
                        }
                    }
                }
            }

            if (res == null)
            {
                // 无法找到字体
                return null;
            }

            // 获取字体文件的绝对路径
            var loc = res.GetFontFile();
            string? p = null;
            if (loc != null && root != null)
            {
                var abs = Abs(resMenu, loc);
                try
                {
                    p = root.GetFile(abs.GetFileName());
                }
                catch (FileNotFoundException)
                {
                    // ignore
                }
            }

            return new ExistCtFont(res, p);
        }

        /// <summary>
        /// 设置文档的根节点
        /// </summary>
        /// <param name="root">根节点</param>
        public void SetRoot(OFDDir root)
        {
            this.root = root;
        }

        /// <summary>
        /// 获取文档根容器
        /// </summary>
        /// <returns>文档根容器</returns>
        public OFDDir? GetRoot()
        {
            return root;
        }

        /// <summary>
        /// 资源完整路径
        /// </summary>
        /// <param name="res">资源清单</param>
        /// <param name="target">目标路径</param>
        /// <returns>相对于文件的绝对路径</returns>
        private StLoc Abs(Res res, StLoc target)
        {
            if (target.IsRootPath())
            {
                // 绝对路径
                return target;
            }
            StLoc absLoc;
            var baseLoc = res.GetBaseLoc();
            if (baseLoc != null && baseLoc.IsRootPath())
            {
                // 资源文件的通用存储路径 为根路径时直接在此基础上拼接
                absLoc = baseLoc;
            }
            else
            {
                // 资源文件的通用存储路径 为相对路径时，拼接当前文档路径
                absLoc = docDir.GetAbsLoc().Cat(baseLoc);
            }
            return absLoc.Cat(target);
        }

        /// <summary>
        /// 获取当前操作的文档容器
        /// </summary>
        /// <returns>文档容器</returns>
        public DocDir GetDocDir()
        {
            return docDir;
        }
    }
}
package org.ofdrw.layout.engine;

import org.dom4j.DocumentException;
import org.dom4j.Element;
import org.ofdrw.core.OFDElement;
import org.ofdrw.core.basicStructure.doc.CT_CommonData;
import org.ofdrw.core.basicStructure.doc.Document;
import org.ofdrw.core.basicStructure.ofd.OFD;
import org.ofdrw.core.basicStructure.res.CT_MultiMedia;
import org.ofdrw.core.basicStructure.res.MediaType;
import org.ofdrw.core.basicStructure.res.OFDResource;
import org.ofdrw.core.basicStructure.res.Res;
import org.ofdrw.core.basicStructure.res.resources.*;
import org.ofdrw.core.basicType.ST_ID;
import org.ofdrw.core.basicType.ST_Loc;
import org.ofdrw.core.compositeObj.CT_VectorG;
import org.ofdrw.core.pageDescription.color.colorSpace.CT_ColorSpace;
import org.ofdrw.core.pageDescription.drawParam.CT_DrawParam;
import org.ofdrw.core.text.font.CT_Font;
import org.ofdrw.font.Font;
import org.ofdrw.pkg.container.DocDir;
import org.ofdrw.pkg.container.OFDDir;
import org.ofdrw.reader.OFDReader;
import org.ofdrw.reader.ResourceLocator;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Objects;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * 公共资源管理器
 * <p>
 * 管理待加入文档中所有资源
 *
 * @author 权观宇
 * @since 2020-03-22 16:20:07
 */
public class ResManager {


    /**
     * 自增的ID生成器
     */
    private AtomicInteger maxUnitID;

    /**
     * OFD文档对象
     */
    private OFDDir root;


    /**
     * 文档容器
     */
    private DocDir docDir;

    /**
     * 媒体资源列表
     * <p>
     * 位于文档资源列表
     */
    private MultiMedias medias;

    /**
     * 绘制参数列表
     * <p>
     * 位于文档资源列表
     */
    private DrawParams drawParams;


    /**
     * 字体资源列表
     * <p>
     * 位于公共资源列表
     */
    private Fonts fonts;

    /**
     * 颜色空间的描述列表
     * <p>
     * 位于公共资源列表
     */
    private ColorSpaces colorSpaces;

    /**
     * 矢量图像列表
     * <p>
     * 位于文档资源列表
     */
    private CompositeGraphicUnits compositeGraphicUnits;

    /**
     * 文档对象
     */
    private Document document;

    /**
     * 文档资源
     */
    private Res documentRes;

    /**
     * 公共资源
     */
    private Res publicRes;

    /**
     * 新增资源对象ID
     */
    public ArrayList<ST_ID> newResIds = new ArrayList<>();

    /**
     * 绘制参数Hash
     * <p>
     * KEY: 资源对象的去除ID后的XML字符串的hashCode
     * VALUE: 文档中的对象ID。
     * <p>
     * 该缓存表用于解决绘制参数冗余造成的资源浪费。
     */
    private final HashMap<Integer, ST_ID> resObjHash = new HashMap<>();

    private ResManager() {
    }

    /**
     * 创建资源管理器
     * <p>
     * 要求文档 Doc_N 路径中存在 Document.xml
     *
     * @param root      文档根目录
     * @param docDir    文档虚拟容器，请确保文档容器中存在 Document.xml
     * @param maxUnitID 自增最大ID提供者
     * @throws RuntimeException 文档解析异常
     */
    public ResManager(OFDDir root, DocDir docDir, AtomicInteger maxUnitID) {
        this();
        this.root = root;
        this.docDir = docDir;
        this.maxUnitID = maxUnitID;

        try {
            this.document = docDir.getDocument();
        } catch (FileNotFoundException | DocumentException e) {
            throw new RuntimeException("文档解析失败未能找到 Document.xml", e);
        }

        // 如果存在公共资源，尝试加载
        if (docDir.exist(DocDir.PublicResFileName)) {
            try {
                this.publicRes = docDir.getPublicRes();
                reloadRes(publicRes);
            } catch (FileNotFoundException e) {
                // ignore 文件不存在，不解析
            } catch (DocumentException e) {
                throw new RuntimeException("已有 PublicRes.xml 资源文件解析失败", e);
            }
        }

        // 如果存在文档资源，尝试加载
        if (docDir.exist(DocDir.DocumentResFileName)) {
            try {
                this.documentRes = docDir.getDocumentRes();
                reloadRes(documentRes);
            } catch (FileNotFoundException e) {
                // ignore 文件不存在，不解析
            } catch (DocumentException e) {
                throw new RuntimeException("已有 DocumentRes.xml 资源文件解析失败", e);
            }
        }

    }

    /**
     * 创建资源管理器，
     *
     * <p>
     * 要求文档 Doc_N 路径中存在 Document.xml
     *
     * @param reader OFD解析器
     * @throws DocumentException     文档解析异常
     * @throws FileNotFoundException OFD文档结构非法
     */
    public ResManager(OFDReader reader) throws FileNotFoundException, DocumentException {
        this();

        OFDDir ofdDir = reader.getOFDDir();
        OFD ofd = ofdDir.getOfd();
        // 资源定位器
        ResourceLocator resourceLocator = reader.getResourceLocator();
        // 找到 Document.xml文件并且序列化
        ST_Loc docRoot = ofd.getDocBody().getDocRoot();
        Document document = resourceLocator.get(docRoot, Document::new);
        CT_CommonData commonData = document.getCommonData();

        this.root = ofdDir;
        this.docDir = ofdDir.obtainDocDefault();
        this.document = document;
        this.maxUnitID = new AtomicInteger(commonData.getMaxUnitID().getId().intValue());

        try {
            resourceLocator.save();
            resourceLocator.cd(docDir);
            for (ST_Loc loc : commonData.getPublicResList()) {
                this.publicRes = resourceLocator.get(loc, Res::new);
                reloadRes(this.publicRes);
            }
            for (ST_Loc loc : commonData.getDocumentResList()) {
                this.documentRes = resourceLocator.get(loc, Res::new);
                reloadRes(documentRes);
            }
        } catch (Exception e) {
            // 忽略异常
        } finally {
            resourceLocator.restore();
        }
    }

    /**
     * 创建资源管理
     *
     * <p>
     * 要求文档 Doc_N 路径中存在 Document.xml
     *
     * @param docDir    文档虚拟容器，请确保文档容器中存在 Document.xml
     * @param maxUnitID 自增最大ID提供者
     * @deprecated 缺少根容器可能导致部分资源无法获取，请使用 {@link #ResManager(OFDDir, DocDir, AtomicInteger)}
     */
    @Deprecated
    public ResManager(DocDir docDir, AtomicInteger maxUnitID) {
        this(null, docDir, maxUnitID);
    }

    /**
     * 重载公共资源缓存
     */
    private void reloadRes(Res res) {
        List<OFDResource> resources = res.getResources();
        if (resources == null || resources.isEmpty()) {
            return;
        }
        for (OFDResource resource : resources) {
            // 获取各个集合下的资源对象
            List<Element> elements = resource.elements();
            if (elements == null || elements.isEmpty()) {
                continue;
            }
            for (Element ctResObj : elements) {
                // 获取原来资源对象的ID
                ST_ID id = ST_ID.getInstance(ctResObj.attributeValue("ID"));
                if (id == null) {
                    return;
                }
                // 遍历每一个资源对象，复制对象，删除对象ID，序列化为XML字符串
                Element copy = (Element) ctResObj.clone();
                copy.remove(copy.attribute("ID"));
                String key = copy.asXML();
                resObjHash.put(key.hashCode(), id);
            }
        }

    }

    /**
     * 增加字体资源
     * <p>
     * 如果字体已经被加入，那么不会重复加入
     *
     * @param font 字体描述对象
     * @return 字体的对象ID
     * @throws IOException 文件复制异常
     */
    public ST_ID addFont(Font font) throws IOException {
        return addFontRet(font).getID();
    }

    /**
     * 增加字体资源 并获取 添加的字体对象
     * <p>
     * 如果字体已经被加入，那么不会重复加入
     *
     * @param font 字体描述对象
     * @return 字体的对象
     * @throws IOException 文件复制异常
     */
    public CT_Font addFontRet(Font font) throws IOException {
        // 获取字体全名
        String familyName = font.getFamilyName();
        // 新建一个OFD字体对象
        CT_Font ctFont = new CT_Font()
                .setFontName(font.getName())
                .setFamilyName(familyName);
        Path fontFile = font.getFontFile();
        if (fontFile != null && font.isEmbeddable()) {
            // 将字体文件加入到文档容器中
            fontFile = docDir.addResourceWithPath(fontFile);

            String filename = fontFile.getFileName().toString();
            // 若资源文件中的相对路径不是Res，那么采用绝对路径
            if (publicRes != null && !ST_Loc.equal("Res", publicRes.getBaseLoc())) {
                filename = docDir.getAbsLoc().cat("Res").cat(filename).toString();
            }
            ctFont.setFontFile(filename);
        }

        // 设置特殊字族属性
        if (familyName != null) {
            switch (familyName.toLowerCase()) {
                case "serif":
                    ctFont.setSerif(true);
                    break;
                case "bold":
                    ctFont.setBold(true);
                    break;
                case "italic":
                    ctFont.setItalic(true);
                    break;
                case "fixedwidth":
                    ctFont.setFixedWidth(true);
                    break;
            }
        }
        addRawWithCache(ctFont);
        return ctFont;
    }

    /**
     * 加入一个图片资源
     * <p>
     * 如果图片已经存在那么不会重复加入
     *
     * @param imgPath 图片路径，请避免资源和文档中已经存在的资源重复
     * @return 资源ID
     * @throws IOException 文件复制异常
     */
    public ST_ID addImage(Path imgPath) throws IOException {
        // 将文件加入资源容器中，并获取资源在文件中的绝对路径
        Path imgCtnPath = docDir.addResourceWithPath(imgPath);
        // 获取在容器中的文件名称
        String filename = imgCtnPath.getFileName().toString();
        // 若资源文件中的相对路径不是Res，那么采用绝对路径
        if (documentRes != null && !ST_Loc.equal("Res", documentRes.getBaseLoc())) {
            filename = docDir.getAbsLoc().cat("Res").cat(filename).toString();
        }

        // 获取图片文件后缀名称
        String fileSuffix = pictureFormat(filename);
        // 创建图片对象
        CT_MultiMedia multiMedia = new CT_MultiMedia()
                .setType(MediaType.Image)
                .setFormat(fileSuffix)
                .setMediaFile(ST_Loc.getInstance(filename));
        // 添加到资源列表中
        return addRawWithCache(multiMedia);
    }

    /**
     * 加入一个绘制参数
     * <p>
     * 如果存在相同或类似的绘制参数则不会重复添加。
     *
     * @param param 绘制参数
     * @return 资源ID
     */
    public ST_ID addDrawParam(CT_DrawParam param) {
        if (param == null) {
            return null;
        }
        // 复制绘制参数，防止出现节点重复添加问题。
        param = param.clone();

        return addRawWithCache(param.clone());
    }

    /**
     * 根据图片名称推断图片格式
     *
     * @param fileName 图片文件名称
     * @return 图片格式字符串
     */
    private String pictureFormat(String fileName) {
        String fileSuffix = fileName.substring(fileName.lastIndexOf(".") + 1).toUpperCase();
        switch (fileSuffix) {
            case "JPG":
                return "JPEG";
            case "TIF":
                return "TIFF";
            default:
                return fileSuffix;
        }
    }

    /**
     * 获取公共资源清单
     * <p>
     * 如： 图形、字体等需要共用的资源
     *
     * @return 公共资源清单
     */
    public Res pubRes() {
        if (publicRes != null) {
            return publicRes;
        }
        // 如果不存在那么创建一个公共资源清单，容器目录为文档根目录下的Res目录
        Res pubRes = new Res().setBaseLoc(ST_Loc.getInstance("Res"));
        docDir.setPublicRes(pubRes);
        CT_CommonData commonData = document.getCommonData();
        if (commonData == null) {
            commonData = new CT_CommonData();
            document.setCommonData(commonData);
        }
        commonData.addPublicRes(ST_Loc.getInstance("PublicRes.xml"));
        this.publicRes = pubRes;
        return publicRes;
    }

    /**
     * 文档资源清单
     * <p>
     * 与文档相关的资源：图片、视频等
     *
     * @return 文档资源清单
     */
    public Res docRes() {
        if (documentRes != null) {
            return documentRes;
        }

        // 如果不存在那么创建一个公共资源清单，容器目录为文档根目录下的Res目录
        Res docRes = new Res().setBaseLoc(ST_Loc.getInstance("Res"));
        docDir.setDocumentRes(docRes);
        CT_CommonData commonData = document.getCommonData();
        if (commonData == null) {
            commonData = new CT_CommonData();
            document.setCommonData(commonData);
        }
        commonData.addDocumentRes(ST_Loc.getInstance("DocumentRes.xml"));
        documentRes = docRes;
        return documentRes;
    }


    /**
     * 直接向资源列表中加入资源对象
     * <p>
     * 注意：该方法是一个原生方法，具有一定的资源重复风险。
     *
     * @param resObj 资源对象
     * @return this
     * @deprecated {@link #addRawWithCache(OFDElement)}
     */
    @Deprecated
    public ResManager addRaw(OFDElement resObj) {
        if (resObj == null) {
            return this;
        }

        if (resObj instanceof CT_ColorSpace) {
            Res resMenu = pubRes();
            if (colorSpaces == null) {
                this.colorSpaces = new ColorSpaces();
                resMenu.addResource(colorSpaces);
            }
            colorSpaces.addColorSpace((CT_ColorSpace) resObj);
        } else if (resObj instanceof CT_Font) {
            Res resMenu = pubRes();
            if (fonts == null) {
                this.fonts = new Fonts();
                resMenu.addResource(fonts);
            }
            fonts.addFont((CT_Font) resObj);
        } else if (resObj instanceof CT_DrawParam) {
            Res resMenu = docRes();
            if (drawParams == null) {
                this.drawParams = new DrawParams();
                resMenu.addResource(drawParams);
            }
            drawParams.addDrawParam((CT_DrawParam) resObj);
        } else if (resObj instanceof CT_MultiMedia) {
            Res resMenu = docRes();
            if (medias == null) {
                this.medias = new MultiMedias();
                resMenu.addResource(medias);
            }
            medias.addMultiMedia((CT_MultiMedia) resObj);
        } else if (resObj instanceof CT_VectorG) {
            Res resMenu = docRes();
            if (compositeGraphicUnits == null) {
                this.compositeGraphicUnits = new CompositeGraphicUnits();
                resMenu.addResource(compositeGraphicUnits);
            }
            compositeGraphicUnits.addCompositeGraphicUnit((CT_VectorG) resObj);
        }
        return this;
    }


    /**
     * 直接向资源列表中加入资源对象
     * <p>
     * 加入资源时将优先检查缓存是否存在完全一致的资源，如果存在则复用对象。
     * <p>
     * 注意：加入对象的ID将被忽略，对象ID有资源管理器生成并设置。
     *
     * @param resObj 资源对象
     * @return 对象在文档中的资源ID
     */
    public ST_ID addRawWithCache(OFDElement resObj) {
        if (resObj == null) {
            return null;
        }

        // 移除对象上已经存在的用于基于资源本身的Hash值
        resObj.removeAttr("ID");
        int key = resObj.asXML().hashCode();

        ST_ID objId = this.resObjHash.get(key);
        if (objId != null) {
            // 文档中已经存在相同资源，则复用该资源。
            resObj.setObjID(objId);
            return objId;
        } else {
            // 文档中不存在该资源则资源ID，并缓存
            objId = new ST_ID(maxUnitID.incrementAndGet());
            resObj.setObjID(objId);
            // 记录资源ID
            newResIds.add(objId);
            this.resObjHash.put(key, objId);
        }

        // 判断资源类型加入到合适的资源列表中
        if (resObj instanceof CT_ColorSpace) {
            Res resMenu = pubRes();
            if (colorSpaces == null) {
                this.colorSpaces = new ColorSpaces();
                resMenu.addResource(colorSpaces);
            }
            colorSpaces.addColorSpace((CT_ColorSpace) resObj);
        } else if (resObj instanceof CT_Font) {
            Res resMenu = pubRes();
            if (fonts == null) {
                this.fonts = new Fonts();
                resMenu.addResource(fonts);
            }
            fonts.addFont((CT_Font) resObj);
        } else if (resObj instanceof CT_DrawParam) {
            Res resMenu = docRes();
            if (drawParams == null) {
                this.drawParams = new DrawParams();
                resMenu.addResource(drawParams);
            }
            drawParams.addDrawParam((CT_DrawParam) resObj);
        } else if (resObj instanceof CT_MultiMedia) {
            Res resMenu = docRes();
            if (medias == null) {
                this.medias = new MultiMedias();
                resMenu.addResource(medias);
            }
            medias.addMultiMedia((CT_MultiMedia) resObj);
        } else if (resObj instanceof CT_VectorG) {
            Res resMenu = docRes();
            if (compositeGraphicUnits == null) {
                this.compositeGraphicUnits = new CompositeGraphicUnits();
                resMenu.addResource(compositeGraphicUnits);
            }
            compositeGraphicUnits.addCompositeGraphicUnit((CT_VectorG) resObj);
        }
        return objId;
    }

    /**
     * 通过字族名获取字体对象，如果无法找到则返还null
     *
     * @param name 字体名称
     * @return 字体对象 或 null
     */
    public ExistCTFont getFont(String name) {
        if ("".equals(name) || name == null) {
            return null;
        }
        name = name.toLowerCase();
        CT_Font res = null;
        // 尝试从公共资源中获取 字体清单
        Res resMenu = pubRes();
        List<Fonts> fontsList = resMenu.getFonts();
        for (Fonts fonts : fontsList) {
            List<CT_Font> arr = fonts.getFonts();
            for (CT_Font ctFont : arr) {
                // 忽略大小写的比较
                String fontName = ctFont.getFontName();
                if (fontName != null) {
                    fontName = fontName.toLowerCase();
                }

                String familyName = ctFont.getFamilyName();
                if (familyName != null) {
                    familyName = familyName.toLowerCase();
                }

                if (name.equals(fontName) || name.equals(familyName)) {
                    // 找到最后一个匹配的字体
                    res = ctFont;
                }
            }
        }
        if (res == null) {
            // 尝试从文档资源中获取 字体清单
            resMenu = docRes();
            fontsList = resMenu.getFonts();
            for (Fonts fonts : fontsList) {
                List<CT_Font> arr = fonts.getFonts();
                for (CT_Font ctFont : arr) {
                    // 忽略大小写的比较
                    String fontName = ctFont.getFontName();
                    if (fontName != null) {
                        fontName = fontName.toLowerCase();
                    }

                    String familyName = ctFont.getFamilyName();
                    if (familyName != null) {
                        familyName = familyName.toLowerCase();
                    }
                    if (name.equals(fontName) || name.equals(familyName)) {
                        res = ctFont;
                    }
                }
            }
        }

        if (res == null) {
            // 无法找到字体
            return null;
        }

        // 获取字体文件的绝对路径
        ST_Loc loc = res.getFontFile();
        Path p = null;
        if (loc != null && root != null) {
            ST_Loc abs = abs(resMenu, loc);
            try {
                p = root.getFile(abs.getFileName());
            } catch (FileNotFoundException e) {
                // ignore
            }
        }

        return new ExistCTFont(res, p);
    }

    /**
     * 设置文档的根节点
     *
     * @param root 根节点
     */
    public void setRoot(OFDDir root) {
        this.root = root;
    }

    /**
     * 获取文档根容器
     *
     * @return 文档根容器
     */
    public OFDDir getRoot() {
        return root;
    }

    /**
     * 资源完整路径
     *
     * @param res    资源清单
     * @param target 目标路径
     * @return 相对于文件的绝对路径
     */
    private ST_Loc abs(Res res, ST_Loc target) {
        if (target.isRootPath()) {
            // 绝对路径
            return target;
        }
        ST_Loc absLoc = null;
        ST_Loc base = res.getBaseLoc();
        if (base != null && base.isRootPath()) {
            // 资源文件的通用存储路径 为根路径时直接在此基础上拼接
            absLoc = base;
        } else {
            // 资源文件的通用存储路径 为相对路径时，拼接当前文档路径
            absLoc = docDir.getAbsLoc().cat(base);
        }
        return absLoc.cat(target);
    }

    /**
     * 获取新加入的资源ID
     *
     * @return 新加入的资源ID
     */
    public ArrayList<ST_ID> getNewResIds() {
        return newResIds;
    }

    /**
     * 获取当前操作的文档容器
     *
     * @return 文档容器
     */
    public DocDir getDocDir() {
        return docDir;
    }
}

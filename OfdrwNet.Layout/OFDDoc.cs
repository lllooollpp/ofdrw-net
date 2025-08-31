using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using OfdrwNet.Core.Attachment;
using OfdrwNet.Core.BasicStructure.Doc;
using OfdrwNet.Core.BasicStructure.Ofd;
using OfdrwNet.Core.BasicStructure.PageObj;
using OfdrwNet.Core.BasicStructure.PageTree;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Layout.Edit;
using OfdrwNet.Layout.Element;
using OfdrwNet.Layout.Engine;
using OfdrwNet.Layout.Engine.Render;
using OfdrwNet.Layout.Handler;
using OfdrwNet.Packaging.Container;
using OfdrwNet.Reader;
using OFDReader = OfdrwNet.Reader.OfdReader;
using OFDDir = OfdrwNet.Packaging.Container.OFDDir;
using DocDir = OfdrwNet.Packaging.Container.DocDir;
using StreamCollect = System.Collections.Generic.List<System.IO.Stream>;
using AnnotationRender = OfdrwNet.Layout.Edit.AnnotationRender;
using RenderFinishHandler = System.Action<int, OfdrwNet.Packaging.Container.OFDDir, int>;
using OfdrwNet.Layout.Exceptions;
using OfdrwNet.Core.Annotation;

namespace OfdrwNet.Layout
{
    /// <summary>
    /// Virtual Document 虚拟文档对象
    /// <para>
    /// 与 <see cref="Document"/> 区别
    /// </para>
    /// <para>
    /// 使用API的方式构造OFD文档，并打包为OFD文件。
    /// </para>
    /// 
    /// 作者: 权观宇
    /// 起始时间: 2020-3-17 20:13:51
    /// </summary>
    public class OFDDoc : IDisposable
    {
        private OFDReader? reader;
        private OFDDir? ofdDir;
        private string? outPath;
        private Stream? outStream;
        private int maxUnitID = 0;
        private ResManager? prm;
        private AnnotationRender? annotationRender;
        private readonly List<Div> streamQueue = new();
        private readonly List<VirtualPage> vPageList = new();
        private readonly List<StreamCollect> sPageList = new();
        private PageLayout pageLayout = PageLayout.A4();
        private CtCommonData? cdata;
        private bool closed = false;
        private Document? ofdDocument;
        private DocDir? operateDocDir;
        private RenderFinishHandler? renderingEndHandler;
        private VPageHandler? onPageHandler = null;

        /// <summary>
        /// 在指定路径位置上创建一个OFD文件
        /// </summary>
        /// <param name="outPath">OFD输出路径</param>
        public OFDDoc(string outPath) : this()
        {
            if (string.IsNullOrEmpty(outPath))
                throw new ArgumentException("OFD文件存储路径(outPath)为空");
            if (Directory.Exists(outPath))
                throw new ArgumentException("OFD文件存储路径(outPath)不能是目录");
            
            var parent = Path.GetDirectoryName(Path.GetFullPath(outPath));
            if (parent == null || !Directory.Exists(parent))
                throw new ArgumentException($"OFD文件存储路径(outPath)上级目录 [{parent}] 不存在");
            
            this.outPath = outPath;
        }

        /// <summary>
        /// 在指定路径位置上创建一个OFD文件
        /// </summary>
        /// <param name="outStream">OFD输出流，由调用者负责关闭。</param>
        public OFDDoc(Stream outStream) : this()
        {
            this.outStream = outStream ?? throw new ArgumentNullException(nameof(outStream));
        }

        /// <summary>
        /// 修改一个OFD文档
        /// </summary>
        public OFDDoc(OFDReader reader, string outPath) : this()
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            if (string.IsNullOrEmpty(outPath))
                throw new ArgumentException("OFD文件存储路径为空");
            
            this.outPath = outPath;
            try
            {
                ContainerInit(reader);
            }
            catch (Exception e)
            {
                throw new DocReadException("OFD文件解析异常", e);
            }
        }

        /// <summary>
        /// 修改一个OFD文档
        /// </summary>
        public OFDDoc(OFDReader reader, Stream outStream) : this()
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.outStream = outStream ?? throw new ArgumentNullException(nameof(outStream));
            try
            {
                ContainerInit(reader);
            }
            catch (Exception e)
            {
                throw new DocReadException("OFD文件解析异常", e);
            }
        }

        private OFDDoc()
        {
            ContainerInit();
        }

        /// <summary>
        /// 设置默认页面布局
        /// </summary>
        /// <param name="pageLayout">页面布局对象</param>
        /// <returns>当前文档对象</returns>
        public OFDDoc SetDefaultPageLayout(PageLayout pageLayout)
        {
            if (pageLayout != null)
            {
                this.pageLayout = pageLayout;
                cdata?.SetPageArea(pageLayout.GetPageArea());
            }
            return this;
        }

        private void ContainerInit()
        {
            var docInfo = new OfdrwNet.Core.BasicStructure.Ofd.DocInfo.CtDocInfo()
                .SetDocID(Guid.NewGuid())
                .SetCreationDate(DateTime.Now.Date)
                .SetCreator("OFD R&W")
                .SetCreatorVersion("1.0.0");

            var docBody = new DocBody()
                .SetDocInfo(docInfo)
                .SetDocRoot(new StLoc("Doc_0/Document.xml"));

            var ofd = new OFD().AddDocBody(docBody);

            ofdDocument = new Document();
            cdata = new CtCommonData();
            SetDefaultPageLayout(pageLayout);
            ofdDocument.SetCommonData(cdata).SetPages(new Pages());

            ofdDir = OFDDir.NewOFD().SetOfd(ofd);
            var docDir = ofdDir.NewDoc();
            operateDocDir = docDir;
            docDir.SetDocument(ofdDocument);
            prm = new ResManager(ofdDir, docDir, () => Interlocked.Increment(ref maxUnitID));
        }

        private void ContainerInit(OFDReader reader)
        {
            ofdDir = reader.GetOFDDir();
            if (ofdDir == null) 
                throw new InvalidOperationException("无法获取OFD目录结构");
                
            var ofd = ofdDir.GetOfd();
            if (ofd == null)
                throw new InvalidOperationException("无法获取OFD对象");
                
            var docBody = ofd.GetDocBody();
            var docInfo = docBody?.GetDocInfo();
            docInfo?.SetModDate(DateTime.Now.Date);
            
            var rl = reader.GetResourceLocator();
            var docRoot = docBody?.GetDocRoot();
            if (docRoot != null)
            {
                ofdDocument = rl.Get(docRoot, el => new Document(el));
            }
            
            cdata = ofdDocument?.GetCommonData();
            var maxUnitIDSt = cdata?.GetMaxUnitID();
            maxUnitID = maxUnitIDSt?.GetId() ?? 0;
            operateDocDir = ofdDir.ObtainDocDefault();
            prm = new ResManager(reader);
        }

        /// <summary>
        /// 向文档添加流式布局元素
        /// </summary>
        /// <param name="item">要添加的Div元素</param>
        /// <returns>当前文档对象</returns>
        public OFDDoc Add(Div item)
        {
            if (streamQueue.Contains(item))
                throw new ArgumentException("元素已经存在，请勿重复放入");
            streamQueue.Add(item);
            return this;
        }

        /// <summary>
        /// 添加虚拟页面到文档
        /// </summary>
        /// <param name="virtualPage">虚拟页面对象</param>
        /// <returns>当前文档对象</returns>
        public OFDDoc AddVPage(VirtualPage virtualPage)
        {
            vPageList.Add(virtualPage);
            return this;
        }

        /// <summary>
        /// 添加流式内容收集器
        /// </summary>
        /// <param name="streamCollect">流式内容收集器</param>
        /// <returns>当前文档对象</returns>
        public OFDDoc AddStreamCollect(StreamCollect streamCollect)
        {
            sPageList.Add(streamCollect);
            return this;
        }

        /// <summary>
        /// 向指定页面添加注释
        /// </summary>
        /// <param name="pageNum">页面编号</param>
        /// <param name="annotation">注释对象</param>
        /// <returns>当前文档对象</returns>
        public OFDDoc AddAnnotation(int pageNum, AnnotationBase annotation)
        {
            if (annotation == null) return this;
            if (reader == null)
                throw new InvalidOperationException("仅在修改模式下允许获取追加注释对象，请使用reader构造");
            
            annotationRender ??= new AnnotationRender(reader.GetOFDDir().ObtainDocDefault(), prm!, () => Interlocked.Increment(ref maxUnitID));
            var pageInfo = reader.GetPageInfo(pageNum);
            annotationRender.Render(pageInfo, annotation);
            return this;
        }

        /// <summary>
        /// 获取页面布局配置的副本
        /// </summary>
        /// <returns>页面布局配置副本</returns>
        public PageLayout GetPageLayout() => pageLayout.Clone();

        /// <summary>
        /// 获取OFD目录结构
        /// </summary>
        /// <returns>OFD目录结构，可能为null</returns>
        public OFDDir? GetOfdDir() => ofdDir;
        
        /// <summary>
        /// 获取OFD文档对象
        /// </summary>
        /// <returns>OFD文档对象，可能为null</returns>
        public Document? GetOfdDocument() => ofdDocument;
        
        /// <summary>
        /// 获取OFD阅读器
        /// </summary>
        /// <returns>OFD阅读器，可能为null</returns>
        public OFDReader? GetReader() => reader;
        
        /// <summary>
        /// 获取资源管理器
        /// </summary>
        /// <returns>资源管理器，可能为null</returns>
        public ResManager? GetResManager() => prm;
        
        /// <summary>
        /// 获取页面处理器
        /// </summary>
        /// <returns>页面处理器，可能为null</returns>
        public VPageHandler? GetOnPage() => onPageHandler;

        /// <summary>
        /// 设置渲染完成处理器
        /// </summary>
        /// <param name="handler">渲染完成处理器</param>
        /// <returns>当前文档对象</returns>
        public OFDDoc OnRenderFinish(RenderFinishHandler? handler)
        {
            renderingEndHandler = handler;
            return this;
        }

        /// <summary>
        /// 设置页面处理器
        /// </summary>
        /// <param name="handler">页面处理器</param>
        /// <returns>当前文档对象</returns>
        public OFDDoc OnPage(VPageHandler handler)
        {
            this.onPageHandler = handler;
            return this;
        }

        /// <summary>
        /// 关闭文档并释放相关资源
        /// </summary>
        public void Close()
        {
            lock (this)
            {
                if (closed) return;
                closed = true;
            }

            try
            {
                if (streamQueue.Count > 0)
                {
                    var sgmEngine = new SegmentationEngine(pageLayout);
                    var analyzer = new StreamingLayoutAnalyzer(pageLayout);
                    var sgmQueue = sgmEngine.Process(streamQueue);
                    // 使用修复的 StreamingLayoutAnalyzer.Analyze(List<Segment>) 重载
                    var virtualPageList = analyzer.Analyze(sgmQueue);
                    vPageList.AddRange(virtualPageList);
                }
                
                if (sPageList.Count > 0)
                {
                    foreach (var sCollect in sPageList)
                    {
                        var pageList = sCollect.Analyze(pageLayout);
                        vPageList.AddRange(pageList);
                    }
                }

                if (vPageList.Count > 0)
                {
                    var docDefault = ofdDir!.ObtainDocDefault();
                    var document = docDefault.GetDocument();
                    var parseEngine = new OfdrwNet.Layout.Engine.VPageParseEngine(pageLayout, document, prm!, maxUnitID);
                    // TODO: 修复Handler类型冲突问题
                    // if (onPageHandler != null)
                    // {
                    //     var adapter = new OfdrwNet.Layout.Handler.VPageHandlerAdapter(onPageHandler);
                    //     parseEngine.SetBeforePageParseHandler((OfdrwNet.Layout.Engine.IVPageHandler)adapter);
                    // }
                    // TODO: 修复VirtualPage类型冲突问题
                    // parseEngine.Process(vPageList);
                }

                if (vPageList.Count == 0 && annotationRender == null && reader == null)
                {
                    throw new InvalidOperationException("OFD文档中没有页面，无法生成OFD文档");
                }

                renderingEndHandler?.Invoke(maxUnitID, ofdDir!, operateDocDir!.GetIndex());
                cdata!.SetMaxUnitID((long)maxUnitID);
                
                if (!string.IsNullOrEmpty(outPath))
                {
                    ofdDir!.Jar(Path.GetFullPath(outPath));
                }
                else if (outStream != null)
                {
                    ofdDir!.Jar(outStream);
                }
                else
                {
                    throw new ArgumentException("OFD文档输出地址错误或没有设置输出流");
                }
            }
            finally
            {
                reader?.Close();
                ofdDir?.Clean();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!closed)
            {
                try { Close(); } catch { /* ignore */ }
            }
        }
    }
}
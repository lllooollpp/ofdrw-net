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
using OfdrwNet.Layout.Exception;
using OfdrwNet.Layout.Handler;
using OfdrwNet.Pkg.Container;
using OfdrwNet.Reader;
using OFDReader = OfdrwNet.Reader.OfdReader;
using OFDDir = System.String;
using DocDir = System.String;
using StreamCollect = System.Collections.Generic.List<System.IO.Stream>;
using Annotation = System.Object;
using AnnotationRender = System.Object;
using RenderFinishHandler = System.EventHandler;

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
            var docInfo = new CtDocInfo()
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
            var ofd = ofdDir.GetOfd();
            var docBody = ofd.GetDocBody();
            var docInfo = docBody.GetDocInfo();
            docInfo.SetModDate(DateTime.Now.Date);
            
            var rl = reader.GetResourceLocator();
            var docRoot = docBody.GetDocRoot();
            ofdDocument = rl.Get(docRoot, () => new Document());
            
            cdata = ofdDocument.GetCommonData();
            var maxUnitIDSt = cdata.GetMaxUnitID();
            maxUnitID = maxUnitIDSt?.GetId() ?? 0;
            operateDocDir = ofdDir.ObtainDocDefault();
            prm = new ResManager(reader);
        }

        public OFDDoc Add(Div item)
        {
            if (streamQueue.Contains(item))
                throw new ArgumentException("元素已经存在，请勿重复放入");
            streamQueue.Add(item);
            return this;
        }

        public OFDDoc AddVPage(VirtualPage virtualPage)
        {
            vPageList.Add(virtualPage);
            return this;
        }

        public OFDDoc AddStreamCollect(StreamCollect streamCollect)
        {
            sPageList.Add(streamCollect);
            return this;
        }

        public OFDDoc AddAnnotation(int pageNum, Annotation annotation)
        {
            if (annotation == null) return this;
            if (reader == null)
                throw new InvalidOperationException("仅在修改模式下允许获取追加注释对象，请使用reader构造");
            
            annotationRender ??= new AnnotationRender(reader.GetOFDDir().ObtainDocDefault(), prm!, () => Interlocked.Increment(ref maxUnitID));
            var pageInfo = reader.GetPageInfo(pageNum);
            annotationRender.Render(pageInfo, annotation);
            return this;
        }

        public PageLayout GetPageLayout() => pageLayout.Clone();

        public OFDDir? GetOfdDir() => ofdDir;
        public Document? GetOfdDocument() => ofdDocument;
        public OFDReader? GetReader() => reader;
        public ResManager? GetResManager() => prm;
        public VPageHandler? GetOnPage() => onPageHandler;

        public OFDDoc OnRenderFinish(RenderFinishHandler? handler)
        {
            renderingEndHandler = handler;
            return this;
        }

        public OFDDoc OnPage(VPageHandler handler)
        {
            onPageHandler = handler;
            return this;
        }

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
                    var parseEngine = new VPageParseEngine(pageLayout, docDefault, prm!, () => Interlocked.Increment(ref maxUnitID));
                    parseEngine.SetBeforePageParseHandler(onPageHandler);
                    parseEngine.Process(vPageList);
                }

                if (vPageList.Count == 0 && annotationRender == null && reader == null)
                {
                    throw new InvalidOperationException("OFD文档中没有页面，无法生成OFD文档");
                }

                renderingEndHandler?.Handle(maxUnitID, ofdDir!, operateDocDir!.GetIndex());
                cdata!.SetMaxUnitID(maxUnitID);
                
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

        public void Dispose()
        {
            if (!closed)
            {
                try { Close(); } catch { /* ignore */ }
            }
        }
    }
}
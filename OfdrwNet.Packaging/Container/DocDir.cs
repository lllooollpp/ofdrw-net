using System;
using System.IO;
using System.Xml.Linq;
using OfdrwNet.Core.BasicStructure.Doc;
using OfdrwNet.Core.BasicStructure.Res;
using OfdrwNet.Core.Annotation;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core;

namespace OfdrwNet.Packaging.Container;

/// <summary>
/// 文档容器薄包装，复用 VirtualContainer
/// 提供常用 API 给 ResManager/OFDDoc 使用
/// </summary>
public class DocDir
{
    private readonly VirtualContainer _inner;
    private readonly int _index;

    public const string DocContainerPrefix = "Doc_";
    public const string DocumentFileName = "Document.xml";
    public const string PublicResFileName = "PublicRes.xml";
    public const string DocumentResFileName = "DocumentRes.xml";
    public const string AnnotationsFileName = "Annotations.xml";
    public const string Attachments = "Attachments.xml";
    // 常用目录名称
    public const string PagesDir = "Pages";
    public const string ResDir = "Res";

    public DocDir(VirtualContainer inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        // try parse index
        var name = _inner.GetContainerName();
        var idx = 0;
        if (!string.IsNullOrEmpty(name) && name.StartsWith(DocContainerPrefix))
        {
            var s = name.Replace(DocContainerPrefix, string.Empty);
            int.TryParse(s, out idx);
        }
        _index = idx;
    }

    public int GetIndex() => _index;

    public Document GetDocument() => new Document(_inner.GetObj(DocumentFileName));
    public DocDir SetDocument(Document document)
    {
        _inner.PutObj(DocumentFileName, document);
        return this;
    }

    public Res GetPublicRes() => new Res(_inner.GetObj(PublicResFileName));
    public DocDir SetPublicRes(Res res)
    {
        _inner.PutObj(PublicResFileName, res);
        return this;
    }

    public Res GetDocumentRes() => new Res(_inner.GetObj(DocumentResFileName));
    public DocDir SetDocumentRes(Res res)
    {
        _inner.PutObj(DocumentResFileName, res);
        return this;
    }

    public Annotations GetAnnotations() => new Annotations(_inner.GetObj(AnnotationsFileName));
    public DocDir SetAnnotations(Annotations annotations)
    {
        _inner.PutObj(AnnotationsFileName, annotations);
        return this;
    }

    public VirtualContainer GetRes() => _inner.GetContainer(ResDir, () => new VirtualContainer(_inner.GetSysAbsPath(), ResDir));
    public VirtualContainer ObtainRes() => _inner.ObtainContainer(ResDir, () => new VirtualContainer(_inner.GetSysAbsPath(), ResDir));

    public string AddResourceWithPath(string resource)
    {
        // copy file into Res dir
        var filename = Path.GetFileName(resource);
        var resDir = Path.Combine(_inner.GetSysAbsPath(), ResDir);
        Directory.CreateDirectory(resDir);
        var dest = Path.Combine(resDir, filename);
        File.Copy(resource, dest, true);
        return dest;
    }

    public StLoc GetAbsLoc() => _inner.GetAbsLoc();

    public bool Exist(string name)
    {
        try
        {
            _inner.GetObj(name);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }

    public VirtualContainer ObtainDocDefault() => _inner;

    public VirtualContainer GetContainer(string name, Func<VirtualContainer> constructor) => _inner.GetContainer(name, constructor);

    public VirtualContainer ObtainContainer(string name, Func<VirtualContainer> constructor) => _inner.ObtainContainer(name, constructor);

    public void PutObj(string fileName, OfdElement obj) => _inner.PutObj(fileName, obj);

    // wrapper for other VirtualContainer methods used in code
    public string GetFile(string fileName) => _inner.GetFile(fileName);

    public VirtualContainer ObtainPages() => _inner.ObtainContainer(PagesDir, () => new VirtualContainer(_inner.GetSysAbsPath(), PagesDir));

    public void Flush() => _inner.Flush();

    public string GetContainerPath() => _inner.GetSysAbsPath();
}

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OfdrwNet.Core.BasicStructure.Ofd;

namespace OfdrwNet.Packaging.Container;

/// <summary>
/// OFD 文档容器薄包装，复用现有 VirtualContainer 实现
/// 提供与 Java OFDDir 兼容的常用 API
/// </summary>
public class OFDDir
{
    private readonly VirtualContainer _inner;
    private int _maxDocIndex = 0;

    public const string OFDFileName = "OFD.xml";

    public OFDDir(VirtualContainer inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        Init();
    }

    public OFDDir(string fullDir)
    {
        _inner = new VirtualContainer(fullDir);
        Init();
    }

    private void Init()
    {
        try
        {
            var dir = new DirectoryInfo(_inner.GetSysAbsPath());
            if (!dir.Exists) return;
            foreach (var d in dir.GetDirectories())
            {
                if (d.Name.StartsWith(DocDir.DocContainerPrefix))
                {
                    var numb = d.Name.Replace(DocDir.DocContainerPrefix, string.Empty);
                    if (int.TryParse(numb, out var n) && _maxDocIndex <= n)
                    {
                        _maxDocIndex = n + 1;
                    }
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    public static OFDDir NewOFD()
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"ofd-tmp-{Guid.NewGuid()}");
        Directory.CreateDirectory(tmp);
        return new OFDDir(new VirtualContainer(tmp));
    }

    public OFDDir SetOfd(OFD ofd)
    {
        _inner.PutObj(OFDFileName, ofd);
        return this;
    }

    public OFD GetOfd()
    {
        var el = _inner.GetObj(OFDFileName);
        return new OFD(el);
    }

    public DocDir NewDoc()
    {
        var name = DocDir.DocContainerPrefix + _maxDocIndex;
        _maxDocIndex++;
        var full = Path.Combine(_inner.GetSysAbsPath(), name);
        Directory.CreateDirectory(full);
        return new DocDir(new VirtualContainer(full));
    }

    public DocDir ObtainDoc(int index)
    {
        var name = DocDir.DocContainerPrefix + index;
        if (index >= _maxDocIndex) _maxDocIndex = index + 1;
        var full = Path.Combine(_inner.GetSysAbsPath(), name);
        Directory.CreateDirectory(full);
        return new DocDir(new VirtualContainer(full));
    }

    public DocDir ObtainDocDefault()
    {
        try
        {
            var ofd = GetOfd();
            var bodies = ofd.GetDocBodies();
            if (bodies != null && bodies.Count > 0)
            {
                var docRoot = bodies[bodies.Count - 1].GetDocRoot();
                if (docRoot != null)
                {
                    var docRootStr = docRoot.ToString();
                    var parts = docRootStr.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    var parent = parts.FirstOrDefault(p => p.StartsWith(DocDir.DocContainerPrefix));
                    if (!string.IsNullOrEmpty(parent))
                    {
                        var full = Path.Combine(_inner.GetSysAbsPath(), parent);
                        return new DocDir(new VirtualContainer(full));
                    }
                }
            }
        }
        catch
        {
            // ignore and fallback
        }

        return ObtainDoc(0);
    }

    public string GetSysAbsPath() => _inner.GetSysAbsPath();

    public void Jar(string outputPath)
    {
        if (string.IsNullOrEmpty(outputPath)) throw new ArgumentException(nameof(outputPath));
        var tmpFile = Path.GetTempFileName();
        try
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
            System.IO.Compression.ZipFile.CreateFromDirectory(_inner.GetSysAbsPath(), tmpFile, System.IO.Compression.CompressionLevel.Optimal, false);
            File.Copy(tmpFile, outputPath, true);
        }
        finally
        {
            try { File.Delete(tmpFile); } catch { }
        }
    }

    public void Jar(Stream outputStream)
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
            System.IO.Compression.ZipFile.CreateFromDirectory(_inner.GetSysAbsPath(), tmpFile, System.IO.Compression.CompressionLevel.Optimal, false);
            using var fs = File.OpenRead(tmpFile);
            fs.CopyTo(outputStream);
        }
        finally
        {
            try { File.Delete(tmpFile); } catch { }
        }
    }

    public void Clean() => _inner.Clean();

    public DocDir GetDocByIndex(int index)
    {
        var name = DocDir.DocContainerPrefix + index;
        var full = Path.Combine(_inner.GetSysAbsPath(), name);
        if (!Directory.Exists(full)) throw new FileNotFoundException("指定索引的文档容器不存在");
        return new DocDir(new VirtualContainer(full));
    }

    public DocDir GetDocDir(string name)
    {
        var full = Path.Combine(_inner.GetSysAbsPath(), name);
        if (!Directory.Exists(full)) throw new FileNotFoundException("容器不存在");
        return new DocDir(new VirtualContainer(full));
    }

    public VirtualContainer GetInner() => _inner;
}

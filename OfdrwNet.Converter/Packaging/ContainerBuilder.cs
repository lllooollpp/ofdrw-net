using System.IO.Compression;
using System.Xml.Linq;
using OfdrwNet.Core.Diagnostics;
using OfdrwNet.Core.Conversion;

namespace OfdrwNet.Converter.Packaging;

/// <summary>
/// OFD container builder service
/// Constructs OFD ZIP container with proper structure
/// </summary>
public class ContainerBuilder : IDisposable
{
    private readonly string _outputPath;
    private readonly IStructuredLogger? _logger;
    private ZipArchive? _archive;
    private FileStream? _fileStream;
    private int _pageCount = 0;
    private readonly List<string> _resourcePaths = new();

    /// <summary>
    /// Initialize container builder
    /// </summary>
    /// <param name="outputPath">Output OFD file path</param>
    /// <param name="logger">Optional structured logger</param>
    public ContainerBuilder(string outputPath, IStructuredLogger? logger = null)
    {
        _outputPath = outputPath;
        _logger = logger;
    }

    /// <summary>
    /// Begin building container
    /// </summary>
    public void Begin()
    {
        _logger?.LogInfo(LogEvents.BuildContainer, new
        {
            action = "begin",
            output = _outputPath
        });

        // Create output directory if needed
        var dir = Path.GetDirectoryName(_outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Create ZIP archive
        _fileStream = File.Create(_outputPath);
        _archive = new ZipArchive(_fileStream, ZipArchiveMode.Create, leaveOpen: false);

        // Create basic OFD structure
        AddOfdXml();
    }

    /// <summary>
    /// Add OFD.xml root descriptor
    /// </summary>
    private void AddOfdXml()
    {
        var ofd = new XElement("ofd:OFD",
            new XAttribute(XNamespace.Xmlns + "ofd", "http://www.ofdspec.org/2016"),
            new XAttribute("Version", "1.0"),
            new XElement("ofd:DocBody",
                new XElement("ofd:DocInfo",
                    new XElement("ofd:DocID", Guid.NewGuid().ToString("N")),
                    new XElement("ofd:CreationDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                    new XElement("ofd:Creator", "OfdrwNet.Converter")
                ),
                new XElement("ofd:DocRoot", "Doc_0/Document.xml")
            )
        );

        AddXmlEntry("OFD.xml", ofd);

        _logger?.LogInfo(LogEvents.BuildContainer, new
        {
            action = "add_ofd_xml"
        });
    }

    /// <summary>
    /// Add Document.xml
    /// </summary>
    public void AddDocument(int pageCount)
    {
        _pageCount = pageCount;

        var doc = new XElement("ofd:Document",
            new XAttribute(XNamespace.Xmlns + "ofd", "http://www.ofdspec.org/2016"),
            new XElement("ofd:CommonData",
                new XElement("ofd:MaxUnitID", "1000"),
                new XElement("ofd:PageArea",
                    new XElement("ofd:PhysicalBox", "0 0 210 297") // A4 default
                )
            ),
            new XElement("ofd:Pages")
        );

        // Add page references
        var pagesElement = doc.Element(XName.Get("Pages", "http://www.ofdspec.org/2016"));
        for (int i = 0; i < pageCount; i++)
        {
            pagesElement?.Add(new XElement("ofd:Page",
                new XAttribute("ID", i + 1),
                new XAttribute("BaseLoc", $"Pages/Page_{i}/Content.xml")
            ));
        }

        AddXmlEntry("Doc_0/Document.xml", doc);

        _logger?.LogInfo(LogEvents.BuildContainer, new
        {
            action = "add_document",
            pageCount
        });
    }

    /// <summary>
    /// Add page content
    /// </summary>
    /// <param name="pageIndex">Page index</param>
    /// <param name="content">Page XML content</param>
    public void AddPage(int pageIndex, XElement content)
    {
        var pagePath = $"Doc_0/Pages/Page_{pageIndex}/Content.xml";
        AddXmlEntry(pagePath, content);

        _logger?.LogInfo(LogEvents.BuildContainer, new
        {
            action = "add_page",
            page = pageIndex,
            path = pagePath
        });
    }

    /// <summary>
    /// Add resource file (font, image, etc.)
    /// </summary>
    /// <param name="resourcePath">Path within OFD structure</param>
    /// <param name="data">Resource binary data</param>
    public void AddResource(string resourcePath, byte[] data)
    {
        if (_archive == null)
            throw new InvalidOperationException("Container not initialized. Call Begin() first.");

        var entry = _archive.CreateEntry(resourcePath, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(data, 0, data.Length);

        _resourcePaths.Add(resourcePath);

        _logger?.LogInfo(LogEvents.ResourceEmbedded, new
        {
            action = "add_resource",
            path = resourcePath,
            size = data.Length
        });
    }

    /// <summary>
    /// Add resource from file
    /// </summary>
    /// <param name="resourcePath">Path within OFD structure</param>
    /// <param name="sourceFile">Source file path</param>
    public void AddResourceFromFile(string resourcePath, string sourceFile)
    {
        var data = File.ReadAllBytes(sourceFile);
        AddResource(resourcePath, data);
    }

    /// <summary>
    /// Add PublicRes.xml
    /// </summary>
    public void AddPublicResources(List<string> fontPaths, List<string> imagePaths)
    {
        var publicRes = new XElement("ofd:Res",
            new XAttribute(XNamespace.Xmlns + "ofd", "http://www.ofdspec.org/2016"),
            new XElement("ofd:Fonts"),
            new XElement("ofd:MultiMedias"),
            new XElement("ofd:ColorSpaces")
        );

        // Add font references
        var fontsElement = publicRes.Element(XName.Get("Fonts", "http://www.ofdspec.org/2016"));
        int fontId = 1;
        foreach (var fontPath in fontPaths)
        {
            fontsElement?.Add(new XElement("ofd:Font",
                new XAttribute("ID", fontId++),
                new XAttribute("FontName", Path.GetFileNameWithoutExtension(fontPath)),
                new XAttribute("FontFile", fontPath)
            ));
        }

        // Add image references (simplified)
        var mediaElement = publicRes.Element(XName.Get("MultiMedias", "http://www.ofdspec.org/2016"));
        int mediaId = 1;
        foreach (var imagePath in imagePaths)
        {
            mediaElement?.Add(new XElement("ofd:MultiMedia",
                new XAttribute("ID", mediaId++),
                new XAttribute("Type", "Image"),
                new XAttribute("MediaFile", imagePath)
            ));
        }

        AddXmlEntry("Doc_0/PublicRes.xml", publicRes);

        _logger?.LogInfo(LogEvents.BuildContainer, new
        {
            action = "add_public_resources",
            fonts = fontPaths.Count,
            images = imagePaths.Count
        });
    }

    /// <summary>
    /// Complete container and close archive
    /// </summary>
    public void Complete()
    {
        if (_archive == null)
            return;

        _archive.Dispose();
        _archive = null;

        _fileStream?.Dispose();
        _fileStream = null;

        _logger?.LogInfo(LogEvents.BuildContainer, new
        {
            action = "complete",
            output = _outputPath,
            pageCount = _pageCount,
            resourceCount = _resourcePaths.Count,
            fileSize = new FileInfo(_outputPath).Length
        });
    }

    /// <summary>
    /// Add XML entry to archive
    /// </summary>
    private void AddXmlEntry(string path, XElement element)
    {
        if (_archive == null)
            throw new InvalidOperationException("Container not initialized. Call Begin() first.");

        var entry = _archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            element
        );
        doc.Save(stream);
    }

    public void Dispose()
    {
        _archive?.Dispose();
        _fileStream?.Dispose();
    }
}

/// <summary>
/// Container building context
/// </summary>
public class ContainerBuildContext
{
    public string JobId { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public List<string> FontResources { get; set; } = new();
    public List<string> ImageResources { get; set; } = new();
    public List<string> ColorProfiles { get; set; } = new();
    public Dictionary<int, XElement> PageContents { get; set; } = new();
}

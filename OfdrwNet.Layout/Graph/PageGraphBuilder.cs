using System.Xml.Linq;
using OfdrwNet.Core.Conversion;
using OfdrwNet.Core.Pages;

namespace OfdrwNet.Layout.Graph;

/// <summary>
/// Page graph builder for CTM/Clip hierarchy
/// </summary>
public class PageGraphBuilder
{
    /// <summary>
    /// Build transformation graph from page
    /// </summary>
    public PageGraph BuildGraph(PageContext context, XElement pageXml)
    {
        var graph = new PageGraph
        {
            PageIndex = context.PageNumber,
            RootNode = new GraphNode
            {
                Type = "Page",
                Transform = Matrix3x2.Identity
            }
        };

        // Placeholder: actual graph construction from page XML
        // Parse Layer -> Block -> Object hierarchy
        // Extract CTM matrices and clip paths

        return graph;
    }
}

public class PageGraph
{
    public int PageIndex { get; set; }
    public GraphNode? RootNode { get; set; }
}

public class GraphNode
{
    public string Type { get; set; } = string.Empty;
    public Matrix3x2 Transform { get; set; }
    public List<GraphNode> Children { get; set; } = new();
}

public struct Matrix3x2
{
    public double M11, M12, M21, M22, M31, M32;
    public static Matrix3x2 Identity => new() { M11 = 1, M22 = 1 };
}

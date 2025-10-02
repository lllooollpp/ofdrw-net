using OfdrwNet.Layout.Graph;

namespace OfdrwNet.Layout.Analysis;

/// <summary>
/// Layout features detector (vertical text, ruby annotations)
/// </summary>
public class LayoutFeaturesDetector
{
    /// <summary>
    /// Detect layout features from page graph
    /// </summary>
    public LayoutFeatures DetectFeatures(PageGraph graph)
    {
        var features = new LayoutFeatures();

        // Placeholder: actual feature detection
        // - Scan text objects for vertical writing mode
        // - Detect ruby annotation elements
        // - Identify complex layouts

        return features;
    }
}

public class LayoutFeatures
{
    public bool HasVerticalText { get; set; }
    public bool HasRubyAnnotations { get; set; }
    public bool HasComplexLayout { get; set; }
    public List<string> DetectedFeatures { get; set; } = new();
}

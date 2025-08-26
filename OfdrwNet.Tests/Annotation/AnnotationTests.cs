using OfdrwNet.Core.Annotation;
using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Resource;
using OfdrwNet.Core.Container;
using OfdrwNet.Packaging.Container;
using FluentAssertions;
using Xunit;

namespace OfdrwNet.Tests.Annotation;

/// <summary>
/// 高亮注释单元测试
/// </summary>
public class HighlightAnnotationTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateHighlightAnnotation()
    {
        // Arrange
        var id = new StId(1);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var color = CreateTestColor();

        // Act
        var annotation = new HighlightAnnotation(id, pageId, boundary, color);

        // Assert
        annotation.Should().NotBeNull();
        annotation.Id.Should().Be(id);
        annotation.Type.Should().Be(AnnotationType.Highlight);
        annotation.PageId.Should().Be(pageId);
        annotation.Boundary.Should().Be(boundary);
        annotation.HighlightColor.Should().Be(color);
        annotation.Visible.Should().BeTrue();
        annotation.Printable.Should().BeTrue();
        annotation.Opacity.Should().Be(1.0);
    }

    [Fact]
    public void AddHighlightArea_WithValidArea_ShouldAddAreaAndUpdateBoundary()
    {
        // Arrange
        var annotation = CreateTestHighlightAnnotation();
        var area = new StBox(15, 15, 50, 10);

        // Act
        var result = annotation.AddHighlightArea(area);

        // Assert
        result.Should().BeSameAs(annotation); // 支持链式调用
        annotation.HighlightAreas.Should().Contain(area);
        annotation.HighlightAreas.Should().HaveCount(1);
    }

    [Fact]
    public void AddHighlightAreas_WithMultipleAreas_ShouldAddAllAreas()
    {
        // Arrange
        var annotation = CreateTestHighlightAnnotation();
        var areas = new List<StBox>
        {
            new StBox(10, 10, 30, 10),
            new StBox(50, 10, 40, 10),
            new StBox(10, 25, 80, 10)
        };

        // Act
        annotation.AddHighlightAreas(areas);

        // Assert
        annotation.HighlightAreas.Should().HaveCount(3);
        annotation.HighlightAreas.Should().BeEquivalentTo(areas);
    }

    [Fact]
    public void SetOpacity_WithValidValue_ShouldSetOpacity()
    {
        // Arrange
        var annotation = CreateTestHighlightAnnotation();

        // Act
        annotation.SetOpacity(0.5);

        // Assert
        annotation.Opacity.Should().Be(0.5);
    }

    [Fact]
    public void SetOpacity_WithValueAboveOne_ShouldClampToOne()
    {
        // Arrange
        var annotation = CreateTestHighlightAnnotation();

        // Act
        annotation.SetOpacity(1.5);

        // Assert
        annotation.Opacity.Should().Be(1.0);
    }

    [Fact]
    public void SetOpacity_WithNegativeValue_ShouldClampToZero()
    {
        // Arrange
        var annotation = CreateTestHighlightAnnotation();

        // Act
        annotation.SetOpacity(-0.5);

        // Assert
        annotation.Opacity.Should().Be(0.0);
    }

    [Fact]
    public void ContainsPoint_WithPointInArea_ShouldReturnTrue()
    {
        // Arrange
        var annotation = CreateTestHighlightAnnotation();
        annotation.AddHighlightArea(new StBox(10, 10, 50, 20));

        // Act
        var contains = annotation.ContainsPoint(30, 15);

        // Assert
        contains.Should().BeTrue();
    }

    [Fact]
    public void ContainsPoint_WithPointOutsideArea_ShouldReturnFalse()
    {
        // Arrange
        var annotation = CreateTestHighlightAnnotation();
        annotation.AddHighlightArea(new StBox(10, 10, 50, 20));

        // Act
        var contains = annotation.ContainsPoint(5, 5);

        // Assert
        contains.Should().BeFalse();
    }

    [Fact]
    public void GetTotalArea_WithMultipleAreas_ShouldReturnCorrectSum()
    {
        // Arrange
        var annotation = CreateTestHighlightAnnotation();
        annotation.AddHighlightArea(new StBox(0, 0, 10, 10)); // 面积 100
        annotation.AddHighlightArea(new StBox(0, 0, 20, 5));  // 面积 100

        // Act
        var totalArea = annotation.GetTotalArea();

        // Assert
        totalArea.Should().Be(200);
    }

    [Fact]
    public void ToXml_ShouldGenerateValidXml()
    {
        // Arrange
        var annotation = CreateTestHighlightAnnotation();
        annotation.Title = "Test Highlight";
        annotation.Content = "Test content";
        annotation.Creator = "Test User";
        annotation.AddHighlightArea(new StBox(10, 10, 50, 20));

        // Act
        var xml = annotation.ToXml();

        // Assert
        xml.Should().NotBeEmpty();
        xml.Should().Contain("<ofd:Annot");
        xml.Should().Contain("Type='Highlight'");
        xml.Should().Contain("Test Highlight");
        xml.Should().Contain("Test content");
        xml.Should().Contain("Test User");
        xml.Should().Contain("<ofd:HighlightAreas>");
    }

    [Fact]
    public void Clone_ShouldCreateIdenticalCopy()
    {
        // Arrange
        var original = CreateTestHighlightAnnotation();
        original.Title = "Original";
        original.Content = "Original content";
        original.AddHighlightArea(new StBox(10, 10, 50, 20));
        var newId = new StId(999);

        // Act
        var clone = original.Clone(newId);

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Id.Should().Be(newId);
        clone.PageId.Should().Be(original.PageId);
        clone.Title.Should().Be(original.Title);
        clone.Content.Should().Be(original.Content);
        clone.HighlightAreas.Should().BeEquivalentTo(original.HighlightAreas);
    }

    private static HighlightAnnotation CreateTestHighlightAnnotation()
    {
        var id = new StId(1);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var color = CreateTestColor();
        return new HighlightAnnotation(id, pageId, boundary, color);
    }

    private static OfdrwNet.Core.Resource.Color CreateTestColor()
    {
        var colorSpace = new ColorSpace(new StId(0), ColorSpaceType.RGB);
        return new OfdrwNet.Core.Resource.Color(new StId(1), colorSpace)
        {
            Components = new[] { 1.0, 1.0, 0.0 } // 黄色
        };
    }
}

/// <summary>
/// 链接注释单元测试
/// </summary>
public class LinkAnnotationTests
{
    [Fact]
    public void CreateUrlLink_WithValidUrl_ShouldCreateLinkAnnotation()
    {
        // Arrange
        var id = new StId(1);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var url = "https://www.example.com";

        // Act
        var annotation = LinkAnnotation.CreateUrlLink(id, pageId, boundary, url);

        // Assert
        annotation.Should().NotBeNull();
        annotation.LinkType.Should().Be(LinkType.Url);
        annotation.Target.Should().Be(url);
        annotation.Title.Should().Be("网页链接");
    }

    [Fact]
    public void CreateUrlLink_WithInvalidUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new StId(1);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var invalidUrl = "not-a-url";

        // Act & Assert
        var act = () => LinkAnnotation.CreateUrlLink(id, pageId, boundary, invalidUrl);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*URL格式不正确*");
    }

    [Fact]
    public void CreatePageLink_WithValidParameters_ShouldCreatePageLinkAnnotation()
    {
        // Arrange
        var id = new StId(1);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var targetPageId = new StId(5);

        // Act
        var annotation = LinkAnnotation.CreatePageLink(id, pageId, boundary, targetPageId);

        // Assert
        annotation.Should().NotBeNull();
        annotation.LinkType.Should().Be(LinkType.Page);
        annotation.TargetPageId.Should().Be(targetPageId);
        annotation.Title.Should().Be("页面跳转");
    }

    [Fact]
    public void CreateEmailLink_WithValidEmail_ShouldCreateEmailLinkAnnotation()
    {
        // Arrange
        var id = new StId(1);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var email = "test@example.com";
        var subject = "Test Subject";

        // Act
        var annotation = LinkAnnotation.CreateEmailLink(id, pageId, boundary, email, subject);

        // Assert
        annotation.Should().NotBeNull();
        annotation.LinkType.Should().Be(LinkType.Email);
        annotation.Target.Should().Contain("mailto:test@example.com");
        annotation.Target.Should().Contain("subject=Test%20Subject");
    }

    [Fact]
    public void CreateEmailLink_WithInvalidEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new StId(1);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var invalidEmail = "not-an-email";

        // Act & Assert
        var act = () => LinkAnnotation.CreateEmailLink(id, pageId, boundary, invalidEmail);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*邮箱地址格式不正确*");
    }

    [Fact]
    public void ValidateTarget_WithValidUrl_ShouldReturnTrue()
    {
        // Arrange
        var annotation = LinkAnnotation.CreateUrlLink(
            new StId(1), new StId(1), new StBox(0, 0, 100, 20), "https://www.example.com");

        // Act
        var isValid = annotation.ValidateTarget();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void GetDisplayText_WithUrlLink_ShouldReturnUrl()
    {
        // Arrange
        var url = "https://www.example.com";
        var annotation = LinkAnnotation.CreateUrlLink(
            new StId(1), new StId(1), new StBox(0, 0, 100, 20), url);

        // Act
        var displayText = annotation.GetDisplayText();

        // Assert
        displayText.Should().Be(url);
    }

    [Fact]
    public void GetDisplayText_WithEmailLink_ShouldReturnEmailAddress()
    {
        // Arrange
        var email = "test@example.com";
        var annotation = LinkAnnotation.CreateEmailLink(
            new StId(1), new StId(1), new StBox(0, 0, 100, 20), email);

        // Act
        var displayText = annotation.GetDisplayText();

        // Assert
        displayText.Should().Be(email);
    }

    [Fact]
    public void SetBorder_ShouldSetBorderProperties()
    {
        // Arrange
        var annotation = LinkAnnotation.CreateUrlLink(
            new StId(1), new StId(1), new StBox(0, 0, 100, 20), "https://www.example.com");
        var color = CreateTestColor();

        // Act
        annotation.SetBorder(LinkBorderStyle.Solid, color, 2.0);

        // Assert
        annotation.BorderStyle.Should().Be(LinkBorderStyle.Solid);
        annotation.BorderColor.Should().Be(color);
        annotation.BorderWidth.Should().Be(2.0);
    }

    private static OfdrwNet.Core.Resource.Color CreateTestColor()
    {
        var colorSpace = new ColorSpace(new StId(0), ColorSpaceType.RGB);
        return new OfdrwNet.Core.Resource.Color(new StId(1), colorSpace)
        {
            Components = new[] { 0.0, 0.0, 1.0 } // 蓝色
        };
    }
}

/// <summary>
/// 注释管理器单元测试
/// </summary>
public class AnnotationManagerTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly IContainer _container;

    public AnnotationManagerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OfdrwNet_AnnotationTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _container = new VirtualContainer(_tempDirectory);
    }

    [Fact]
    public void AddHighlightAnnotation_ShouldAddAnnotationSuccessfully()
    {
        // Arrange
        using var manager = new AnnotationManager(_container);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var color = CreateTestColor();

        // Act
        var annotation = manager.AddHighlightAnnotation(pageId, boundary, color);

        // Assert
        annotation.Should().NotBeNull();
        manager.GetAnnotationCount().Should().Be(1);
        manager.GetPageAnnotationCount(pageId).Should().Be(1);
    }

    [Fact]
    public void GetPageAnnotations_WithAnnotationsOnPage_ShouldReturnCorrectAnnotations()
    {
        // Arrange
        using var manager = new AnnotationManager(_container);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var color = CreateTestColor();

        manager.AddHighlightAnnotation(pageId, boundary, color);
        manager.AddUrlLinkAnnotation(pageId, boundary, "https://www.example.com");

        // Act
        var pageAnnotations = manager.GetPageAnnotations(pageId);

        // Assert
        pageAnnotations.Should().HaveCount(2);
        pageAnnotations.Should().Contain(a => a.Type == AnnotationType.Highlight);
        pageAnnotations.Should().Contain(a => a.Type == AnnotationType.Link);
    }

    [Fact]
    public void RemoveAnnotation_WithValidId_ShouldRemoveAnnotationSuccessfully()
    {
        // Arrange
        using var manager = new AnnotationManager(_container);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var color = CreateTestColor();
        var annotation = manager.AddHighlightAnnotation(pageId, boundary, color);

        // Act
        var removed = manager.RemoveAnnotation(annotation.Id);

        // Assert
        removed.Should().BeTrue();
        manager.GetAnnotationCount().Should().Be(0);
        manager.GetPageAnnotationCount(pageId).Should().Be(0);
    }

    [Fact]
    public void FindAnnotationsAtPoint_WithAnnotationAtPoint_ShouldReturnAnnotation()
    {
        // Arrange
        using var manager = new AnnotationManager(_container);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var color = CreateTestColor();
        manager.AddHighlightAnnotation(pageId, boundary, color);

        // Act
        var foundAnnotations = manager.FindAnnotationsAtPoint(pageId, 50, 15);

        // Assert
        foundAnnotations.Should().HaveCount(1);
        foundAnnotations.First().Type.Should().Be(AnnotationType.Highlight);
    }

    [Fact]
    public void ValidateAllAnnotations_WithValidAnnotations_ShouldReturnAllValid()
    {
        // Arrange
        using var manager = new AnnotationManager(_container);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var color = CreateTestColor();
        
        var highlight = manager.AddHighlightAnnotation(pageId, boundary, color);
        var link = manager.AddUrlLinkAnnotation(pageId, boundary, "https://www.example.com");

        // Act
        var validationResults = manager.ValidateAllAnnotations();

        // Assert
        validationResults.Should().HaveCount(2);
        validationResults[highlight.Id.ToString()].Should().BeTrue();
        validationResults[link.Id.ToString()].Should().BeTrue();
    }

    [Fact]
    public void GetStatistics_WithMultipleAnnotations_ShouldReturnCorrectStats()
    {
        // Arrange
        using var manager = new AnnotationManager(_container);
        var pageId = new StId(1);
        var boundary = new StBox(10, 10, 100, 20);
        var color = CreateTestColor();
        
        manager.AddHighlightAnnotation(pageId, boundary, color);
        manager.AddHighlightAnnotation(pageId, boundary, color);
        manager.AddUrlLinkAnnotation(pageId, boundary, "https://www.example.com");

        // Act
        var stats = manager.GetStatistics();

        // Assert
        stats.TotalCount.Should().Be(3);
        stats.PageCount.Should().Be(1);
        stats.HighlightCount.Should().Be(2);
        stats.LinkCount.Should().Be(1);
    }

    private static OfdrwNet.Core.Resource.Color CreateTestColor()
    {
        var colorSpace = new ColorSpace(new StId(0), ColorSpaceType.RGB);
        return new OfdrwNet.Core.Resource.Color(new StId(1), colorSpace)
        {
            Components = new[] { 1.0, 1.0, 0.0 } // 黄色
        };
    }

    public void Dispose()
    {
        _container?.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
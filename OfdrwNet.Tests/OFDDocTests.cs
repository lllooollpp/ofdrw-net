using Xunit;
using FluentAssertions;
using OfdrwNet;
using System.IO;

namespace OfdrwNet.Tests;

/// <summary>
/// OFDDoc 类单元测试
/// 对应 Java 版本的 OFDDoc 测试
/// </summary>
public class OFDDocTests : IDisposable
{
    private readonly string _tempDirectory;

    public OFDDocTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OfdrwNetTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Constructor_WithValidPath_ShouldCreateInstance()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");

        // Act
        using var doc = new OFDDoc(outputPath);

        // Assert
        doc.Should().NotBeNull();
        doc.PageLayout.Should().NotBeNull();
        doc.MaxUnitID.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Constructor_WithNullPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new OFDDoc((string)null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*OFD文件存储路径不能为空*");
    }

    [Fact]
    public void Constructor_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => new OFDDoc(string.Empty);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*OFD文件存储路径不能为空*");
    }

    [Fact]
    public void Constructor_WithStream_ShouldCreateInstance()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        using var doc = new OFDDoc(stream);

        // Assert
        doc.Should().NotBeNull();
        doc.PageLayout.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new OFDDoc((Stream)null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetDefaultPageLayout_WithValidLayout_ShouldSetLayout()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        using var doc = new OFDDoc(outputPath);
        var customLayout = new PageLayout
        {
            Width = 200,
            Height = 280,
            Margins = new Margins { Top = 10, Bottom = 10, Left = 15, Right = 15 }
        };

        // Act
        var result = doc.SetDefaultPageLayout(customLayout);

        // Assert
        result.Should().BeSameAs(doc); // 返回自身用于链式调用
        doc.PageLayout.Width.Should().Be(200);
        doc.PageLayout.Height.Should().Be(280);
        doc.PageLayout.Margins.Top.Should().Be(10);
    }

    [Fact]
    public void SetDefaultPageLayout_WithNullLayout_ShouldNotThrow()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        using var doc = new OFDDoc(outputPath);
        var originalLayout = doc.PageLayout;

        // Act
        var result = doc.SetDefaultPageLayout(null!);

        // Assert
        result.Should().BeSameAs(doc);
        // 布局应该保持不变
        doc.PageLayout.Width.Should().Be(originalLayout.Width);
        doc.PageLayout.Height.Should().Be(originalLayout.Height);
    }

    [Fact]
    public void Add_WithValidElement_ShouldAddToQueue()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        using var doc = new OFDDoc(outputPath);
        var paragraph = new TextParagraph("Test content");

        // Act
        var result = doc.Add(paragraph);

        // Assert
        result.Should().BeSameAs(doc); // 返回自身用于链式调用
    }

    [Fact]
    public void Add_WithDuplicateElement_ShouldThrowArgumentException()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        using var doc = new OFDDoc(outputPath);
        var paragraph = new TextParagraph("Test content");
        doc.Add(paragraph);

        // Act & Assert
        var action = () => doc.Add(paragraph);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*元素已经存在，请勿重复添加*");
    }

    [Fact]
    public void AddVirtualPage_WithValidPage_ShouldAddToList()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        using var doc = new OFDDoc(outputPath);
        var vPage = new VirtualPage();

        // Act
        var result = doc.AddVirtualPage(vPage);

        // Assert
        result.Should().BeSameAs(doc);
    }

    [Fact]
    public void GetNextID_ShouldReturnIncrementingValues()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        using var doc = new OFDDoc(outputPath);

        // Act
        var id1 = doc.GetNextID();
        var id2 = doc.GetNextID();
        var id3 = doc.GetNextID();

        // Assert
        id2.Should().BeGreaterThan(id1);
        id3.Should().BeGreaterThan(id2);
        (id3 - id1).Should().Be(2);
    }

    [Fact]
    public async Task CloseAsync_WithNoContent_ShouldCreateEmptyDocument()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "empty.ofd");
        using var doc = new OFDDoc(outputPath);

        // Act
        await doc.CloseAsync();

        // Assert
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task CloseAsync_WithContent_ShouldGenerateFile()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        using var doc = new OFDDoc(outputPath);
        doc.Add(new TextParagraph("Test content"));

        // Act
        await doc.CloseAsync();

        // Assert
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task CloseAsync_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        using var doc = new OFDDoc(outputPath);
        doc.Add(new TextParagraph("Test content"));

        // Act
        await doc.CloseAsync();
        await doc.CloseAsync(); // 第二次调用

        // Assert
        // 不应该抛出异常
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public void PageLayout_ShouldReturnClone()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        using var doc = new OFDDoc(outputPath);

        // Act
        var layout1 = doc.PageLayout;
        var layout2 = doc.PageLayout;

        // Assert
        layout1.Should().NotBeSameAs(layout2); // 应该是不同的实例
        layout1.Width.Should().Be(layout2.Width); // 但值应该相同
        layout1.Height.Should().Be(layout2.Height);
    }

    [Fact]
    public void Dispose_ShouldCloseDocumentAutomatically()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");
        var doc = new OFDDoc(outputPath);
        doc.Add(new TextParagraph("Test content"));

        // Act
        doc.Dispose();

        // Assert
        File.Exists(outputPath).Should().BeTrue();
    }

    // 清理方法
    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
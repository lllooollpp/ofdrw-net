using Xunit;
using FluentAssertions;
using OfdrwNet;
using System.IO;

namespace OfdrwNet.Tests;

/// <summary>
/// OfdrwHelper 便捷API 单元测试
/// </summary>
public class OfdrwHelperTests : IDisposable
{
    private readonly string _tempDirectory;

    public OfdrwHelperTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OfdrwHelperTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void CreateDocument_WithPath_ShouldReturnOFDDoc()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test.ofd");

        // Act
        using var doc = OfdrwHelper.CreateDocument(outputPath);

        // Assert
        doc.Should().NotBeNull();
        doc.Should().BeOfType<OFDDoc>();
    }

    [Fact]
    public void CreateDocument_WithStream_ShouldReturnOFDDoc()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        using var doc = OfdrwHelper.CreateDocument(stream);

        // Assert
        doc.Should().NotBeNull();
        doc.Should().BeOfType<OFDDoc>();
    }

    [Fact]
    public async Task CreateHelloWorldAsync_WithValidPath_ShouldCreateFile()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "hello.ofd");
        var message = "Hello, OFDRW.NET Test!";

        // Act
        await OfdrwHelper.CreateHelloWorldAsync(outputPath, message);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateHelloWorldAsync_WithDefaultMessage_ShouldUseDefaultMessage()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "hello_default.ofd");

        // Act
        await OfdrwHelper.CreateHelloWorldAsync(outputPath);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public async Task ConvertToPdfAsync_ShouldNotThrow()
    {
        // Arrange
        var ofdPath = "test.ofd";
        var pdfPath = "test.pdf";

        // Act & Assert
        var action = async () => await OfdrwHelper.ConvertToPdfAsync(ofdPath, pdfPath);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConvertToImagesAsync_ShouldNotThrow()
    {
        // Arrange
        var ofdPath = "test.ofd";
        var outputDir = _tempDirectory;

        // Act & Assert
        var action = async () => await OfdrwHelper.ConvertToImagesAsync(ofdPath, outputDir);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConvertFromPdfAsync_ShouldNotThrow()
    {
        // Arrange
        var pdfPath = "test.pdf";
        var ofdPath = "test.ofd";

        // Act & Assert
        var action = async () => await OfdrwHelper.ConvertFromPdfAsync(pdfPath, ofdPath);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetDocumentInfoAsync_ShouldReturnDocumentInfo()
    {
        // Arrange
        var filePath = "test.ofd";

        // Act
        var info = await OfdrwHelper.GetDocumentInfoAsync(filePath);

        // Assert
        info.Should().NotBeNull();
        info.Should().BeOfType<DocumentInfo>();
        info.Title.Should().NotBeNull();
        info.Author.Should().NotBeNull();
        info.PageCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPageCountAsync_ShouldReturnPositiveNumber()
    {
        // Arrange
        var filePath = "test.ofd";

        // Act
        var pageCount = await OfdrwHelper.GetPageCountAsync(filePath);

        // Assert
        pageCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task IsValidOfdDocumentAsync_WithExistingFile_ShouldReturnTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "valid.ofd");
        await File.WriteAllTextAsync(filePath, "dummy content");

        // Act
        var isValid = await OfdrwHelper.IsValidOfdDocumentAsync(filePath);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidOfdDocumentAsync_WithNonExistingFile_ShouldReturnFalse()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "nonexisting.ofd");

        // Act
        var isValid = await OfdrwHelper.IsValidOfdDocumentAsync(filePath);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("test.ofd")]
    [InlineData("document.ofd")]
    [InlineData("sample.ofd")]
    public async Task GetDocumentInfoAsync_WithVariousPaths_ShouldReturnInfo(string fileName)
    {
        // Act
        var info = await OfdrwHelper.GetDocumentInfoAsync(fileName);

        // Assert
        info.Should().NotBeNull();
        info.PageCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ConvertToImagesAsync_WithCustomParameters_ShouldNotThrow()
    {
        // Arrange
        var ofdPath = "test.ofd";
        var outputDir = _tempDirectory;
        var format = "jpg";
        var dpi = 150;

        // Act & Assert
        var action = async () => await OfdrwHelper.ConvertToImagesAsync(ofdPath, outputDir, format, dpi);
        await action.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
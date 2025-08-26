using OfdrwNet.Core.BasicType;
using OfdrwNet.Core.Resource;
using OfdrwNet.Packaging.Container;
using FluentAssertions;
using Xunit;

namespace OfdrwNet.Tests.Resource;

/// <summary>
/// 图像资源功能单元测试
/// </summary>
public class ImageResourceTests : IDisposable
{
    private readonly string _tempDirectory;

    public ImageResourceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OfdrwNet_ImageResourceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void FromBytes_WithValidData_ShouldCreateImageResource()
    {
        // Arrange
        var id = new StId(1);
        var imageData = CreateTestImageData();
        var format = ImageFormat.PNG;

        // Act
        var imageResource = ImageResource.FromBytes(id, imageData, format);

        // Assert
        imageResource.Should().NotBeNull();
        imageResource.Id.Should().Be(id);
        imageResource.Format.Should().Be(format);
        imageResource.ImageData.Should().BeEquivalentTo(imageData);
        imageResource.Width.Should().BeGreaterThan(0);
        imageResource.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FromBytes_WithNullData_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new StId(1);
        byte[]? imageData = null;
        var format = ImageFormat.PNG;

        // Act & Assert
        var act = () => ImageResource.FromBytes(id, imageData!, format);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*图像数据不能为空*");
    }

    [Fact]
    public void FromBytes_WithEmptyData_ShouldThrowArgumentException()
    {
        // Arrange
        var id = new StId(1);
        var imageData = Array.Empty<byte>();
        var format = ImageFormat.PNG;

        // Act & Assert
        var act = () => ImageResource.FromBytes(id, imageData, format);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*图像数据不能为空*");
    }

    [Fact]
    public void GetMimeType_WithDifferentFormats_ShouldReturnCorrectMimeType()
    {
        // Arrange
        var id = new StId(1);
        var imageData = CreateTestImageData();

        // Act & Assert
        var pngResource = ImageResource.FromBytes(id, imageData, ImageFormat.PNG);
        pngResource.GetMimeType().Should().Be("image/png");

        var jpegResource = ImageResource.FromBytes(id, imageData, ImageFormat.JPEG);
        jpegResource.GetMimeType().Should().Be("image/jpeg");

        var gifResource = ImageResource.FromBytes(id, imageData, ImageFormat.GIF);
        gifResource.GetMimeType().Should().Be("image/gif");
    }

    [Fact]
    public void GetFileExtension_WithDifferentFormats_ShouldReturnCorrectExtension()
    {
        // Arrange
        var id = new StId(1);
        var imageData = CreateTestImageData();

        // Act & Assert
        var pngResource = ImageResource.FromBytes(id, imageData, ImageFormat.PNG);
        pngResource.GetFileExtension().Should().Be(".png");

        var jpegResource = ImageResource.FromBytes(id, imageData, ImageFormat.JPEG);
        jpegResource.GetFileExtension().Should().Be(".jpg");

        var bmpResource = ImageResource.FromBytes(id, imageData, ImageFormat.BMP);
        bmpResource.GetFileExtension().Should().Be(".bmp");
    }

    [Fact]
    public void GenerateResourcePath_WithDefaultDirectory_ShouldCreateCorrectPath()
    {
        // Arrange
        var id = new StId(5);
        var imageData = CreateTestImageData();
        var imageResource = ImageResource.FromBytes(id, imageData, ImageFormat.PNG);

        // Act
        var resourcePath = imageResource.GenerateResourcePath();

        // Assert
        resourcePath.Should().NotBeNull();
        resourcePath.ToString().Should().Be("/Res/Image_5.png");
        imageResource.ResourcePath.Should().Be(resourcePath);
    }

    [Fact]
    public void GenerateResourcePath_WithCustomDirectory_ShouldCreateCorrectPath()
    {
        // Arrange
        var id = new StId(3);
        var imageData = CreateTestImageData();
        var imageResource = ImageResource.FromBytes(id, imageData, ImageFormat.JPEG);

        // Act
        var resourcePath = imageResource.GenerateResourcePath("Images");

        // Assert
        resourcePath.ToString().Should().Be("/Images/Image_3.jpg");
    }

    [Fact]
    public void ValidateIntegrity_WithUnchangedData_ShouldReturnTrue()
    {
        // Arrange
        var id = new StId(1);
        var imageData = CreateTestImageData();
        var imageResource = ImageResource.FromBytes(id, imageData, ImageFormat.PNG);

        // Act
        var isValid = imageResource.ValidateIntegrity();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateIntegrity_WithChangedData_ShouldReturnFalse()
    {
        // Arrange
        var id = new StId(1);
        var imageData = CreateTestImageData();
        var imageResource = ImageResource.FromBytes(id, imageData, ImageFormat.PNG);

        // Act - 修改数据
        imageResource.ImageData![0] = (byte)(imageResource.ImageData[0] + 1);
        var isValid = imageResource.ValidateIntegrity();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var id = new StId(10);
        var imageData = CreateTestImageData();
        var imageResource = ImageResource.FromBytes(id, imageData, ImageFormat.PNG);

        // Act
        var result = imageResource.ToString();

        // Assert
        result.Should().Contain("ImageResource");
        result.Should().Contain("ID=10");
        result.Should().Contain("Format=PNG");
        result.Should().Contain("Embedded=True");
    }

    [Fact]
    public void Dispose_ShouldClearImageData()
    {
        // Arrange
        var id = new StId(1);
        var imageData = CreateTestImageData();
        var imageResource = ImageResource.FromBytes(id, imageData, ImageFormat.PNG);

        // Act
        imageResource.Dispose();

        // Assert
        imageResource.ImageData.Should().BeNull();
    }

    /// <summary>
    /// 创建测试用的图像数据（简单的字节数组）
    /// </summary>
    /// <returns>模拟的图像数据</returns>
    private static byte[] CreateTestImageData()
    {
        // 创建一个简单的测试图像数据
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}

/// <summary>
/// 图像资源管理器单元测试
/// </summary>
public class ImageResourceManagerTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly VirtualContainer _container;

    public ImageResourceManagerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OfdrwNet_ImageManagerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _container = new VirtualContainer(_tempDirectory);
    }

    [Fact]
    public void AddImage_WithByteArray_ShouldAddImageSuccessfully()
    {
        // Arrange
        using var manager = new ImageResourceManager(_container);
        var imageData = CreateTestImageData();

        // Act
        var imageResource = manager.AddImage(imageData, ImageFormat.PNG, "test");

        // Assert
        imageResource.Should().NotBeNull();
        manager.GetImageCount().Should().Be(1);
        manager.ContainsImage(imageResource.Id).Should().BeTrue();
    }

    [Fact]
    public void GetImage_WithValidId_ShouldReturnImage()
    {
        // Arrange
        using var manager = new ImageResourceManager(_container);
        var imageData = CreateTestImageData();
        var addedImage = manager.AddImage(imageData, ImageFormat.PNG);

        // Act
        var retrievedImage = manager.GetImage(addedImage.Id);

        // Assert
        retrievedImage.Should().NotBeNull();
        retrievedImage!.Id.Should().Be(addedImage.Id);
    }

    [Fact]
    public void GetImage_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        using var manager = new ImageResourceManager(_container);
        var invalidId = new StId(999);

        // Act
        var retrievedImage = manager.GetImage(invalidId);

        // Assert
        retrievedImage.Should().BeNull();
    }

    [Fact]
    public void RemoveImage_WithValidId_ShouldRemoveImageSuccessfully()
    {
        // Arrange
        using var manager = new ImageResourceManager(_container);
        var imageData = CreateTestImageData();
        var addedImage = manager.AddImage(imageData, ImageFormat.PNG);

        // Act
        var removed = manager.RemoveImage(addedImage.Id);

        // Assert
        removed.Should().BeTrue();
        manager.GetImageCount().Should().Be(0);
        manager.ContainsImage(addedImage.Id).Should().BeFalse();
    }

    [Fact]
    public void GetAllImages_WithMultipleImages_ShouldReturnAllImages()
    {
        // Arrange
        using var manager = new ImageResourceManager(_container);
        var imageData = CreateTestImageData();
        
        manager.AddImage(imageData, ImageFormat.PNG);
        manager.AddImage(imageData, ImageFormat.JPEG);
        manager.AddImage(imageData, ImageFormat.GIF);

        // Act
        var allImages = manager.GetAllImages();

        // Assert
        allImages.Should().HaveCount(3);
        allImages.Select(i => i.Format).Should().Contain(new[] { ImageFormat.PNG, ImageFormat.JPEG, ImageFormat.GIF });
    }

    [Fact]
    public void GetStatistics_WithMultipleImages_ShouldReturnCorrectStats()
    {
        // Arrange
        using var manager = new ImageResourceManager(_container);
        var imageData = CreateTestImageData();
        
        manager.AddImage(imageData, ImageFormat.PNG);
        manager.AddImage(imageData, ImageFormat.PNG);
        manager.AddImage(imageData, ImageFormat.JPEG);

        // Act
        var stats = manager.GetStatistics();

        // Assert
        stats.TotalCount.Should().Be(3);
        stats.PngCount.Should().Be(2);
        stats.JpegCount.Should().Be(1);
        stats.TotalSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ValidateAllImages_WithValidImages_ShouldReturnAllValid()
    {
        // Arrange
        using var manager = new ImageResourceManager(_container);
        var imageData = CreateTestImageData();
        
        var image1 = manager.AddImage(imageData, ImageFormat.PNG);
        var image2 = manager.AddImage(imageData, ImageFormat.JPEG);

        // Act
        var validationResults = manager.ValidateAllImages();

        // Assert
        validationResults.Should().HaveCount(2);
        validationResults[image1.Id.ToString()].Should().BeTrue();
        validationResults[image2.Id.ToString()].Should().BeTrue();
    }

    [Fact]
    public void Clear_ShouldRemoveAllImages()
    {
        // Arrange
        using var manager = new ImageResourceManager(_container);
        var imageData = CreateTestImageData();
        
        manager.AddImage(imageData, ImageFormat.PNG);
        manager.AddImage(imageData, ImageFormat.JPEG);
        manager.GetImageCount().Should().Be(2);

        // Act
        manager.Clear();

        // Assert
        manager.GetImageCount().Should().Be(0);
        manager.GetAllImages().Should().BeEmpty();
    }

    [Fact]
    public async Task FlushToContainerAsync_ShouldWriteImagesToContainer()
    {
        // Arrange
        using var manager = new ImageResourceManager(_container);
        var imageData = CreateTestImageData();
        var image = manager.AddImage(imageData, ImageFormat.PNG, "test");

        // Act
        await manager.FlushToContainerAsync(() => new VirtualContainer(Path.Combine(_tempDirectory, "Res")));

        // Assert
        var resDirectory = Path.Combine(_tempDirectory, "Res");
        Directory.Exists(resDirectory).Should().BeTrue();
        
        var imageFile = Path.Combine(resDirectory, "test.png");
        File.Exists(imageFile).Should().BeTrue();
    }

    /// <summary>
    /// 创建测试用的图像数据
    /// </summary>
    /// <returns>模拟的图像数据</returns>
    private static byte[] CreateTestImageData()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
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
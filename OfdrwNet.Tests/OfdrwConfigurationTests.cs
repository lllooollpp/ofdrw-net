using Xunit;
using FluentAssertions;
using OfdrwNet;

namespace OfdrwNet.Tests;

/// <summary>
/// OfdrwConfiguration 全局配置单元测试
/// </summary>
public class OfdrwConfigurationTests
{
    [Fact]
    public void Version_ShouldHaveValue()
    {
        // Act & Assert
        OfdrwConfiguration.Version.Should().NotBeNullOrEmpty();
        OfdrwConfiguration.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void OfdStandardVersion_ShouldBeGBStandard()
    {
        // Act & Assert
        OfdrwConfiguration.OfdStandardVersion.Should().Be("GB/T 33190-2016");
    }

    [Fact]
    public void DefaultSettings_ShouldHaveReasonableValues()
    {
        // Act & Assert
        OfdrwConfiguration.DefaultFont.Should().Be("SimSun");
        OfdrwConfiguration.DefaultFontSize.Should().Be(3.5);
        OfdrwConfiguration.DefaultCreator.Should().Be("OFDRW.NET");
        OfdrwConfiguration.DefaultCreatorVersion.Should().Be(OfdrwConfiguration.Version);
        OfdrwConfiguration.DefaultPageLayout.Should().NotBeNull();
    }

    [Fact]
    public void FontMappings_ShouldContainCommonFonts()
    {
        // Act & Assert
        OfdrwConfiguration.FontMappings.Should().ContainKey("宋体");
        OfdrwConfiguration.FontMappings.Should().ContainKey("黑体");
        OfdrwConfiguration.FontMappings.Should().ContainKey("Arial");
        OfdrwConfiguration.FontMappings.Should().ContainKey("Times New Roman");
        
        OfdrwConfiguration.FontMappings["宋体"].Should().Be("SimSun");
        OfdrwConfiguration.FontMappings["黑体"].Should().Be("SimHei");
    }

    [Fact]
    public void AddFontMapping_ShouldAddOrUpdateMapping()
    {
        // Arrange
        var fontName = "TestFont";
        var systemFont = "SystemTestFont";
        var originalCount = OfdrwConfiguration.FontMappings.Count;

        // Act
        OfdrwConfiguration.AddFontMapping(fontName, systemFont);

        // Assert
        OfdrwConfiguration.FontMappings.Should().ContainKey(fontName);
        OfdrwConfiguration.FontMappings[fontName].Should().Be(systemFont);
    }

    [Fact]
    public void GetMappedFont_WithExistingFont_ShouldReturnMappedFont()
    {
        // Arrange
        var fontName = "宋体";

        // Act
        var mappedFont = OfdrwConfiguration.GetMappedFont(fontName);

        // Assert
        mappedFont.Should().Be("SimSun");
    }

    [Fact]
    public void GetMappedFont_WithNonExistingFont_ShouldReturnOriginalFont()
    {
        // Arrange
        var fontName = "NonExistingFont";

        // Act
        var mappedFont = OfdrwConfiguration.GetMappedFont(fontName);

        // Assert
        mappedFont.Should().Be(fontName);
    }

    [Fact]
    public void PerformanceSettings_ShouldHaveReasonableDefaults()
    {
        // Act & Assert
        OfdrwConfiguration.EnableParallelProcessing.Should().BeTrue();
        OfdrwConfiguration.MaxDegreeOfParallelism.Should().BeGreaterThan(0);
        OfdrwConfiguration.MemoryCacheSizeMB.Should().Be(100);
        OfdrwConfiguration.EnableFileCache.Should().BeTrue();
        OfdrwConfiguration.TempDirectory.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RenderingSettings_ShouldHaveReasonableDefaults()
    {
        // Act & Assert
        OfdrwConfiguration.DefaultDpi.Should().Be(300);
        OfdrwConfiguration.ImageCompressionQuality.Should().Be(85);
        OfdrwConfiguration.EnableAntiAliasing.Should().BeTrue();
        OfdrwConfiguration.TextRenderingHint.Should().Be(TextRenderingHint.AntiAlias);
    }

    [Fact]
    public void LoggingSettings_ShouldHaveReasonableDefaults()
    {
        // Act & Assert
        OfdrwConfiguration.EnableLogging.Should().BeFalse(); // 默认关闭日志
        OfdrwConfiguration.LogLevel.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void SecuritySettings_ShouldHaveReasonableDefaults()
    {
        // Act & Assert
        OfdrwConfiguration.StrictMode.Should().BeFalse();
        OfdrwConfiguration.MaxDocumentSizeMB.Should().Be(1024);
        OfdrwConfiguration.MaxPageCount.Should().Be(10000);
    }

    [Fact]
    public void SetCustomSetting_ShouldAddSetting()
    {
        // Arrange
        var key = "TestKey";
        var value = "TestValue";

        // Act
        OfdrwConfiguration.SetCustomSetting(key, value);

        // Assert
        OfdrwConfiguration.CustomSettings.Should().ContainKey(key);
        OfdrwConfiguration.CustomSettings[key].Should().Be(value);
    }

    [Fact]
    public void GetCustomSetting_WithExistingSetting_ShouldReturnValue()
    {
        // Arrange
        var key = "ExistingKey";
        var expectedValue = "ExistingValue";
        OfdrwConfiguration.SetCustomSetting(key, expectedValue);

        // Act
        var actualValue = OfdrwConfiguration.GetCustomSetting<string>(key);

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void GetCustomSetting_WithNonExistingSetting_ShouldReturnDefault()
    {
        // Arrange
        var key = "NonExistingKey";
        var defaultValue = "DefaultValue";

        // Act
        var actualValue = OfdrwConfiguration.GetCustomSetting(key, defaultValue);

        // Assert
        actualValue.Should().Be(defaultValue);
    }

    [Fact]
    public void GetCustomSetting_WithDifferentTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var intKey = "IntKey";
        var intValue = 42;
        var boolKey = "BoolKey";
        var boolValue = true;

        OfdrwConfiguration.SetCustomSetting(intKey, intValue);
        OfdrwConfiguration.SetCustomSetting(boolKey, boolValue);

        // Act & Assert
        OfdrwConfiguration.GetCustomSetting<int>(intKey).Should().Be(intValue);
        OfdrwConfiguration.GetCustomSetting<bool>(boolKey).Should().Be(boolValue);
    }

    [Fact]
    public void ResetToDefaults_ShouldRestoreDefaultValues()
    {
        // Arrange - 修改一些设置
        OfdrwConfiguration.DefaultFont = "ModifiedFont";
        OfdrwConfiguration.DefaultFontSize = 99.9;
        OfdrwConfiguration.EnableParallelProcessing = false;
        OfdrwConfiguration.SetCustomSetting("TestKey", "TestValue");

        // Act
        OfdrwConfiguration.ResetToDefaults();

        // Assert
        OfdrwConfiguration.DefaultFont.Should().Be("SimSun");
        OfdrwConfiguration.DefaultFontSize.Should().Be(3.5);
        OfdrwConfiguration.EnableParallelProcessing.Should().BeTrue();
        OfdrwConfiguration.CustomSettings.Should().BeEmpty();
    }

    [Theory]
    [InlineData(TextRenderingHint.SystemDefault)]
    [InlineData(TextRenderingHint.AntiAlias)]
    [InlineData(TextRenderingHint.ClearTypeGridFit)]
    public void TextRenderingHint_ShouldAcceptValidValues(TextRenderingHint hint)
    {
        // Act
        OfdrwConfiguration.TextRenderingHint = hint;

        // Assert
        OfdrwConfiguration.TextRenderingHint.Should().Be(hint);
    }

    [Theory]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    public void LogLevel_ShouldAcceptValidValues(LogLevel level)
    {
        // Act
        OfdrwConfiguration.LogLevel = level;

        // Assert
        OfdrwConfiguration.LogLevel.Should().Be(level);
    }

    [Fact]
    public void ModifySettings_ShouldPersistChanges()
    {
        // Arrange
        var newFont = "NewTestFont";
        var newFontSize = 15.0;
        var newDpi = 600;

        // Act
        OfdrwConfiguration.DefaultFont = newFont;
        OfdrwConfiguration.DefaultFontSize = newFontSize;
        OfdrwConfiguration.DefaultDpi = newDpi;

        // Assert
        OfdrwConfiguration.DefaultFont.Should().Be(newFont);
        OfdrwConfiguration.DefaultFontSize.Should().Be(newFontSize);
        OfdrwConfiguration.DefaultDpi.Should().Be(newDpi);
    }
}
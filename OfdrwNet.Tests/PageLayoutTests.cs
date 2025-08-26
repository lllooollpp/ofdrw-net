using Xunit;
using FluentAssertions;
using OfdrwNet;

namespace OfdrwNet.Tests;

/// <summary>
/// PageLayout 类单元测试
/// </summary>
public class PageLayoutTests
{
    [Fact]
    public void A4_ShouldReturnStandardA4Size()
    {
        // Act
        var a4Layout = PageLayout.A4();

        // Assert
        a4Layout.Should().NotBeNull();
        a4Layout.Width.Should().Be(210.0); // A4宽度 210mm
        a4Layout.Height.Should().Be(297.0); // A4高度 297mm
        a4Layout.Margins.Should().NotBeNull();
        a4Layout.Margins.Top.Should().Be(25.4);
        a4Layout.Margins.Bottom.Should().Be(25.4);
        a4Layout.Margins.Left.Should().Be(31.7);
        a4Layout.Margins.Right.Should().Be(31.7);
    }

    [Fact]
    public void Clone_ShouldReturnDeepCopy()
    {
        // Arrange
        var original = PageLayout.A4();
        original.Width = 200;
        original.Height = 280;
        original.Margins.Top = 10;

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original); // 不同的实例
        clone.Width.Should().Be(original.Width);
        clone.Height.Should().Be(original.Height);
        clone.Margins.Should().NotBeSameAs(original.Margins); // 边距也应该是深拷贝
        clone.Margins.Top.Should().Be(original.Margins.Top);
    }

    [Theory]
    [InlineData(210.0, 297.0)] // A4
    [InlineData(216.0, 279.0)] // Letter
    [InlineData(148.0, 210.0)] // A5
    public void Constructor_WithCustomSize_ShouldSetProperties(double width, double height)
    {
        // Arrange & Act
        var layout = new PageLayout
        {
            Width = width,
            Height = height
        };

        // Assert
        layout.Width.Should().Be(width);
        layout.Height.Should().Be(height);
        layout.Margins.Should().NotBeNull();
    }
}

/// <summary>
/// Margins 类单元测试
/// </summary>
public class MarginsTests
{
    [Fact]
    public void Constructor_ShouldInitializeAllProperties()
    {
        // Act
        var margins = new Margins();

        // Assert
        margins.Top.Should().Be(0);
        margins.Bottom.Should().Be(0);
        margins.Left.Should().Be(0);
        margins.Right.Should().Be(0);
    }

    [Theory]
    [InlineData(10, 15, 20, 25)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(5.5, 10.25, 15.75, 20.125)]
    public void Properties_ShouldSetAndGetCorrectly(double top, double bottom, double left, double right)
    {
        // Arrange
        var margins = new Margins();

        // Act
        margins.Top = top;
        margins.Bottom = bottom;
        margins.Left = left;
        margins.Right = right;

        // Assert
        margins.Top.Should().Be(top);
        margins.Bottom.Should().Be(bottom);
        margins.Left.Should().Be(left);
        margins.Right.Should().Be(right);
    }

    [Fact]
    public void Clone_ShouldReturnDeepCopy()
    {
        // Arrange
        var original = new Margins
        {
            Top = 10,
            Bottom = 15,
            Left = 20,
            Right = 25
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Top.Should().Be(original.Top);
        clone.Bottom.Should().Be(original.Bottom);
        clone.Left.Should().Be(original.Left);
        clone.Right.Should().Be(original.Right);
    }

    [Fact]
    public void Clone_ModifyingClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = new Margins { Top = 10, Bottom = 15, Left = 20, Right = 25 };
        var clone = original.Clone();

        // Act
        clone.Top = 100;
        clone.Left = 200;

        // Assert
        original.Top.Should().Be(10); // 原始对象不变
        original.Left.Should().Be(20);
        clone.Top.Should().Be(100); // 克隆对象改变
        clone.Left.Should().Be(200);
    }
}

/// <summary>
/// TextParagraph 类单元测试
/// </summary>
public class TextParagraphTests
{
    [Fact]
    public void Constructor_WithText_ShouldSetText()
    {
        // Arrange
        var expectedText = "Hello, World!";

        // Act
        var paragraph = new TextParagraph(expectedText);

        // Assert
        paragraph.Text.Should().Be(expectedText);
        paragraph.FontSize.Should().Be(0); // 默认值
        paragraph.FontFamily.Should().Be("SimSun"); // 默认字体
        paragraph.Position.Should().NotBeNull();
    }

    [Theory]
    [InlineData("简单文本")]
    [InlineData("English Text")]
    [InlineData("混合 Mixed 文本")]
    [InlineData("")]
    public void Constructor_WithVariousText_ShouldSetText(string text)
    {
        // Act
        var paragraph = new TextParagraph(text);

        // Assert
        paragraph.Text.Should().Be(text);
    }

    [Fact]
    public void Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var paragraph = new TextParagraph("Test");
        var expectedFontSize = 12.5;
        var expectedFontFamily = "Arial";
        var expectedPosition = new Position(50, 100);

        // Act
        paragraph.FontSize = expectedFontSize;
        paragraph.FontFamily = expectedFontFamily;
        paragraph.Position = expectedPosition;

        // Assert
        paragraph.FontSize.Should().Be(expectedFontSize);
        paragraph.FontFamily.Should().Be(expectedFontFamily);
        paragraph.Position.Should().BeSameAs(expectedPosition);
    }
}

/// <summary>
/// Position 类单元测试
/// </summary>
public class PositionTests
{
    [Fact]
    public void DefaultConstructor_ShouldInitializeToZero()
    {
        // Act
        var position = new Position();

        // Assert
        position.X.Should().Be(0);
        position.Y.Should().Be(0);
    }

    [Theory]
    [InlineData(10.5, 20.75)]
    [InlineData(0, 0)]
    [InlineData(-10, -20)]
    [InlineData(100.123, 200.456)]
    public void Constructor_WithCoordinates_ShouldSetProperties(double x, double y)
    {
        // Act
        var position = new Position(x, y);

        // Assert
        position.X.Should().Be(x);
        position.Y.Should().Be(y);
    }

    [Fact]
    public void Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var position = new Position();
        var expectedX = 123.456;
        var expectedY = 789.012;

        // Act
        position.X = expectedX;
        position.Y = expectedY;

        // Assert
        position.X.Should().Be(expectedX);
        position.Y.Should().Be(expectedY);
    }
}
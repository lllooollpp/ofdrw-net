using Xunit;
using FluentAssertions;
using OfdrwNet.Core.BasicType;

namespace OfdrwNet.Tests.Core.BasicType;

/// <summary>
/// StId 类型单元测试
/// 对应 Java 版本的 ST_ID 测试
/// </summary>
public class StIdTests
{
    [Fact]
    public void Constructor_WithValidId_ShouldSetValue()
    {
        // Arrange
        var expectedId = 42;

        // Act
        var stId = new StId(expectedId);

        // Assert
        stId.Value.Should().Be(expectedId);
    }

    [Fact]
    public void Constructor_WithZero_ShouldSetValue()
    {
        // Arrange
        var expectedId = 0;

        // Act
        var stId = new StId(expectedId);

        // Assert
        stId.Value.Should().Be(expectedId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999999)]
    public void Constructor_WithVariousValidIds_ShouldSetValue(int id)
    {
        // Act
        var stId = new StId(id);

        // Assert
        stId.Value.Should().Be(id);
    }

    [Fact]
    public void ToString_ShouldReturnStringValue()
    {
        // Arrange
        var id = 123;
        var stId = new StId(id);

        // Act
        var result = stId.ToString();

        // Assert
        result.Should().Be("123");
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var id1 = new StId(42);
        var id2 = new StId(42);

        // Act & Assert
        id1.Equals(id2).Should().BeTrue();
        (id1 == id2).Should().BeTrue();
        (id1 != id2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var id1 = new StId(42);
        var id2 = new StId(43);

        // Act & Assert
        id1.Equals(id2).Should().BeFalse();
        (id1 == id2).Should().BeFalse();
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var id1 = new StId(42);
        var id2 = new StId(42);

        // Act & Assert
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }
}

/// <summary>
/// StBox 类型单元测试
/// 对应 Java 版本的 ST_Box 测试
/// </summary>
public class StBoxTests
{
    [Fact]
    public void Constructor_WithValidValues_ShouldSetProperties()
    {
        // Arrange
        var x = 10.0;
        var y = 20.0;
        var width = 100.0;
        var height = 50.0;

        // Act
        var box = new StBox(x, y, width, height);

        // Assert
        box.TopLeftX.Should().Be(x);
        box.TopLeftY.Should().Be(y);
        box.Width.Should().Be(width);
        box.Height.Should().Be(height);
    }

    [Fact]
    public void Constructor_WithZeroValues_ShouldSetProperties()
    {
        // Act
        var box = new StBox(0, 0, 0, 0);

        // Assert
        box.TopLeftX.Should().Be(0);
        box.TopLeftY.Should().Be(0);
        box.Width.Should().Be(0);
        box.Height.Should().Be(0);
    }

    [Theory]
    [InlineData(10, 20, 100, 50)]
    [InlineData(-10, -20, 200, 100)]
    [InlineData(0, 0, 210, 297)]
    public void Constructor_WithVariousValues_ShouldSetProperties(double x, double y, double width, double height)
    {
        // Act
        var box = new StBox(x, y, width, height);

        // Assert
        box.TopLeftX.Should().Be(x);
        box.TopLeftY.Should().Be(y);
        box.Width.Should().Be(width);
        box.Height.Should().Be(height);
    }

    [Fact]
    public void Right_ShouldReturnXPlusWidth()
    {
        // Arrange
        var box = new StBox(10, 20, 100, 50);

        // Act & Assert
        box.GetBottomRightX().Should().Be(110); // 10 + 100
    }

    [Fact]
    public void Bottom_ShouldReturnYPlusHeight()
    {
        // Arrange
        var box = new StBox(10, 20, 100, 50);

        // Act & Assert
        box.GetBottomRightY().Should().Be(70); // 20 + 50
    }

    [Fact]
    public void Contains_WithPointInside_ShouldReturnTrue()
    {
        // Arrange
        var box = new StBox(10, 20, 100, 50);

        // Act & Assert
        box.Contains(50, 40).Should().BeTrue(); // 点在框内
        box.Contains(10, 20).Should().BeTrue(); // 左上角
        box.Contains(110, 70).Should().BeTrue(); // 右下角
    }

    [Fact]
    public void Contains_WithPointOutside_ShouldReturnFalse()
    {
        // Arrange
        var box = new StBox(10, 20, 100, 50);

        // Act & Assert
        box.Contains(5, 40).Should().BeFalse(); // 左边外
        box.Contains(50, 15).Should().BeFalse(); // 上边外
        box.Contains(115, 40).Should().BeFalse(); // 右边外
        box.Contains(50, 75).Should().BeFalse(); // 下边外
    }

    [Fact]
    public void Intersects_WithOverlappingBox_ShouldReturnTrue()
    {
        // Arrange
        var box1 = new StBox(10, 20, 100, 50);
        var box2 = new StBox(50, 40, 80, 60);

        // Act & Assert
        box1.Intersects(box2).Should().BeTrue();
        box2.Intersects(box1).Should().BeTrue();
    }

    [Fact]
    public void Intersects_WithNonOverlappingBox_ShouldReturnFalse()
    {
        // Arrange
        var box1 = new StBox(10, 20, 50, 30);
        var box2 = new StBox(100, 80, 50, 30);

        // Act & Assert
        box1.Intersects(box2).Should().BeFalse();
        box2.Intersects(box1).Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var box = new StBox(10.5, 20.75, 100.25, 50.125);

        // Act
        var result = box.ToString();

        // Assert
        result.Should().Contain("10.5");
        result.Should().Contain("20.75");
        result.Should().Contain("100.25");
        result.Should().Contain("50.125");
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var box1 = new StBox(10, 20, 100, 50);
        var box2 = new StBox(10, 20, 100, 50);

        // Act & Assert
        box1.Equals(box2).Should().BeTrue();
        (box1 == box2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var box1 = new StBox(10, 20, 100, 50);
        var box2 = new StBox(10, 20, 100, 51);

        // Act & Assert
        box1.Equals(box2).Should().BeFalse();
        (box1 == box2).Should().BeFalse();
    }
}
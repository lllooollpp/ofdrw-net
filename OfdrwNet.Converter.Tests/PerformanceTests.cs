using Xunit;
using Xunit.Abstractions;
using OfdrwNet.Converter;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OfdrwNet.Converter.Tests;

/// <summary>
/// 性能测试类
/// 测试并行处理和顺序处理的性能差异
/// </summary>
public class PerformanceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testPdfPath;
    private readonly string _tempDir;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _testPdfPath = @"d:\workspace\ofdrw-master\ofdrw-net-specify\tests\fixtures\pdfs\simple_vector.pdf";
        _tempDir = Path.Combine(Path.GetTempPath(), "ofdrw_perf_test_" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDir);

        // 确保测试PDF文件存在
        if (!File.Exists(_testPdfPath))
        {
            throw new FileNotFoundException($"测试PDF文件不存在: {_testPdfPath}");
        }
    }

    public void Dispose()
    {
        // 清理临时文件
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // 忽略清理失败
        }
    }

    [Fact]
    public async Task SequentialProcessing_ShouldWork()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDir, "sequential.ofd");

        // Act
        var stopwatch = Stopwatch.StartNew();
        await ConvertHelper.PdfToOfdAsync(_testPdfPath, outputPath, new ConvertHelper.PdfToOfdOptions
        {
            MaxDegreeOfParallelism = 1, // 强制顺序处理
            Logger = null // 禁用日志以获得更准确的性能数据
        });
        stopwatch.Stop();

        // Assert
        Assert.True(File.Exists(outputPath), "输出文件应存在");
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0, "输出文件不应为空");

        _output.WriteLine($"顺序处理耗时: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"输出文件大小: {fileInfo.Length} bytes");
    }

    [Fact]
    public async Task ParallelProcessing_ShouldWork()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDir, "parallel.ofd");
        var parallelism = Math.Min(Environment.ProcessorCount, 4); // 限制最大并行度以避免过度测试

        // Act
        var stopwatch = Stopwatch.StartNew();
        await ConvertHelper.PdfToOfdAsync(_testPdfPath, outputPath, new ConvertHelper.PdfToOfdOptions
        {
            MaxDegreeOfParallelism = parallelism,
            Logger = null // 禁用日志以获得更准确的性能数据
        });
        stopwatch.Stop();

        // Assert
        Assert.True(File.Exists(outputPath), "输出文件应存在");
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0, "输出文件不应为空");

        _output.WriteLine($"并行处理 (并行度={parallelism}) 耗时: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"输出文件大小: {fileInfo.Length} bytes");
    }

    [Fact]
    public async Task PerformanceComparison_ShouldShowResults()
    {
        // Arrange
        var sequentialOutput = Path.Combine(_tempDir, "perf_sequential.ofd");
        var parallelOutput = Path.Combine(_tempDir, "perf_parallel.ofd");
        var parallelism = Math.Min(Environment.ProcessorCount, 4);

        // Act - 顺序处理
        var sequentialStopwatch = Stopwatch.StartNew();
        await ConvertHelper.PdfToOfdAsync(_testPdfPath, sequentialOutput, new ConvertHelper.PdfToOfdOptions
        {
            MaxDegreeOfParallelism = 1,
            Logger = null
        });
        sequentialStopwatch.Stop();

        // Act - 并行处理
        var parallelStopwatch = Stopwatch.StartNew();
        await ConvertHelper.PdfToOfdAsync(_testPdfPath, parallelOutput, new ConvertHelper.PdfToOfdOptions
        {
            MaxDegreeOfParallelism = parallelism,
            Logger = null
        });
        parallelStopwatch.Stop();

        // Assert
        Assert.True(File.Exists(sequentialOutput), "顺序处理输出文件应存在");
        Assert.True(File.Exists(parallelOutput), "并行处理输出文件应存在");

        var sequentialSize = new FileInfo(sequentialOutput).Length;
        var parallelSize = new FileInfo(parallelOutput).Length;

        // 文件大小应该相同（或非常接近）
        Assert.True(Math.Abs(sequentialSize - parallelSize) < 1024, "顺序和并行处理的文件大小应基本相同");

        // 输出性能对比结果
        var sequentialTime = sequentialStopwatch.ElapsedMilliseconds;
        var parallelTime = parallelStopwatch.ElapsedMilliseconds;
        var speedup = sequentialTime > 0 ? (double)sequentialTime / parallelTime : 1.0;

        _output.WriteLine("=== 性能测试结果 ===");
        _output.WriteLine($"CPU核心数: {Environment.ProcessorCount}");
        _output.WriteLine($"测试并行度: {parallelism}");
        _output.WriteLine($"顺序处理时间: {sequentialTime}ms");
        _output.WriteLine($"并行处理时间: {parallelTime}ms");
        _output.WriteLine($"性能提升倍数: {speedup:F2}x");
        _output.WriteLine($"文件大小: {sequentialSize} bytes");

        // 对于单页PDF，并行处理可能不会更快，但至少不应该更慢很多
        Assert.True(parallelTime <= sequentialTime * 2, "并行处理不应比顺序处理慢太多");
    }

    [Fact]
    public async Task LargeFilePerformanceHint_ShouldComplete()
    {
        // 这个测试主要是为了验证大文件处理的基本功能
        // 在实际项目中，应该有更大的测试文件

        var outputPath = Path.Combine(_tempDir, "large_hint.ofd");

        var stopwatch = Stopwatch.StartNew();
        await ConvertHelper.PdfToOfdAsync(_testPdfPath, outputPath, new ConvertHelper.PdfToOfdOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            ExtractAndEmbedFonts = true,
            RealImageEmbedding = true,
            Logger = null
        });
        stopwatch.Stop();

        Assert.True(File.Exists(outputPath), "输出文件应存在");

        _output.WriteLine($"大文件性能提示测试完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine("注意：对于真正的性能测试，需要使用更大的PDF文件");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task DifferentParallelismLevels_ShouldWork(int parallelism)
    {
        // Arrange
        var outputPath = Path.Combine(_tempDir, $"parallelism_{parallelism}.ofd");

        // Act
        var stopwatch = Stopwatch.StartNew();
        await ConvertHelper.PdfToOfdAsync(_testPdfPath, outputPath, new ConvertHelper.PdfToOfdOptions
        {
            MaxDegreeOfParallelism = parallelism,
            Logger = null
        });
        stopwatch.Stop();

        // Assert
        Assert.True(File.Exists(outputPath), "输出文件应存在");
        var fileInfo = new FileInfo(outputPath);
        Assert.True(fileInfo.Length > 0, "输出文件不应为空");

        _output.WriteLine($"并行度 {parallelism}: {stopwatch.ElapsedMilliseconds}ms, {fileInfo.Length} bytes");
    }
}
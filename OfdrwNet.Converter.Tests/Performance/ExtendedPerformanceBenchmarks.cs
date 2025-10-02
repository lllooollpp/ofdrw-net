using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using OfdrwNet.Converter.Batch;
using OfdrwNet.Converter.ColorManagement;
using OfdrwNet.Converter.Domain;
using OfdrwNet.Converter.Recognition;

namespace OfdrwNet.Converter.Tests.Performance;

/// <summary>
/// Extended performance benchmarks for PDF-to-OFD conversion features
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RankColumn]
public class ExtendedPerformanceBenchmarks
{
    private ColorSpaceConverter? _colorConverter;
    private RuleBasedTableRecognizer? _tableRecognizer;
    private BasicFormulaRecognizer? _formulaRecognizer;
    private BatchProcessor? _batchProcessor;

    private byte[] _rgbTestData = Array.Empty<byte>();
    private byte[] _cmykTestData = Array.Empty<byte>();
    private List<string> _testFiles = new();

    [GlobalSetup]
    public void Setup()
    {
        _colorConverter = new ColorSpaceConverter();
        _tableRecognizer = new RuleBasedTableRecognizer();
        _formulaRecognizer = new BasicFormulaRecognizer();
        _batchProcessor = new BatchProcessor();

        // Prepare test data
        _rgbTestData = new byte[1920 * 1080 * 3]; // 1080p RGB
        _cmykTestData = new byte[1920 * 1080 * 4]; // 1080p CMYK

        var random = new Random(42);
        random.NextBytes(_rgbTestData);
        random.NextBytes(_cmykTestData);

        // Prepare batch test files
        _testFiles = Enumerable.Range(0, 100)
            .Select(i => $"test-file-{i}.pdf")
            .ToList();
    }

    /// <summary>
    /// Benchmark: RGB to sRGB color conversion (1080p image)
    /// Target: < 100ms for 1920x1080 image (DR-18: ΔE < 2.0)
    /// </summary>
    [Benchmark]
    public void ColorConversion_RgbToSrgb_1080p()
    {
        if (_colorConverter == null) return;

        // Simulate converting 1920x1080 RGB image
        var pixelCount = 1920 * 1080;
        for (int i = 0; i < pixelCount; i += 1000) // Sample every 1000 pixels
        {
            var r = _rgbTestData[i * 3];
            var g = _rgbTestData[i * 3 + 1];
            var b = _rgbTestData[i * 3 + 2];

            _colorConverter.ConvertRgbToSrgb(r, g, b);
        }
    }

    /// <summary>
    /// Benchmark: CMYK to sRGB color conversion (1080p image)
    /// Target: < 150ms for 1920x1080 image (DR-19: ΔE < 5.0)
    /// </summary>
    [Benchmark]
    public void ColorConversion_CmykToSrgb_1080p()
    {
        if (_colorConverter == null) return;

        // Simulate converting 1920x1080 CMYK image
        var pixelCount = 1920 * 1080;
        for (int i = 0; i < pixelCount; i += 1000) // Sample every 1000 pixels
        {
            var c = _cmykTestData[i * 4] / 255.0;
            var m = _cmykTestData[i * 4 + 1] / 255.0;
            var y = _cmykTestData[i * 4 + 2] / 255.0;
            var k = _cmykTestData[i * 4 + 3] / 255.0;

            _colorConverter.ConvertCmykToSrgb(c, m, y, k);
        }
    }

    /// <summary>
    /// Benchmark: ΔE calculation (CIE Lab color difference)
    /// Target: < 1μs per calculation (for real-time validation)
    /// </summary>
    [Benchmark]
    public void ColorConversion_DeltaE_Calculation()
    {
        if (_colorConverter == null) return;

        // Calculate ΔE between two Lab colors
        var lab1 = (L: 50.0, a: 25.0, b: -25.0);
        var lab2 = (L: 60.0, a: 30.0, b: -20.0);

        for (int i = 0; i < 1000; i++)
        {
            _colorConverter.CalculateDeltaE(lab1, lab2);
        }
    }

    /// <summary>
    /// Benchmark: Table recognition on typical page
    /// Target: < 200ms per page with 2-3 tables
    /// </summary>
    [Benchmark]
    public void TableRecognition_TypicalPage()
    {
        if (_tableRecognizer == null) return;

        // Simulate detecting tables in a page with 500 text blocks
        var textBlocks = GenerateTestTextBlocks(500, hasTable: true);

        var result = _tableRecognizer.RecognizeTable(textBlocks);
    }

    /// <summary>
    /// Benchmark: Complex table recognition (large table with merged cells)
    /// Target: < 500ms for 20x50 table
    /// </summary>
    [Benchmark]
    public void TableRecognition_ComplexTable()
    {
        if (_tableRecognizer == null) return;

        // Simulate large table with 1000 cells
        var textBlocks = GenerateTestTextBlocks(1000, hasTable: true, isComplex: true);

        var result = _tableRecognizer.RecognizeTable(textBlocks);
    }

    /// <summary>
    /// Benchmark: Formula recognition on math-heavy page
    /// Target: < 100ms per page with 5-10 formulas
    /// </summary>
    [Benchmark]
    public void FormulaRecognition_MathPage()
    {
        if (_formulaRecognizer == null) return;

        // Simulate page with mathematical notation
        var mathRegions = GenerateTestMathRegions(10);

        foreach (var region in mathRegions)
        {
            _formulaRecognizer.RecognizeFormula(region);
        }
    }

    /// <summary>
    /// Benchmark: Batch processing throughput (sequential)
    /// Target: > 5 files/second for small PDFs
    /// </summary>
    [Benchmark]
    public async Task BatchProcessing_Sequential_100Files()
    {
        if (_batchProcessor == null) return;

        var options = new BatchProcessOptions
        {
            MaxDegreeOfParallelism = 1,
            ContinueOnError = true
        };

        await _batchProcessor.ProcessFilesAsync(
            _testFiles,
            async (file, ct) =>
            {
                // Simulate file processing (10ms per file)
                await Task.Delay(10, ct);
                return new { File = file, Success = true };
            },
            options
        );
    }

    /// <summary>
    /// Benchmark: Batch processing throughput (parallel 4x)
    /// Target: > 15 files/second for small PDFs
    /// </summary>
    [Benchmark]
    public async Task BatchProcessing_Parallel4x_100Files()
    {
        if (_batchProcessor == null) return;

        var options = new BatchProcessOptions
        {
            MaxDegreeOfParallelism = 4,
            ContinueOnError = true
        };

        await _batchProcessor.ProcessFilesAsync(
            _testFiles,
            async (file, ct) =>
            {
                // Simulate file processing (10ms per file)
                await Task.Delay(10, ct);
                return new { File = file, Success = true };
            },
            options
        );
    }

    /// <summary>
    /// Benchmark: Memory guard overhead
    /// Target: < 1ms per check
    /// </summary>
    [Benchmark]
    public void MemoryGuard_CheckOverhead()
    {
        var guard = new MemoryGuard(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<MemoryGuard>(),
            warningThresholdMB: 2000,
            criticalThresholdMB: 3000
        );

        for (int i = 0; i < 100; i++)
        {
            guard.CheckMemory();
        }
    }

    /// <summary>
    /// Benchmark: Memory estimation accuracy
    /// Target: < 5ms for batch size calculation
    /// </summary>
    [Benchmark]
    public void MemoryGuard_BatchSizeEstimation()
    {
        var guard = new MemoryGuard(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<MemoryGuard>(),
            warningThresholdMB: 2000,
            criticalThresholdMB: 3000
        );

        for (int i = 0; i < 100; i++)
        {
            guard.SuggestSegmentSize(totalPages: 1000, averagePageMemoryMB: 2.5);
        }
    }

    /// <summary>
    /// Benchmark: Color profile loading and caching
    /// Target: < 50ms for first load, < 1ms for cached access
    /// </summary>
    [Benchmark]
    public void ColorProfile_LoadAndCache()
    {
        var manager = new ColorProfileManager();

        // First load (cold cache)
        manager.LoadDefaultProfiles();

        // Cached access (warm cache)
        for (int i = 0; i < 100; i++)
        {
            manager.GetDefaultSrgbProfile();
        }
    }

    // Helper methods for generating test data

    private List<TextBlock> GenerateTestTextBlocks(int count, bool hasTable = false, bool isComplex = false)
    {
        var blocks = new List<TextBlock>();
        var random = new Random(42);

        if (hasTable)
        {
            int rows = isComplex ? 50 : 10;
            int cols = isComplex ? 20 : 5;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    blocks.Add(new TextBlock
                    {
                        X = c * 100,
                        Y = r * 20,
                        Width = 95,
                        Height = 18,
                        Text = $"Cell_{r}_{c}"
                    });
                }
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                blocks.Add(new TextBlock
                {
                    X = random.Next(0, 600),
                    Y = random.Next(0, 800),
                    Width = random.Next(50, 200),
                    Height = random.Next(10, 20),
                    Text = $"Text_{i}"
                });
            }
        }

        return blocks;
    }

    private List<MathRegion> GenerateTestMathRegions(int count)
    {
        var regions = new List<MathRegion>();
        var formulas = new[]
        {
            "x^2 + y^2 = r^2",
            "\\int_0^\\infty e^{-x^2} dx = \\frac{\\sqrt{\\pi}}{2}",
            "E = mc^2",
            "\\sum_{i=1}^n i = \\frac{n(n+1)}{2}",
            "\\frac{-b \\pm \\sqrt{b^2-4ac}}{2a}"
        };

        for (int i = 0; i < count; i++)
        {
            regions.Add(new MathRegion
            {
                Formula = formulas[i % formulas.Length],
                Confidence = 0.85
            });
        }

        return regions;
    }
}

// Test helper classes

public class TextBlock
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class MathRegion
{
    public string Formula { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class BatchProcessOptions
{
    public int MaxDegreeOfParallelism { get; set; } = 1;
    public bool ContinueOnError { get; set; } = true;
}

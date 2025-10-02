using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace OfdrwNet.Converter.Tests.Integration;

public class ColorManagementScenarioTests
{
    [Fact]
    public async Task Color_Metrics_ShouldBeCaptured()
    {
        var fixturePdf = TestFileHelpers.GetFixturePdf("0.pdf");
        Assert.True(File.Exists(fixturePdf), $"Fixture PDF not found at {fixturePdf}");

        var workDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ofd-color-" + Guid.NewGuid().ToString("N"))).FullName;
        var outputDir = Path.Combine(workDir, "out_dir");
        Directory.CreateDirectory(outputDir);

        var args = new[]
        {
            "convert-ofd",
            "--input", fixturePdf,
            "--output", outputDir,
            "--table-recog-threshold", "0.8",
            "--formula-recog-threshold", "0.8",
            "--compat-level", "Std2020",
            "--target-reader", "Foxit",
            "--render-intent", "perceptual",
            "--max-mem", "512",
            "--pages-per-segment", "100",
            "--perm", "print=true,modify=false",
            "--version-policy", "maxChain=30,sizeLimit=3x"
        };

        var result = await CliProcessRunner.RunCliAsync(args);
        Assert.True(result.ExitCode == 0, result.FormatForFailure());

        var logDir = Path.Combine(outputDir, "logs");
        Assert.True(Directory.Exists(logDir), "logs directory should be created within the output folder");

        var structuredLogPath = Path.Combine(logDir, "structured-log.ndjson");
        Assert.True(File.Exists(structuredLogPath), "structured-log.ndjson should be emitted with structured events");

        var lines = await File.ReadAllLinesAsync(structuredLogPath);
        Assert.Contains(lines, line => line.Contains("\"event\":\"ColorDelta\"", StringComparison.Ordinal));

        var colorMetricsPath = Path.Combine(outputDir, "color-metrics.json");
        Assert.True(File.Exists(colorMetricsPath), "color-metrics.json should be emitted to summarize ΔE statistics");

        await using var stream = File.OpenRead(colorMetricsPath);
        var json = await JsonNode.ParseAsync(stream);
        Assert.NotNull(json);

        var avg = json!["avg"]?.GetValue<double>();
        Assert.NotNull(avg);

        var max = json["max"]?.GetValue<double>();
        Assert.NotNull(max);
    }
}

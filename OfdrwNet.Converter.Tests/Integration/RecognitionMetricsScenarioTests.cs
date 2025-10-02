using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace OfdrwNet.Converter.Tests.Integration;

public class RecognitionMetricsScenarioTests
{
    [Fact]
    public async Task Recognition_Metrics_ShouldBeReported()
    {
        var fixturePdf = TestFileHelpers.GetFixturePdf("1.pdf");
        Assert.True(File.Exists(fixturePdf), $"Fixture PDF not found at {fixturePdf}");

        var workDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ofd-recog-" + Guid.NewGuid().ToString("N"))).FullName;
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
        Assert.True(Directory.Exists(logDir), "logs directory should be generated");

        var structuredLogPath = Path.Combine(logDir, "structured-log.ndjson");
        Assert.True(File.Exists(structuredLogPath), "structured-log.ndjson should be present for structured events");

        var lines = await File.ReadAllLinesAsync(structuredLogPath);
        Assert.Contains(lines, line => line.Contains("\"event\":\"RecognitionMetrics\"", StringComparison.Ordinal));

        var recognitionMetricsPath = Path.Combine(outputDir, "recognition-metrics.json");
        Assert.True(File.Exists(recognitionMetricsPath), "recognition-metrics.json should be emitted for recognition stats");

        await using var stream = File.OpenRead(recognitionMetricsPath);
        var json = await JsonNode.ParseAsync(stream);
        Assert.NotNull(json);

        var tables = json!["tables"]?.AsObject();
        Assert.NotNull(tables);
        Assert.NotNull(tables!["count"]);
        Assert.NotNull(tables["precision"]);
        Assert.NotNull(tables["recall"]);

        var formulas = json["formulas"]?.AsObject();
        Assert.NotNull(formulas);
        Assert.NotNull(formulas!["count"]);
        Assert.NotNull(formulas["accuracy"]);
    }
}

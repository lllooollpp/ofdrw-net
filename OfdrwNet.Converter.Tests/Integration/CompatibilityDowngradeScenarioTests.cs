using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace OfdrwNet.Converter.Tests.Integration;

public class CompatibilityDowngradeScenarioTests
{
    [Fact]
    public async Task Compatibility_Profile_ShouldTriggerDowngradeActions()
    {
        var fixturePdf = TestFileHelpers.GetFixturePdf("2.pdf");
        Assert.True(File.Exists(fixturePdf), $"Fixture PDF not found at {fixturePdf}");

        var workDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ofd-compat-" + Guid.NewGuid().ToString("N"))).FullName;
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
        Assert.True(Directory.Exists(logDir), "logs directory should exist when compatibility downgrade runs");

        var structuredLogPath = Path.Combine(logDir, "structured-log.ndjson");
        Assert.True(File.Exists(structuredLogPath), "structured-log.ndjson should be created for downgrade events");

        var logLines = await File.ReadAllLinesAsync(structuredLogPath);
        Assert.Contains(logLines, line => line.Contains("\"event\":\"Downgrade\"", StringComparison.Ordinal));
        Assert.Contains(logLines, line => line.Contains("\"reader\":\"Foxit\"", StringComparison.Ordinal));

        var reportPath = Path.Combine(outputDir, "compatibility-report.json");
        Assert.True(File.Exists(reportPath), "compatibility-report.json should summarize downgrade actions");

        await using var reportStream = File.OpenRead(reportPath);
        var reportJson = await JsonNode.ParseAsync(reportStream);
        Assert.NotNull(reportJson);

        var actions = reportJson!["actions"]?.AsArray();
        Assert.NotNull(actions);
        Assert.True(actions!.Any(), "compatibility-report.json should list at least one downgrade action");

        var firstAction = actions![0]?.AsObject();
        Assert.NotNull(firstAction);
        Assert.True(firstAction!.ContainsKey("feature"), "downgrade action should include feature name");
        Assert.True(firstAction.ContainsKey("mode"), "downgrade action should include applied mode");

        var conversionReportPath = Path.Combine(outputDir, "conversion-report.json");
        Assert.True(File.Exists(conversionReportPath), "conversion-report.json should still be emitted for compatibility runs");

        await using var conversionStream = File.OpenRead(conversionReportPath);
        var conversionJson = await JsonNode.ParseAsync(conversionStream);
        Assert.NotNull(conversionJson);

        var downgradedFeatures = conversionJson!["stats"]? ["downgradedFeatures"]?.GetValue<int?>();
        Assert.NotNull(downgradedFeatures);
        Assert.True(downgradedFeatures!.Value > 0, "conversion report should record downgraded feature count");
    }
}

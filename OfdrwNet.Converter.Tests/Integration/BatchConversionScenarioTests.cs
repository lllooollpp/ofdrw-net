using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace OfdrwNet.Converter.Tests.Integration;

public class BatchConversionScenarioTests
{
    [Fact]
    public async Task Batch_Command_ShouldProcessMultipleFiles()
    {
        var fixtures = new[]
        {
            TestFileHelpers.GetFixturePdf("0.pdf"),
            TestFileHelpers.GetFixturePdf("1.pdf")
        };

        foreach (var fixture in fixtures)
        {
            Assert.True(File.Exists(fixture), $"Fixture PDF not found at {fixture}");
        }

        var workDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ofd-batch-" + Guid.NewGuid().ToString("N"))).FullName;
        var inputDir = Path.Combine(workDir, "pdfs");
        Directory.CreateDirectory(inputDir);

        foreach (var fixture in fixtures)
        {
            var destination = Path.Combine(inputDir, Path.GetFileName(fixture)!);
            File.Copy(fixture, destination, overwrite: true);
        }

        var outputRoot = Path.Combine(workDir, "outputs");
        Directory.CreateDirectory(outputRoot);

        var args = new[]
        {
            "convert-ofd-batch",
            "--input-dir", inputDir,
            "--output-root", outputRoot,
            "--parallel", "2",
            "--max-mem", "1024"
        };

        var result = await CliProcessRunner.RunCliAsync(args);
        Assert.True(result.ExitCode == 0, result.FormatForFailure());

        var summaryPath = Path.Combine(outputRoot, "batch-summary.json");
        Assert.True(File.Exists(summaryPath), "batch-summary.json should be emitted to the output root directory");

        await using var summaryStream = File.OpenRead(summaryPath);
        var summaryJson = await JsonNode.ParseAsync(summaryStream);
        Assert.NotNull(summaryJson);

        var total = summaryJson!["total"]?.GetValue<int>();
        Assert.Equal(fixtures.Length, total);

        var success = summaryJson["success"]?.GetValue<int>();
        Assert.Equal(fixtures.Length, success);

        foreach (var fixture in fixtures)
        {
            var reportDir = Path.Combine(outputRoot, Path.GetFileNameWithoutExtension(fixture)!);
            var reportPath = Path.Combine(reportDir, "conversion-report.json");
            Assert.True(File.Exists(reportPath), $"conversion-report.json should exist for {fixture}");
        }
    }
}

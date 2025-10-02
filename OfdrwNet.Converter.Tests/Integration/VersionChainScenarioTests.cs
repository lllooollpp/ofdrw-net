using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace OfdrwNet.Converter.Tests.Integration;

public class VersionChainScenarioTests
{
    [Fact]
    public async Task Append_Version_ShouldUpdateChainMetadata()
    {
        var fixturePdf = TestFileHelpers.GetFixturePdf("0.pdf");
        Assert.True(File.Exists(fixturePdf), $"Fixture PDF not found at {fixturePdf}");

        var workDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ofd-version-" + Guid.NewGuid().ToString("N"))).FullName;
        var outputDir = Path.Combine(workDir, "doc_v");
        Directory.CreateDirectory(outputDir);

        var baseArgs = new[]
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

        var baseResult = await CliProcessRunner.RunCliAsync(baseArgs);
        Assert.True(baseResult.ExitCode == 0, baseResult.FormatForFailure());

        var chainPath = Path.Combine(outputDir, "version-chain.json");
        Assert.True(File.Exists(chainPath), "version-chain.json should be created after initial conversion");

        await using (var initialStream = File.OpenRead(chainPath))
        {
            var initialJson = await JsonNode.ParseAsync(initialStream);
            Assert.NotNull(initialJson);

            var initialEntries = initialJson!["entries"]?.AsArray();
            Assert.NotNull(initialEntries);
            Assert.True(initialEntries!.Count >= 1, "version chain should contain at least one entry after initial conversion");
        }

        var appendArgs = new[]
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
            "--version-policy", "maxChain=30,sizeLimit=3x",
            "--append-version"
        };

        var appendResult = await CliProcessRunner.RunCliAsync(appendArgs);
        Assert.True(appendResult.ExitCode == 0, appendResult.FormatForFailure());

        await using var finalStream = File.OpenRead(chainPath);
        var finalJson = await JsonNode.ParseAsync(finalStream);
        Assert.NotNull(finalJson);

        var entries = finalJson!["entries"]?.AsArray();
        Assert.NotNull(entries);
        Assert.True(entries!.Count >= 2, "version chain should contain at least two entries after appending");

        var current = finalJson["current"]?.GetValue<int>();
        if (current.HasValue)
        {
            Assert.Equal(entries.Count, current.Value);
        }
    }
}

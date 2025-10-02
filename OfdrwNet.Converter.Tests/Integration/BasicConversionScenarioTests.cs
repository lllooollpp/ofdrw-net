using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace OfdrwNet.Converter.Tests.Integration;

public class BasicConversionScenarioTests
{
    [Fact]
    public async Task Quickstart_Command_ShouldProduceConversionReport()
    {
        var fixturePdf = TestFileHelpers.GetFixturePdf("0.pdf");
        Assert.True(File.Exists(fixturePdf), $"Fixture PDF not found at {fixturePdf}");

        var workDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ofd-basic-" + Guid.NewGuid().ToString("N"))).FullName;
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
            "--version-policy", "maxChain=30,sizeLimit=3x",
            "--run-js-snapshot"
        };

    var result = await CliProcessRunner.RunConvertOfdAsync(args);
    Assert.True(result.ExitCode == 0, result.FormatForFailure());

        var reportPath = Path.Combine(outputDir, "conversion-report.json");
        Assert.True(File.Exists(reportPath), "conversion-report.json should be emitted to the output directory");

        await using var stream = File.OpenRead(reportPath);
        var json = await JsonNode.ParseAsync(stream);
        Assert.NotNull(json);

        var stats = json!["stats"];
        Assert.NotNull(stats);
        Assert.NotNull(stats!["pages"]);
        Assert.NotNull(stats["tablesRecognized"]);
        Assert.NotNull(stats["formulasRecognized"]);
        Assert.NotNull(stats["downgradedFeatures"]);

        var colorDelta = json["colorDelta"];
        Assert.NotNull(colorDelta);
        Assert.NotNull(colorDelta!["avg"]);
        Assert.NotNull(colorDelta["max"]);
    }
}

internal sealed record CliProcessResult(int ExitCode, string StdOut, string StdErr)
{
    internal string FormatForFailure()
        => $"CLI exited with code {ExitCode}{Environment.NewLine}STDOUT:{Environment.NewLine}{StdOut}{Environment.NewLine}STDERR:{Environment.NewLine}{StdErr}";
}

internal static class CliProcessRunner
{
    internal static Task<CliProcessResult> RunConvertOfdAsync(string[] args)
        => RunCliAsync(args);

    internal static async Task<CliProcessResult> RunCliAsync(string[] args)
    {
        var repoRoot = TestFileHelpers.GetRepoRoot();
        var cliProject = Path.Combine(repoRoot, "src", "OfdrwNet.Cli", "OfdrwNet.Cli.csproj");
        Assert.True(File.Exists(cliProject), $"CLI project not found at {cliProject}");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{cliProject}\" -- {string.Join(" ", args.Select(QuoteArg))}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = repoRoot
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process for CLI");

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(stdOutTask, stdErrTask, process.WaitForExitAsync());

        return new CliProcessResult(process.ExitCode, stdOutTask.Result, stdErrTask.Result);
    }

    private static string QuoteArg(string arg)
        => string.IsNullOrEmpty(arg) || arg.Any(char.IsWhiteSpace) ? $"\"{arg.Replace("\"", "\\\"")}\"" : arg;
}

internal static class TestFileHelpers
{
    internal static string GetFixturePdf(string fileName)
    {
        var root = GetRepoRoot();
        return Path.Combine(root, "tests", fileName);
    }

    internal static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var src = Path.Combine(directory.FullName, "src");
            var tests = Path.Combine(directory.FullName, "tests");
            if (Directory.Exists(src) && Directory.Exists(tests))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root containing both 'src' and 'tests' directories");
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace OfdrwNet.Converter.Tests.Integration;

public class PermissionEncryptionScenarioTests
{
    [Fact]
    public async Task Permissions_And_Encryption_ShouldBeApplied()
    {
        var repoRoot = TestFileHelpers.GetRepoRoot();
        var fixtureOfd = Path.Combine(repoRoot, "test_cli_output.ofd");
        Assert.True(File.Exists(fixtureOfd), $"Fixture OFD not found at {fixtureOfd}");

        var workDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ofd-security-" + Guid.NewGuid().ToString("N"))).FullName;
        var inputOfd = Path.Combine(workDir, "input.ofd");
        File.Copy(fixtureOfd, inputOfd, overwrite: true);

        var outputDir = Directory.CreateDirectory(Path.Combine(workDir, "signed"));
        var outputOfd = Path.Combine(outputDir.FullName, "signed.ofd");

        var sealPath = Path.Combine(workDir, "seal.png");
        await File.WriteAllBytesAsync(sealPath, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAASsJTYQAAAAASUVORK5CYII="));

        var args = new[]
        {
            "apply-signer",
            "--ofd", inputOfd,
            "--output", outputOfd,
            "--signer", "pkcs11:slot=0,label=FTKey",
            "--perm", "print=false,modify=false,export=false",
            "--encrypt-algorithm", "SM4-CBC",
            "--encrypt-user", "viewer-password",
            "--encrypt-owner", "owner-password",
            "--seal", $"image={sealPath},x=120,y=300"
        };

        var result = await CliProcessRunner.RunCliAsync(args);
        Assert.True(result.ExitCode == 0, result.FormatForFailure());

        Assert.True(File.Exists(outputOfd), "Signed OFD should be created at the specified output path");

        var reportPath = Path.Combine(outputDir.FullName, "security-report.json");
        Assert.True(File.Exists(reportPath), "security-report.json should summarize permission and encryption state");

        await using var reportStream = File.OpenRead(reportPath);
        var json = await JsonNode.ParseAsync(reportStream);
        Assert.NotNull(json);

        var permissions = json!["permissions"]?.AsObject();
        Assert.NotNull(permissions);
        Assert.True(permissions!.TryGetPropertyValue("print", out var printNode));
        Assert.True(permissions.TryGetPropertyValue("modify", out var modifyNode));
        Assert.True(permissions.TryGetPropertyValue("export", out var exportNode));

        var encryption = json["encryption"]?.AsObject();
        Assert.NotNull(encryption);
        Assert.Equal("SM4-CBC", encryption!["algorithm"]?.GetValue<string>());
        Assert.True(encryption.TryGetPropertyValue("keyLength", out _));

        var logsDir = Path.Combine(outputDir.FullName, "logs");
        Assert.True(Directory.Exists(logsDir), "logs directory should be emitted for security operations");

        var structuredLogPath = Path.Combine(logsDir, "structured-log.ndjson");
        Assert.True(File.Exists(structuredLogPath), "structured-log.ndjson should capture SecurityApplied events");

        var logLines = await File.ReadAllLinesAsync(structuredLogPath);
        Assert.Contains(logLines, line => line.Contains("\"event\":\"SecurityApplied\"", StringComparison.Ordinal));
    }
}

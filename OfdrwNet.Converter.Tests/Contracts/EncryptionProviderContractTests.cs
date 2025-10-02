using System.IO;
using System.Linq;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class EncryptionProviderContractTests
{
    [Fact]
    public void EncryptionProvider_ShouldBePublicInterfaceWithAlgorithmMetadata()
    {
        var provider = ContractReflection.FindContractType("OfdrwNet.Converter.Security.IEncryptionProvider");
        Assert.NotNull(provider);

        var type = provider!;
        Assert.True(type.IsInterface, "IEncryptionProvider should be an interface");
        Assert.True(type.IsPublic, "IEncryptionProvider must be public to allow adapter implementations");

        var algorithm = type.GetProperty("Algorithm");
        Assert.NotNull(algorithm);
        Assert.Equal(typeof(string), algorithm!.PropertyType);
        Assert.True(algorithm.CanRead, "Algorithm getter must be exposed");
        Assert.False(algorithm.CanWrite, "Algorithm should be metadata only and not writable");

        var wrapWrite = type.GetMethod("WrapWrite");
        var wrapRead = type.GetMethod("WrapRead");
        Assert.NotNull(wrapWrite);
        Assert.NotNull(wrapRead);
        Assert.Equal(typeof(Stream), wrapWrite!.ReturnType);
        Assert.Equal(typeof(Stream), wrapRead!.ReturnType);
    }

    [Fact]
    public void EncryptionProvider_WrapMethods_ShouldAcceptSingleStreamParameter()
    {
        var provider = ContractReflection.FindContractType("OfdrwNet.Converter.Security.IEncryptionProvider");
        Assert.NotNull(provider);

        var write = provider!.GetMethod("WrapWrite");
        var read = provider.GetMethod("WrapRead");

        Assert.NotNull(write);
        Assert.NotNull(read);

        AssertSingleStreamParameter(write!);
        AssertSingleStreamParameter(read!);
    }

    private static void AssertSingleStreamParameter(System.Reflection.MethodInfo method)
    {
        Assert.False(method.IsGenericMethodDefinition, $"{method.Name} should not be generic");
        Assert.False(method.IsStatic, $"{method.Name} must operate on the provider instance");

        var parameters = method.GetParameters();
        Assert.Single(parameters);

        var parameter = parameters.Single();
        Assert.Equal(typeof(Stream), parameter.ParameterType);
        Assert.False(parameter.IsOptional, "Stream wrapping should require an explicit stream");
        Assert.False(parameter.IsOut || parameter.ParameterType.IsByRef, "Stream should be passed by value");
    }
}

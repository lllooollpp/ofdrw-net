using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class SignerContractTests
{
    [Fact]
    public void Signer_Interface_ShouldExposeCapabilities()
    {
        var signer = ContractReflection.FindContractType("OfdrwNet.Sign.ISigner");
        Assert.NotNull(signer);

        var idProperty = signer!.GetProperty("Id");
        Assert.NotNull(idProperty);
        Assert.Equal(typeof(string), idProperty!.PropertyType);

        var capabilities = signer.GetProperty("Capabilities");
        Assert.NotNull(capabilities);
        Assert.Equal("OfdrwNet.Sign.SignerCapabilities", capabilities!.PropertyType.FullName);

        var method = signer.GetMethods().SingleOrDefault(m => m.Name == "Sign");
        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(byte[]), parameters[0].ParameterType);
        Assert.Equal("OfdrwNet.Sign.SignerContext", parameters[1].ParameterType.FullName);
        Assert.Equal(typeof(byte[]), method.ReturnType);
    }

    [Fact]
    public void SignerContext_ShouldExposeInitOnlyPropertiesWithDefaults()
    {
        var contextType = ContractReflection.FindContractType("OfdrwNet.Sign.SignerContext");
        Assert.NotNull(contextType);
        Assert.True(contextType!.IsSealed);

        var certId = contextType.GetProperty("CertId");
        Assert.NotNull(certId);
        Assert.Equal(typeof(string), certId!.PropertyType);
        Assert.True(IsInitOnly(certId));

        var algorithm = contextType.GetProperty("Algorithm");
        Assert.NotNull(algorithm);
        Assert.Equal(typeof(string), algorithm!.PropertyType);
        Assert.True(IsInitOnly(algorithm));

        var extra = contextType.GetProperty("Extra");
        Assert.NotNull(extra);
        Assert.Equal(typeof(IReadOnlyDictionary<string, string>), extra!.PropertyType);
        Assert.True(IsInitOnly(extra));

        var instance = Activator.CreateInstance(contextType)!;
        Assert.Equal(string.Empty, certId.GetValue(instance));
        Assert.Equal("SM2", algorithm.GetValue(instance));

        var extraValue = extra.GetValue(instance);
        Assert.NotNull(extraValue);
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(extraValue);
        Assert.Empty((IEnumerable)extraValue!);
    }

    [Fact]
    public void SignerCapabilities_ShouldBeFlagsWithExpectedValues()
    {
        var capabilitiesType = ContractReflection.FindContractType("OfdrwNet.Sign.SignerCapabilities");
        Assert.NotNull(capabilitiesType);
        Assert.True(capabilitiesType!.IsEnum);
        Assert.NotNull(capabilitiesType.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).SingleOrDefault());

        var names = Enum.GetNames(capabilitiesType);
        Assert.Contains("None", names);
        Assert.Contains("Detached", names);
        Assert.Contains("Timestamp", names);
        Assert.Contains("Batch", names);

        Assert.Equal(0, (int)Enum.Parse(capabilitiesType, "None"));
        Assert.Equal(1, (int)Enum.Parse(capabilitiesType, "Detached"));
        Assert.Equal(2, (int)Enum.Parse(capabilitiesType, "Timestamp"));
        Assert.Equal(4, (int)Enum.Parse(capabilitiesType, "Batch"));
    }

    private static bool IsInitOnly(System.Reflection.PropertyInfo property)
    {
        var setter = property.SetMethod;
        if (setter is null)
        {
            return false;
        }

        var modifiers = setter.ReturnParameter.GetRequiredCustomModifiers();
        return modifiers.Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
    }
}

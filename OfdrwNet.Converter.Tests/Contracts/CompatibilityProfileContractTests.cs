using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class CompatibilityProfileContractTests
{
    [Fact]
    public void CompatibilityProvider_ShouldLoadProfiles()
    {
        var provider = ContractReflection.FindContractType("OfdrwNet.Converter.Compatibility.ICompatibilityProfileProvider");
        Assert.NotNull(provider);
        Assert.True(provider!.IsInterface, "ICompatibilityProfileProvider should be an interface");

        var method = provider.GetMethods().SingleOrDefault(m => m.Name == "Load");
        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("OfdrwNet.Converter.Compatibility.CompatLevel", parameters[1].ParameterType.FullName);

        Assert.Equal("OfdrwNet.Converter.Compatibility.CompatibilityProfile", method.ReturnType.FullName);
        Assert.False(method.IsGenericMethod, "Load method should not be generic");
        Assert.True(parameters[0].IsOptional == false, "ReaderId parameter should be required");
        Assert.True(parameters[1].IsOptional == false, "CompatLevel parameter should be required");
    }

    [Fact]
    public void CompatibilityProfile_ShouldBeSealedWithInitOnlyPropertiesAndDefaults()
    {
        var profileType = ContractReflection.FindContractType("OfdrwNet.Converter.Compatibility.CompatibilityProfile");
        Assert.NotNull(profileType);

        var type = profileType!;
        Assert.True(type.IsClass, "CompatibilityProfile should be a class");
        Assert.True(type.IsSealed, "CompatibilityProfile should be sealed");
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        var readerId = type.GetProperty("ReaderId");
        Assert.NotNull(readerId);
        Assert.Equal(typeof(string), readerId!.PropertyType);
        Assert.True(IsInitOnly(readerId), "ReaderId must be init-only");
        Assert.Equal(string.Empty, readerId.GetValue(instance));

        var level = type.GetProperty("Level");
        Assert.NotNull(level);
        Assert.Equal("OfdrwNet.Converter.Compatibility.CompatLevel", level!.PropertyType.FullName);
        Assert.True(IsInitOnly(level), "Level must be init-only");

        var unsupportedFeatures = type.GetProperty("UnsupportedFeatures");
        Assert.NotNull(unsupportedFeatures);
        Assert.True(typeof(IReadOnlySet<string>).IsAssignableFrom(unsupportedFeatures!.PropertyType));
        Assert.True(IsInitOnly(unsupportedFeatures), "UnsupportedFeatures must be init-only");

        var value = unsupportedFeatures.GetValue(instance) as IEnumerable<string>;
        Assert.NotNull(value);
        Assert.Empty(value!);
    }

    [Fact]
    public void CompatLevel_ShouldExposeExpectedValues()
    {
        var compatLevel = ContractReflection.FindContractType("OfdrwNet.Converter.Compatibility.CompatLevel");
        Assert.NotNull(compatLevel);

        var type = compatLevel!;
        Assert.True(type.IsEnum, "CompatLevel should be an enum");

        var names = Enum.GetNames(type);
        Assert.Contains("Base", names);
        Assert.Contains("Std2020", names);
        Assert.Contains("Full", names);

    var values = Enum.GetValues(type).Cast<int>().ToArray();
    Assert.Equal(values.Length, values.Distinct().Count());
    }

    private static bool IsInitOnly(PropertyInfo? property)
    {
        if (property?.SetMethod is null)
        {
            return false;
        }

        return property.SetMethod.ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(IsExternalInit));
    }
}

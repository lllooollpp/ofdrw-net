using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class ColorSpaceConverterContractTests
{
    [Fact]
    public void ColorSpaceConverter_Interface_ShouldExposeConversionApi()
    {
        var converterType = ContractReflection.FindContractType("OfdrwNet.Color.IColorSpaceConverter");
        Assert.NotNull(converterType);

        var type = converterType!;
        Assert.True(type.IsInterface, "IColorSpaceConverter should be declared as an interface");
        Assert.True(type.IsPublic, "IColorSpaceConverter must be public for DI discovery");

        var colorValueType = ContractReflection.FindContractType("OfdrwNet.Color.ColorValue");
        Assert.NotNull(colorValueType);
        var colorSpaceType = ContractReflection.FindContractType("OfdrwNet.Color.ColorSpace");
        Assert.NotNull(colorSpaceType);
        var intentType = ContractReflection.FindContractType("OfdrwNet.Color.RenderingIntent");
        Assert.NotNull(intentType);

        var convertAsync = type.GetMethod("ConvertAsync");
        Assert.NotNull(convertAsync);
        Assert.False(convertAsync!.IsStatic, "ConvertAsync should operate on converter instances");
        Assert.True(typeof(Task).IsAssignableFrom(convertAsync.ReturnType), "ConvertAsync must be asynchronous");
        Assert.True(convertAsync.ReturnType.IsGenericType, "ConvertAsync should return Task<T>");
        Assert.Equal("OfdrwNet.Color.ColorConversionResult", convertAsync.ReturnType.GetGenericArguments()[0].FullName);

        var parameters = convertAsync.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal("OfdrwNet.Color.ColorValue", parameters[0].ParameterType.FullName);
        Assert.False(parameters[0].IsOptional, "Source color value should be required");
        Assert.Equal("OfdrwNet.Color.ColorSpace", parameters[1].ParameterType.FullName);
        Assert.False(parameters[1].IsOptional, "Target color space should be required");
        Assert.Equal("OfdrwNet.Color.RenderingIntent", parameters[2].ParameterType.FullName);
        Assert.False(parameters[2].IsOptional, "Rendering intent should be required");
        Assert.Equal(typeof(CancellationToken), parameters[3].ParameterType);
        Assert.True(parameters[3].IsOptional || parameters[3].HasDefaultValue, "CancellationToken should be optional with a default value");
    }

    [Fact]
    public void ColorSpaceConverter_ShouldExposeDeltaEAndProfileContracts()
    {
        var converterType = ContractReflection.FindContractType("OfdrwNet.Color.IColorSpaceConverter");
        Assert.NotNull(converterType);
        var type = converterType!;

        var colorValueType = ContractReflection.FindContractType("OfdrwNet.Color.ColorValue");
        Assert.NotNull(colorValueType);

        var delta = type.GetMethod("CalculateDeltaE2000");
        Assert.NotNull(delta);
        Assert.Equal(typeof(double), delta!.ReturnType);
        var deltaParameters = delta.GetParameters();
        Assert.Equal(2, deltaParameters.Length);
        Assert.All(deltaParameters, p => Assert.Equal("OfdrwNet.Color.ColorValue", p.ParameterType.FullName));
        Assert.All(deltaParameters, p => Assert.False(p.IsOptional, "ColorValue parameters should be mandatory"));

        var loadProfile = type.GetMethod("LoadProfileAsync");
        Assert.NotNull(loadProfile);
        Assert.True(typeof(Task).IsAssignableFrom(loadProfile!.ReturnType), "LoadProfileAsync must be asynchronous");
        Assert.True(loadProfile.ReturnType.IsGenericType, "LoadProfileAsync should return Task<T>");
        Assert.Equal("OfdrwNet.Color.ColorProfile", loadProfile.ReturnType.GetGenericArguments()[0].FullName);

        var profileParameters = loadProfile.GetParameters();
        Assert.Equal(2, profileParameters.Length);
        Assert.Equal(typeof(string), profileParameters[0].ParameterType);
        Assert.False(profileParameters[0].IsOptional, "ICC profile path should be required");
        Assert.Equal(typeof(CancellationToken), profileParameters[1].ParameterType);
        Assert.True(profileParameters[1].IsOptional || profileParameters[1].HasDefaultValue, "CancellationToken should be optional with a default value");

        var fallback = type.GetMethod("GetFallbackInfo");
        Assert.NotNull(fallback);
        Assert.Equal("OfdrwNet.Color.FallbackInfo", fallback!.ReturnType.FullName);
        Assert.Empty(fallback.GetParameters());
    }

    [Fact]
    public void ColorSpaceConverter_DataContracts_ShouldBeImmutable()
    {
        var conversionResultType = ContractReflection.FindContractType("OfdrwNet.Color.ColorConversionResult");
        Assert.NotNull(conversionResultType);
        var resultType = conversionResultType!;

        Assert.True(resultType.IsClass, "ColorConversionResult should be a class");
        Assert.False(resultType.IsAbstract, "ColorConversionResult should be instantiable");
        Assert.True(resultType.IsPublic, "ColorConversionResult must be public");
        Assert.NotNull(resultType.GetConstructor(Type.EmptyTypes));

        var instance = Activator.CreateInstance(resultType);
        Assert.NotNull(instance);

        var convertedValue = GetInstanceProperty(resultType, "ConvertedValue");
        Assert.Equal("OfdrwNet.Color.ColorValue", convertedValue.PropertyType.FullName);
        AssertInitOnly(convertedValue);
        AssertRequired(convertedValue);

        var deltaProperty = GetInstanceProperty(resultType, "DeltaE");
        Assert.Equal(typeof(double), deltaProperty.PropertyType);
        AssertInitOnly(deltaProperty);

        var fallbackFlag = GetInstanceProperty(resultType, "UsedFallback");
        Assert.Equal(typeof(bool), fallbackFlag.PropertyType);
        AssertInitOnly(fallbackFlag);

        var fallbackReason = GetInstanceProperty(resultType, "FallbackReason");
        Assert.Equal(typeof(string), fallbackReason.PropertyType);
        AssertInitOnly(fallbackReason);

        var conversionTime = GetInstanceProperty(resultType, "ConversionTime");
        Assert.Equal(typeof(TimeSpan?), conversionTime.PropertyType);
        AssertInitOnly(conversionTime);

        var fallbackInfoType = ContractReflection.FindContractType("OfdrwNet.Color.FallbackInfo");
        Assert.NotNull(fallbackInfoType);
        var infoType = fallbackInfoType!;

        Assert.True(infoType.IsClass, "FallbackInfo should be a class");
        Assert.False(infoType.IsAbstract, "FallbackInfo should be instantiable");
        Assert.True(infoType.IsPublic, "FallbackInfo must be public");
        Assert.NotNull(infoType.GetConstructor(Type.EmptyTypes));

        var infoInstance = Activator.CreateInstance(infoType);
        Assert.NotNull(infoInstance);

        var isFallbackMode = GetInstanceProperty(infoType, "IsFallbackMode");
        Assert.Equal(typeof(bool), isFallbackMode.PropertyType);
        AssertInitOnly(isFallbackMode);

        var reason = GetInstanceProperty(infoType, "Reason");
        Assert.Equal(typeof(string), reason.PropertyType);
        AssertInitOnly(reason);

        var strategy = GetInstanceProperty(infoType, "Strategy");
        Assert.Equal(typeof(string), strategy.PropertyType);
        AssertInitOnly(strategy);

        var testColorsType = ContractReflection.FindContractType("OfdrwNet.Color.TestColors");
        Assert.NotNull(testColorsType);
        Assert.True(testColorsType!.IsClass && testColorsType.IsAbstract && testColorsType.IsSealed, "TestColors should be a static class");

        var colorProperties = new[] { "Black", "White", "Red", "Gray50", "Cyan" };
        foreach (var propertyName in colorProperties)
        {
            var property = testColorsType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(property);
            Assert.Equal("OfdrwNet.Color.ColorValue", property!.PropertyType.FullName);
            Assert.True(property.CanRead, $"TestColors.{propertyName} should be readable");
            Assert.False(property.CanWrite, $"TestColors.{propertyName} should be read-only");
        }
    }

    private static void AssertInitOnly(PropertyInfo property)
    {
        var setter = property.SetMethod;
        Assert.NotNull(setter);
        var modifiers = setter!.ReturnParameter.GetRequiredCustomModifiers();
        Assert.Contains(typeof(IsExternalInit), modifiers);
    }

    private static PropertyInfo GetInstanceProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        return property!;
    }

    private static void AssertRequired(PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes(inherit: false).OfType<Attribute>();
        Assert.Contains(attributes, attr => attr.GetType().FullName == typeof(RequiredMemberAttribute).FullName);
    }
}

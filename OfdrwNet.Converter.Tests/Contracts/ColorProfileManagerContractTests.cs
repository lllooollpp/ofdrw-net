using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class ColorProfileManagerContractTests
{
    [Fact]
    public void ColorProfileManager_Interface_ShouldExposeDeltaEAndLoadContracts()
    {
        var manager = ContractReflection.FindContractType("OfdrwNet.Core.Color.IColorProfileManager");
        Assert.NotNull(manager);

        var type = manager!;
        Assert.True(type.IsInterface, "IColorProfileManager should be an interface");
        Assert.True(type.IsPublic, "IColorProfileManager must be public for DI discovery");

        var evaluate = type.GetMethod("EvaluateDeltaE");
        Assert.NotNull(evaluate);
        Assert.False(evaluate!.IsStatic, "EvaluateDeltaE should operate on instance state");
        Assert.False(evaluate.IsGenericMethodDefinition, "EvaluateDeltaE should not be generic");
        Assert.Equal("OfdrwNet.Core.Color.ColorDeltaStats", evaluate.ReturnType.FullName);

        var parameters = evaluate.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.All(parameters, p => Assert.Equal("OfdrwNet.Core.Color.ImageReference", p.ParameterType.FullName));
        Assert.All(parameters, p => Assert.False(p.IsOut || p.ParameterType.IsByRef, "ImageReference should be passed by value"));
        Assert.All(parameters, p => Assert.False(p.IsOptional, "ImageReference parameters should be mandatory"));

        var load = type.GetMethod("Load");
        Assert.NotNull(load);
        Assert.False(load!.IsStatic, "Load should operate on manager instance");
        Assert.Equal(1, load.GetParameters().Length);
        Assert.Equal(typeof(string), load.GetParameters()[0].ParameterType);
        Assert.Contains("IccProfile", load.ReturnType.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ColorDeltaStats_ShouldBeSealedWithInitOnlyProperties()
    {
        var statsType = ContractReflection.FindContractType("OfdrwNet.Core.Color.ColorDeltaStats");
        Assert.NotNull(statsType);

        var type = statsType!;
        Assert.True(type.IsClass, "ColorDeltaStats should be a class");
        Assert.True(type.IsSealed, "ColorDeltaStats should be sealed to enforce immutability");
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        AssertInitOnlyDouble(type, instance!, "Average", expected: 0d);
        AssertInitOnlyDouble(type, instance!, "Max", expected: 0d);
    }

    private static void AssertInitOnlyDouble([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, object instance, string propertyName, double expected)
    {
        var property = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(p => p.Name == propertyName);

        Assert.NotNull(property);
        Assert.Equal(typeof(double), property!.PropertyType);
        Assert.True(IsInitOnly(property), $"{propertyName} must be init-only");
        Assert.Equal(expected, property.GetValue(instance));
    }

    private static bool IsInitOnly(PropertyInfo property)
    {
        var setter = property.SetMethod;
        if (setter is null)
        {
            return false;
        }

        return setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(IsExternalInit));
    }
}

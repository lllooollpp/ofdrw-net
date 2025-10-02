using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class MemoryGuardContractTests
{
    [Fact]
    public void MemoryGuard_Interface_ShouldExposeCheckContract()
    {
        var guardType = ContractReflection.FindContractType("OfdrwNet.Converter.Batch.IMemoryGuard");
        Assert.NotNull(guardType);

        var type = guardType!;
        Assert.True(type.IsInterface, "IMemoryGuard should be an interface");
        Assert.True(type.IsPublic, "IMemoryGuard must be public for injection");

        var check = type.GetMethod("Check");
        Assert.NotNull(check);
        Assert.False(check!.IsStatic, "Check should execute on an instance");
        Assert.False(check.IsGenericMethodDefinition, "Check must not be generic");
        Assert.Equal("OfdrwNet.Converter.Batch.MemoryDecision", check.ReturnType.FullName);

        var parameters = check.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(long), parameters[0].ParameterType);
        Assert.False(parameters[0].IsOut || parameters[0].ParameterType.IsByRef, "currentBytes should be passed by value");

        Assert.Equal("OfdrwNet.Converter.Batch.MemoryGuardContext", parameters[1].ParameterType.FullName);
        Assert.False(parameters[1].IsOut || parameters[1].ParameterType.IsByRef, "MemoryGuardContext should be passed by value");
        Assert.False(parameters[1].IsOptional, "MemoryGuardContext must be required");
    }

    [Fact]
    public void MemoryGuardContext_ShouldBeSealedWithInitOnlyProperties()
    {
        var contextType = ContractReflection.FindContractType("OfdrwNet.Converter.Batch.MemoryGuardContext");
        Assert.NotNull(contextType);

        var type = contextType!;
        Assert.True(type.IsClass, "MemoryGuardContext should be a class");
        Assert.True(type.IsSealed, "MemoryGuardContext should be sealed");
        Assert.True(type.IsPublic, "MemoryGuardContext should be public");
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        AssertInitOnly(type, instance!, "ThresholdBytes", typeof(long), expected: 0L);
        AssertInitOnly(type, instance!, "CurrentPage", typeof(int), expected: 0);
    }

    [Fact]
    public void MemoryDecision_ShouldExposeExpectedMembers()
    {
        var enumType = ContractReflection.FindContractType("OfdrwNet.Converter.Batch.MemoryDecision");
        Assert.NotNull(enumType);

        var type = enumType!;
        Assert.True(type.IsEnum, "MemoryDecision should be an enum");
        Assert.True(type.IsPublic, "MemoryDecision must be public");

        var names = Enum.GetNames(type);
        Assert.Contains("Continue", names);
        Assert.Contains("Segment", names);
        Assert.Contains("Abort", names);
    }

    private static void AssertInitOnly(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        object instance,
        string propertyName,
        Type expectedType,
        object expected)
    {
        var property = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(p => p.Name == propertyName);

        Assert.NotNull(property);
        Assert.Equal(expectedType, property!.PropertyType);
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

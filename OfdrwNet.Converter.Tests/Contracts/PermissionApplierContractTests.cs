using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class PermissionApplierContractTests
{
    [Fact]
    public void PermissionApplier_ShouldExposeApplyWithExpectedSignature()
    {
        var applier = ContractReflection.FindContractType("OfdrwNet.Converter.Security.IPermissionApplier");
        Assert.NotNull(applier);
        Assert.True(applier!.IsInterface, "IPermissionApplier should be an interface");

        var apply = applier.GetMethod("Apply");
        Assert.NotNull(apply);
        Assert.Equal(typeof(void), apply!.ReturnType);

        var parameters = apply.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal("OfdrwNet.Core.Document.DocumentRoot", parameters[0].ParameterType.FullName);
        Assert.Equal("OfdrwNet.Core.Security.PermissionConfig", parameters[1].ParameterType.FullName);
        Assert.Equal("OfdrwNet.Converter.Security.IEncryptionProvider", parameters[2].ParameterType.FullName);
        Assert.True(parameters[2].ParameterType.IsInterface, "Encryption parameter should be interface to allow adapters");
    }

    [Fact]
    public void PermissionConfig_ShouldBeSealedInitOnlyWithSafeDefaults()
    {
        var configType = ContractReflection.FindContractType("OfdrwNet.Core.Security.PermissionConfig");
        Assert.NotNull(configType);

        var type = configType!;
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed, "PermissionConfig should be sealed");
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        AssertPermissionProperty(type, instance, "Print", expected: true);
        AssertPermissionProperty(type, instance, "PrintHQ", expected: true);
        AssertPermissionProperty(type, instance, "Modify", expected: true);
        AssertPermissionProperty(type, instance, "Annotate", expected: true);
        AssertPermissionProperty(type, instance, "Export", expected: true);
        AssertPermissionProperty(type, instance, "Owner", expected: true);
    }

    private static void AssertPermissionProperty([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, object instance, string propertyName, bool expected)
    {
        var property = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .SingleOrDefault(p => p.Name == propertyName);
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property!.PropertyType);
        Assert.True(IsInitOnly(property), $"{propertyName} must be init-only");
        Assert.Equal(expected, property.GetValue(instance));
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class BatchProcessorContractTests
{
    [Fact]
    public void BatchProcessor_Interface_ShouldExposeProcessSignature()
    {
        var processor = ContractReflection.FindContractType("OfdrwNet.Converter.Batch.IBatchProcessor");
        Assert.NotNull(processor);

        var type = processor!;
        Assert.True(type.IsInterface, "IBatchProcessor should be an interface");
        Assert.True(type.IsPublic, "IBatchProcessor must be public for registration");

        var method = type.GetMethod("Process");
        Assert.NotNull(method);
        Assert.False(method!.IsStatic, "Process should be an instance method");
        Assert.False(method.IsGenericMethodDefinition, "Process must not be generic");
        Assert.Equal("OfdrwNet.Core.Batch.BatchResult", method.ReturnType.FullName);

        var parameters = method.GetParameters();
        Assert.Equal(3, parameters.Length);

        Assert.Equal(typeof(IEnumerable<string>), parameters[0].ParameterType);
        Assert.False(parameters[0].IsOut || parameters[0].ParameterType.IsByRef, "pdfPaths should be passed by value");

        Assert.Equal("OfdrwNet.Converter.Options.ConverterOptions", parameters[1].ParameterType.FullName);
        Assert.False(parameters[1].IsOut || parameters[1].ParameterType.IsByRef, "ConverterOptions should be passed by value");
        Assert.False(parameters[1].IsOptional, "ConverterOptions argument must be required");

        Assert.Equal(typeof(int), parameters[2].ParameterType);
        Assert.False(parameters[2].IsOut || parameters[2].ParameterType.IsByRef, "parallelism should be passed by value");
        Assert.False(parameters[2].IsOptional, "parallelism must be explicitly provided");
    }

    [Fact]
    public void BatchResult_ShouldBeInitOnlyWithImmutableFailures()
    {
        var resultType = ContractReflection.FindContractType("OfdrwNet.Core.Batch.BatchResult");
        Assert.NotNull(resultType);

        var type = resultType!;
        Assert.True(type.IsClass, "BatchResult should be a class");
        Assert.True(type.IsSealed, "BatchResult should be sealed");
        Assert.True(type.IsPublic, "BatchResult must be public for consumers");
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        AssertInitOnly(type, instance!, "Total", typeof(int), expected: 0);
        AssertInitOnly(type, instance!, "Success", typeof(int), expected: 0);
        AssertInitOnly(type, instance!, "Failed", typeof(int), expected: 0);

        var failedFiles = type.GetProperty("FailedFiles", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(failedFiles);
        Assert.Equal(typeof(IReadOnlyList<string>), failedFiles!.PropertyType);
        Assert.True(IsInitOnly(failedFiles), "FailedFiles must be init-only");

        var value = failedFiles.GetValue(instance);
        Assert.NotNull(value);
        var failures = Assert.IsAssignableFrom<IReadOnlyList<string>>(value);
        Assert.Empty(failures);
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

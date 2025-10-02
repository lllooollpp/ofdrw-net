using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class VersionChainStoreContractTests
{
    [Fact]
    public void VersionChainStore_ShouldExposeCommitGetChainAndCompact()
    {
        var store = ContractReflection.FindContractType("OfdrwNet.Converter.Versioning.IVersionChainStore");
        Assert.NotNull(store);
        Assert.True(store!.IsInterface, "IVersionChainStore should be an interface");

        var commit = store.GetMethods().SingleOrDefault(m => m.Name == "Commit");
        Assert.NotNull(commit);
        Assert.False(commit!.IsGenericMethod, "Commit should not be generic");
        Assert.Equal("OfdrwNet.Core.Versioning.VersionEntry", commit.ReturnType.FullName);
        var commitParameters = commit.GetParameters();
        Assert.Equal(2, commitParameters.Length);
        Assert.Equal("OfdrwNet.Converter.Versioning.VersionDelta", commitParameters[0].ParameterType.FullName);
        Assert.False(commitParameters[0].IsOptional, "VersionDelta parameter must be required");
        Assert.Equal("OfdrwNet.Core.Versioning.VersionPolicy", commitParameters[1].ParameterType.FullName);
        Assert.False(commitParameters[1].IsOptional, "VersionPolicy parameter must be required");

        var getChain = store.GetMethods().SingleOrDefault(m => m.Name == "GetChain");
        Assert.NotNull(getChain);
        Assert.False(getChain!.IsGenericMethod, "GetChain should not be generic");
        Assert.True(getChain.ReturnType.IsGenericType, "GetChain return type should be generic IReadOnlyList");
        Assert.Equal("System.Collections.Generic.IReadOnlyList`1", getChain.ReturnType.GetGenericTypeDefinition().FullName);
        var chainElementType = getChain.ReturnType.GetGenericArguments().Single();
        Assert.Equal("OfdrwNet.Core.Versioning.VersionEntry", chainElementType.FullName);

        var compact = store.GetMethods().SingleOrDefault(m => m.Name == "TryCompact");
        Assert.NotNull(compact);
        Assert.False(compact!.IsGenericMethod, "TryCompact should not be generic");
        Assert.Equal("OfdrwNet.Converter.Versioning.VersionChainCompactResult", compact.ReturnType.FullName);
        var compactParameters = compact.GetParameters();
        Assert.Single(compactParameters);
        Assert.Equal("OfdrwNet.Core.Versioning.VersionPolicy", compactParameters[0].ParameterType.FullName);
    }

    [Fact]
    public void VersionDelta_ShouldBeSealedWithInitOnlyPropertiesAndDefaults()
    {
        var deltaType = ContractReflection.FindContractType("OfdrwNet.Converter.Versioning.VersionDelta");
        Assert.NotNull(deltaType);

        var type = deltaType!;
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed, "VersionDelta should be sealed");
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        var baseHash = type.GetProperty("BaseHash");
        Assert.NotNull(baseHash);
        Assert.Equal(typeof(string), baseHash!.PropertyType);
        Assert.True(IsInitOnly(baseHash), "BaseHash must be init-only");
        Assert.Equal(string.Empty, baseHash.GetValue(instance));

        var deltaSizeBytes = type.GetProperty("DeltaSizeBytes");
        Assert.NotNull(deltaSizeBytes);
        Assert.Equal(typeof(long), deltaSizeBytes!.PropertyType);
        Assert.True(IsInitOnly(deltaSizeBytes), "DeltaSizeBytes must be init-only");
        Assert.Equal(0L, deltaSizeBytes.GetValue(instance));
    }

    [Fact]
    public void VersionEntry_ShouldExposeInitOnlyPropertiesWithDefaults()
    {
        var entryType = ContractReflection.FindContractType("OfdrwNet.Core.Versioning.VersionEntry");
        Assert.NotNull(entryType);

        var type = entryType!;
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed, "VersionEntry should be sealed");
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        var versionId = type.GetProperty("VersionId");
        Assert.NotNull(versionId);
        Assert.Equal(typeof(string), versionId!.PropertyType);
        Assert.True(IsInitOnly(versionId), "VersionId must be init-only");
        Assert.Equal(string.Empty, versionId.GetValue(instance));

        var baseHash = type.GetProperty("BaseHash");
        Assert.NotNull(baseHash);
        Assert.Equal(typeof(string), baseHash!.PropertyType);
        Assert.True(IsInitOnly(baseHash), "BaseHash must be init-only");
        Assert.Equal(string.Empty, baseHash.GetValue(instance));

        var cumulativeSize = type.GetProperty("CumulativeSizeBytes");
        Assert.NotNull(cumulativeSize);
        Assert.Equal(typeof(long), cumulativeSize!.PropertyType);
        Assert.True(IsInitOnly(cumulativeSize), "CumulativeSizeBytes must be init-only");
        Assert.Equal(0L, cumulativeSize.GetValue(instance));

        var createdAt = type.GetProperty("CreatedAt");
        Assert.NotNull(createdAt);
        Assert.Equal(typeof(DateTime), createdAt!.PropertyType);
        Assert.True(IsInitOnly(createdAt), "CreatedAt must be init-only");
        Assert.Equal(default(DateTime), createdAt.GetValue(instance));
    }

    [Fact]
    public void VersionChainCompactResult_ShouldExposeInitOnlyProperties()
    {
        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Versioning.VersionChainCompactResult");
        Assert.NotNull(resultType);

        var type = resultType!;
        Assert.True(type.IsClass);
        Assert.True(type.IsSealed, "VersionChainCompactResult should be sealed");
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        var compacted = type.GetProperty("Compacted");
        Assert.NotNull(compacted);
        Assert.Equal(typeof(bool), compacted!.PropertyType);
        Assert.True(IsInitOnly(compacted), "Compacted must be init-only");
        Assert.False((bool)compacted.GetValue(instance)!);

        var newLength = type.GetProperty("NewLength");
        Assert.NotNull(newLength);
        Assert.Equal(typeof(int), newLength!.PropertyType);
        Assert.True(IsInitOnly(newLength), "NewLength must be init-only");
        Assert.Equal(0, newLength.GetValue(instance));
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

using System;
using System.Collections;
using System.Linq;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class TableRecognitionContractTests
{
    [Fact]
    public void TableStrategy_Interface_ShouldExposeRecognizeContract()
    {
        var strategy = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.ITableRecognitionStrategy");
        Assert.NotNull(strategy);

        var recognize = strategy!.GetMethods().SingleOrDefault(m => m.Name == "Recognize");
        Assert.NotNull(recognize);

        var parameters = recognize!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal("OfdrwNet.Core.Pages.PageContext", parameters[0].ParameterType.FullName);
        Assert.Equal("OfdrwNet.Converter.Recognition.TableRecognitionOptions", parameters[1].ParameterType.FullName);

        Assert.Equal("OfdrwNet.Converter.Recognition.TableRecognitionResult", recognize.ReturnType.FullName);
    }

    [Fact]
    public void TableRecognitionOptions_ShouldExposeConfidenceThresholdInitOnly()
    {
        var optionsType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableRecognitionOptions");
        Assert.NotNull(optionsType);
        Assert.True(optionsType!.IsSealed);

        var confidenceThreshold = optionsType.GetProperty("ConfidenceThreshold");
        Assert.NotNull(confidenceThreshold);
        Assert.Equal(typeof(float), confidenceThreshold!.PropertyType);

        var setter = confidenceThreshold.SetMethod;
        Assert.NotNull(setter);
        var modifiers = setter!.ReturnParameter.GetRequiredCustomModifiers();
        Assert.Contains(modifiers, modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
    }

    [Fact]
    public void TableRecognitionResult_ShouldExposeCellsConfidenceAndDefaults()
    {
        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableRecognitionResult");
        Assert.NotNull(resultType);
        Assert.True(resultType!.IsSealed);

        var success = resultType.GetProperty("Success");
        Assert.NotNull(success);
        Assert.Equal(typeof(bool), success!.PropertyType);

        var confidence = resultType.GetProperty("Confidence");
        Assert.NotNull(confidence);
        Assert.Equal(typeof(float), confidence!.PropertyType);

        var cells = resultType.GetProperty("Cells");
        Assert.NotNull(cells);
        Assert.True(cells!.PropertyType.IsGenericType);
        Assert.Equal(typeof(System.Collections.Generic.IReadOnlyList<>), cells.PropertyType.GetGenericTypeDefinition());

        var cellType = cells.PropertyType.GetGenericArguments().Single();
        Assert.Equal("OfdrwNet.Converter.Recognition.TableCell", cellType.FullName);

        var instance = Activator.CreateInstance(resultType)!;
        Assert.False((bool)success.GetValue(instance)!);
        Assert.Equal(0f, (float)confidence.GetValue(instance)!);

        var cellValue = cells.GetValue(instance);
        Assert.NotNull(cellValue);
        Assert.Empty((IEnumerable)cellValue!);
    }
}

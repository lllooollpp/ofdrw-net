using System;
using System.Linq;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

public class FormulaRecognitionContractTests
{
    [Fact]
    public void FormulaStrategy_Interface_ShouldExposeRecognizeContract()
    {
        var strategy = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.IFormulaRecognitionStrategy");
        Assert.NotNull(strategy);

        var recognize = strategy!.GetMethods().SingleOrDefault(m => m.Name == "Recognize");
        Assert.NotNull(recognize);

        var parameters = recognize!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal("OfdrwNet.Core.Pages.PageContext", parameters[0].ParameterType.FullName);
        Assert.Equal("OfdrwNet.Converter.Recognition.FormulaRecognitionOptions", parameters[1].ParameterType.FullName);

        Assert.Equal("OfdrwNet.Converter.Recognition.FormulaRecognitionResult", recognize.ReturnType.FullName);
    }

    [Fact]
    public void FormulaRecognitionOptions_ShouldExposeConfidenceThresholdInitOnly()
    {
        var optionsType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.FormulaRecognitionOptions");
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
    public void FormulaRecognitionResult_ShouldExposeConfidenceLatexAndDefaults()
    {
        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.FormulaRecognitionResult");
        Assert.NotNull(resultType);
        Assert.True(resultType!.IsSealed);

        var success = resultType.GetProperty("Success");
        Assert.NotNull(success);
        Assert.Equal(typeof(bool), success!.PropertyType);

        var confidence = resultType.GetProperty("Confidence");
        Assert.NotNull(confidence);
        Assert.Equal(typeof(float), confidence!.PropertyType);

        var latex = resultType.GetProperty("LaTeX");
        Assert.NotNull(latex);
        Assert.Equal(typeof(string), latex!.PropertyType);

        var instance = Activator.CreateInstance(resultType)!;
        Assert.False((bool)success.GetValue(instance)!);
        Assert.Equal(0f, (float)confidence.GetValue(instance)!);
        Assert.Null(latex.GetValue(instance));
    }
}

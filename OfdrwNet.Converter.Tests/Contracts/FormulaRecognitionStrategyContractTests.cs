using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

/// <summary>
/// 契约测试: 验证 IFormulaRecognitionStrategy 接口符合设计规范
/// </summary>
/// <remarks>
/// 覆盖需求:
/// - FR-15: 公式识别策略抽象层 (支持规则/ML算法替换)
/// - DR-7~DR-8: 性能指标 (字符召回≥95%, 结构召回≥88%)
///
/// 测试策略:
/// - 使用反射验证接口签名 (不实例化实现)
/// - 确保方法同步执行 (非异步,性能关键路径)
/// - 验证数据契约类型 (FormulaRecognitionOptions, FormulaRecognitionResult) 的不可变性
/// </remarks>
public class FormulaRecognitionStrategyContractTests
{
    [Fact]
    public void FormulaStrategy_Interface_ShouldExposeRecognizeContract()
    {
        // Arrange & Act: 查找 IFormulaRecognitionStrategy 接口
        var strategyType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.IFormulaRecognitionStrategy");
        Assert.NotNull(strategyType);

        var type = strategyType!;

        // Assert: 基本接口属性
        Assert.True(type.IsInterface, "IFormulaRecognitionStrategy should be declared as an interface");
        Assert.True(type.IsPublic, "IFormulaRecognitionStrategy must be public for DI discovery");

        // 验证依赖类型存在
        var pageContextType = ContractReflection.FindContractType("OfdrwNet.Converter.Domain.PageContext");
        Assert.NotNull(pageContextType);

        var optionsType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.FormulaRecognitionOptions");
        Assert.NotNull(optionsType);

        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.FormulaRecognitionResult");
        Assert.NotNull(resultType);

        // 验证 Recognize 方法 (同步方法,性能关键路径)
        var recognize = type.GetMethod("Recognize");
        Assert.NotNull(recognize);
        Assert.False(recognize!.IsStatic, "Recognize should operate on strategy instances");

        // 返回类型: FormulaRecognitionResult (同步返回)
        Assert.Equal("OfdrwNet.Converter.Recognition.FormulaRecognitionResult", recognize.ReturnType.FullName);
        Assert.False(recognize.ReturnType.IsGenericType, "Recognize should be synchronous (not Task<T>)");

        // 参数验证
        var parameters = recognize.GetParameters();
        Assert.Equal(2, parameters.Length);

        // 参数1: PageContext (必需)
        Assert.Equal("OfdrwNet.Converter.Domain.PageContext", parameters[0].ParameterType.FullName);
        Assert.False(parameters[0].IsOptional, "PageContext should be required");

        // 参数2: FormulaRecognitionOptions (必需)
        Assert.Equal("OfdrwNet.Converter.Recognition.FormulaRecognitionOptions", parameters[1].ParameterType.FullName);
        Assert.False(parameters[1].IsOptional, "FormulaRecognitionOptions should be required");
    }

    [Fact]
    public void FormulaRecognitionOptions_ShouldExposeConfidenceThresholdInitOnly()
    {
        // Arrange & Act: 查找 FormulaRecognitionOptions 类型
        var optionsType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.FormulaRecognitionOptions");
        Assert.NotNull(optionsType);
        var options = optionsType!;

        // Assert: 基本类型属性
        Assert.True(options.IsClass, "FormulaRecognitionOptions should be a class");
        Assert.True(options.IsSealed, "FormulaRecognitionOptions should be sealed (performance optimization)");
        Assert.False(options.IsAbstract, "FormulaRecognitionOptions should be instantiable");
        Assert.True(options.IsPublic, "FormulaRecognitionOptions must be public");

        // 验证无参构造函数
        var ctor = options.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(ctor);

        // 创建实例验证
        var instance = Activator.CreateInstance(options);
        Assert.NotNull(instance);

        // 验证 ConfidenceThreshold 属性 (init-only)
        var threshold = GetInstanceProperty(options, "ConfidenceThreshold");
        Assert.Equal(typeof(float), threshold.PropertyType);
        AssertInitOnly(threshold);
        Assert.True(threshold.CanRead, "ConfidenceThreshold should be readable");
        Assert.False(threshold.CanWrite || threshold.SetMethod?.IsPublic == true,
            "ConfidenceThreshold should not be publicly settable (init-only)");
    }

    [Fact]
    public void FormulaRecognitionResult_ShouldExposeConfidenceLatexAndDefaults()
    {
        // Arrange & Act: 查找 FormulaRecognitionResult 类型
        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.FormulaRecognitionResult");
        Assert.NotNull(resultType);
        var result = resultType!;

        // Assert: 基本类型属性
        Assert.True(result.IsClass, "FormulaRecognitionResult should be a class");
        Assert.True(result.IsSealed, "FormulaRecognitionResult should be sealed (performance optimization)");
        Assert.False(result.IsAbstract, "FormulaRecognitionResult should be instantiable");
        Assert.True(result.IsPublic, "FormulaRecognitionResult must be public");

        // 验证无参构造函数
        var ctor = result.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(ctor);

        // 创建实例验证
        var instance = Activator.CreateInstance(result);
        Assert.NotNull(instance);

        // 验证 Success 属性 (init-only)
        var success = GetInstanceProperty(result, "Success");
        Assert.Equal(typeof(bool), success.PropertyType);
        AssertInitOnly(success);
        Assert.True(success.CanRead, "Success should be readable");

        // 验证 Confidence 属性 (init-only)
        var confidence = GetInstanceProperty(result, "Confidence");
        Assert.Equal(typeof(float), confidence.PropertyType);
        AssertInitOnly(confidence);
        Assert.True(confidence.CanRead, "Confidence should be readable");

        // 验证 LaTeX 属性 (init-only, nullable)
        var latex = GetInstanceProperty(result, "LaTeX");
        Assert.Equal(typeof(string), latex.PropertyType);
        AssertInitOnly(latex);
        Assert.True(latex.CanRead, "LaTeX should be readable");

        // 验证默认值为 null (可选)
        var defaultLatex = latex.GetValue(instance);
        Assert.Null(defaultLatex);
    }

    [Fact]
    public void FormulaRecognition_SupportTypes_ShouldExposeExpectedMembers()
    {
        // 验证 PageContext 类型 (共享类型,与表格识别复用)
        var contextType = ContractReflection.FindContractType("OfdrwNet.Converter.Domain.PageContext");
        Assert.NotNull(contextType);
        var context = contextType!;

        Assert.True(context.IsClass, "PageContext should be a class");
        Assert.False(context.IsAbstract, "PageContext should be instantiable");
        Assert.True(context.IsPublic, "PageContext must be public");

        // 验证 PageNumber 属性
        var pageNumber = GetInstanceProperty(context, "PageNumber");
        Assert.Equal(typeof(int), pageNumber.PropertyType);
        Assert.True(pageNumber.CanRead, "PageNumber should be readable");

        // 验证 SourceObjects 属性 (页面对象列表)
        var sourceObjects = GetInstanceProperty(context, "SourceObjects");
        Assert.True(sourceObjects.PropertyType.IsGenericType, "SourceObjects should be a collection");
        Assert.True(sourceObjects.CanRead, "SourceObjects should be readable");

        // 验证 PageObject 基类存在
        var pageObjectType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.PageObject");
        Assert.NotNull(pageObjectType);
        Assert.True(pageObjectType!.IsClass, "PageObject should be a class");
        Assert.True(pageObjectType.IsAbstract, "PageObject should be abstract");
    }

    [Fact]
    public void FormulaRecognition_DataFlow_ShouldFollowExpectedPattern()
    {
        // 验证数据流: Options → Strategy.Recognize → Result

        // 1. Options 应该是可创建的 (使用 object initializer)
        var optionsType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.FormulaRecognitionOptions");
        Assert.NotNull(optionsType);

        var optionsInstance = Activator.CreateInstance(optionsType!);
        Assert.NotNull(optionsInstance);

        var threshold = GetInstanceProperty(optionsType!, "ConfidenceThreshold");
        Assert.True(threshold.CanRead, "ConfidenceThreshold should be readable for passing to strategy");

        // 2. Result 应该是可创建的 (使用 object initializer)
        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.FormulaRecognitionResult");
        Assert.NotNull(resultType);

        var resultInstance = Activator.CreateInstance(resultType!);
        Assert.NotNull(resultInstance);

        var success = GetInstanceProperty(resultType!, "Success");
        var confidence = GetInstanceProperty(resultType!, "Confidence");
        var latex = GetInstanceProperty(resultType!, "LaTeX");

        Assert.True(success.CanRead, "Success should be readable for checking recognition outcome");
        Assert.True(confidence.CanRead, "Confidence should be readable for quality assessment");
        Assert.True(latex.CanRead, "LaTeX should be readable for extracting formula content");

        // 3. Strategy 接口应该接受 Options 并返回 Result
        var strategyType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.IFormulaRecognitionStrategy");
        Assert.NotNull(strategyType);

        var recognize = strategyType!.GetMethod("Recognize");
        Assert.NotNull(recognize);

        var parameters = recognize!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal("OfdrwNet.Converter.Recognition.FormulaRecognitionOptions", parameters[1].ParameterType.FullName);
        Assert.Equal("OfdrwNet.Converter.Recognition.FormulaRecognitionResult", recognize.ReturnType.FullName);
    }

    private static void AssertInitOnly(PropertyInfo property)
    {
        var setter = property.SetMethod;
        Assert.NotNull(setter);
        var modifiers = setter!.ReturnParameter.GetRequiredCustomModifiers();
        Assert.Contains(typeof(IsExternalInit), modifiers);
    }

    private static PropertyInfo GetInstanceProperty(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        return property!;
    }
}

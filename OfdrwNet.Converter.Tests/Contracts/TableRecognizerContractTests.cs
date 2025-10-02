using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

/// <summary>
/// 契约测试: 验证 ITableRecognizer 接口符合设计规范
/// </summary>
/// <remarks>
/// 覆盖需求:
/// - FR-15: 表格识别能力
/// - DR-1~DR-6: 性能指标 (召回率≥92%, 精度≥90%, IOU≥0.85, <500ms/页)
///
/// 测试策略:
/// - 使用反射验证接口签名 (不实例化实现)
/// - 确保方法返回类型、参数类型、可选性符合契约
/// - 验证数据契约类型 (TableRecognitionResult, TableCell, BoundingBox) 的不可变性
/// </remarks>
public class TableRecognizerContractTests
{
    [Fact]
    public void TableRecognizer_Interface_ShouldExposeRecognitionContract()
    {
        // Arrange & Act: 查找 ITableRecognizer 接口
        var recognizerType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.ITableRecognizer");
        Assert.NotNull(recognizerType);

        var type = recognizerType!;

        // Assert: 基本接口属性
        Assert.True(type.IsInterface, "ITableRecognizer should be declared as an interface");
        Assert.True(type.IsPublic, "ITableRecognizer must be public for DI discovery");

        // 验证依赖类型存在
        var pageObjectType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.PageObject");
        Assert.NotNull(pageObjectType);

        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableRecognitionResult");
        Assert.NotNull(resultType);

        var boundingBoxType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.BoundingBox");
        Assert.NotNull(boundingBoxType);

        // 验证 RecognizeTablesAsync 方法
        var recognizeAsync = type.GetMethod("RecognizeTablesAsync");
        Assert.NotNull(recognizeAsync);
        Assert.False(recognizeAsync!.IsStatic, "RecognizeTablesAsync should operate on recognizer instances");
        Assert.True(typeof(Task).IsAssignableFrom(recognizeAsync.ReturnType), "RecognizeTablesAsync must be asynchronous");
        Assert.True(recognizeAsync.ReturnType.IsGenericType, "RecognizeTablesAsync should return Task<T>");

        var returnGenericArg = recognizeAsync.ReturnType.GetGenericArguments()[0];
        Assert.True(returnGenericArg.IsGenericType, "Should return Task<List<T>>");
        Assert.Equal(typeof(List<>), returnGenericArg.GetGenericTypeDefinition());
        Assert.Equal("OfdrwNet.Converter.Recognition.TableRecognitionResult",
            returnGenericArg.GetGenericArguments()[0].FullName);

        var parameters = recognizeAsync.GetParameters();
        Assert.Equal(3, parameters.Length);

        // 参数1: List<PageObject>
        Assert.True(parameters[0].ParameterType.IsGenericType, "First parameter should be List<PageObject>");
        Assert.Equal(typeof(List<>), parameters[0].ParameterType.GetGenericTypeDefinition());
        Assert.Equal("OfdrwNet.Converter.Recognition.PageObject",
            parameters[0].ParameterType.GetGenericArguments()[0].FullName);
        Assert.False(parameters[0].IsOptional, "pageObjects should be required");

        // 参数2: float threshold (可选,默认0.8)
        Assert.Equal(typeof(float), parameters[1].ParameterType);
        Assert.True(parameters[1].IsOptional || parameters[1].HasDefaultValue, "threshold should be optional");
        if (parameters[1].HasDefaultValue)
        {
            Assert.Equal(0.8f, parameters[1].DefaultValue);
        }

        // 参数3: CancellationToken (可选)
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        Assert.True(parameters[2].IsOptional || parameters[2].HasDefaultValue,
            "CancellationToken should be optional with a default value");
    }

    [Fact]
    public void TableRecognizer_ShouldExposeFallbackAndMetricsContracts()
    {
        // Arrange & Act: 查找接口
        var recognizerType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.ITableRecognizer");
        Assert.NotNull(recognizerType);
        var type = recognizerType!;

        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableRecognitionResult");
        Assert.NotNull(resultType);

        var pageObjectType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.PageObject");
        Assert.NotNull(pageObjectType);

        var boundingBoxType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.BoundingBox");
        Assert.NotNull(boundingBoxType);

        // 验证 FallbackToStaticDrawing 方法
        var fallback = type.GetMethod("FallbackToStaticDrawing");
        Assert.NotNull(fallback);
        Assert.False(fallback!.IsStatic, "FallbackToStaticDrawing should be an instance method");

        // 返回类型: List<PageObject>
        Assert.True(fallback.ReturnType.IsGenericType, "Should return List<PageObject>");
        Assert.Equal(typeof(List<>), fallback.ReturnType.GetGenericTypeDefinition());
        Assert.Equal("OfdrwNet.Converter.Recognition.PageObject",
            fallback.ReturnType.GetGenericArguments()[0].FullName);

        var fallbackParams = fallback.GetParameters();
        Assert.Single(fallbackParams);
        Assert.Equal("OfdrwNet.Converter.Recognition.TableRecognitionResult",
            fallbackParams[0].ParameterType.FullName);
        Assert.False(fallbackParams[0].IsOptional, "table parameter should be required");

        // 验证 EstimateIou 方法
        var iou = type.GetMethod("EstimateIou");
        Assert.NotNull(iou);
        Assert.Equal(typeof(double), iou!.ReturnType);

        var iouParams = iou.GetParameters();
        Assert.Equal(2, iouParams.Length);
        Assert.All(iouParams, p => Assert.Equal("OfdrwNet.Converter.Recognition.BoundingBox", p.ParameterType.FullName));
        Assert.All(iouParams, p => Assert.False(p.IsOptional, "BoundingBox parameters should be mandatory"));

        // 验证 EstimateGridRegularity 方法
        var regularity = type.GetMethod("EstimateGridRegularity");
        Assert.NotNull(regularity);
        Assert.Equal(typeof(double), regularity!.ReturnType);

        var regularityParams = regularity.GetParameters();
        Assert.Single(regularityParams);
        Assert.True(regularityParams[0].ParameterType.IsGenericType, "Should accept List<TableCell>");
        Assert.Equal(typeof(List<>), regularityParams[0].ParameterType.GetGenericTypeDefinition());

        var cellType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableCell");
        Assert.NotNull(cellType);
        Assert.Equal("OfdrwNet.Converter.Recognition.TableCell",
            regularityParams[0].ParameterType.GetGenericArguments()[0].FullName);
    }

    [Fact]
    public void TableRecognizer_DataContracts_ShouldBeImmutable()
    {
        // 验证 TableRecognitionResult 类型
        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableRecognitionResult");
        Assert.NotNull(resultType);
        var result = resultType!;

        Assert.True(result.IsClass, "TableRecognitionResult should be a class");
        Assert.False(result.IsAbstract, "TableRecognitionResult should be instantiable");
        Assert.True(result.IsPublic, "TableRecognitionResult must be public");

        // 验证构造函数
        var ctor = result.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(ctor);

        // 创建实例验证
        var instance = Activator.CreateInstance(result);
        Assert.NotNull(instance);

        // 验证必需属性: Cells (required, init-only)
        var cells = GetInstanceProperty(result, "Cells");
        Assert.True(cells.PropertyType.IsGenericType, "Cells should be List<TableCell>");
        Assert.Equal(typeof(List<>), cells.PropertyType.GetGenericTypeDefinition());
        Assert.Equal("OfdrwNet.Converter.Recognition.TableCell",
            cells.PropertyType.GetGenericArguments()[0].FullName);
        AssertInitOnly(cells);
        AssertRequired(cells);

        // 验证属性: Confidence (init-only)
        var confidence = GetInstanceProperty(result, "Confidence");
        Assert.Equal(typeof(double), confidence.PropertyType);
        AssertInitOnly(confidence);

        // 验证属性: IsFallback (init-only)
        var isFallback = GetInstanceProperty(result, "IsFallback");
        Assert.Equal(typeof(bool), isFallback.PropertyType);
        AssertInitOnly(isFallback);

        // 验证属性: ProcessingTime (init-only, nullable)
        var processingTime = GetInstanceProperty(result, "ProcessingTime");
        Assert.Equal(typeof(TimeSpan?), processingTime.PropertyType);
        AssertInitOnly(processingTime);

        // 验证 TableCell 类型
        var cellType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableCell");
        Assert.NotNull(cellType);
        var cell = cellType!;

        Assert.True(cell.IsClass, "TableCell should be a class");
        Assert.False(cell.IsAbstract, "TableCell should be instantiable");
        Assert.True(cell.IsPublic, "TableCell must be public");
        Assert.NotNull(cell.GetConstructor(Type.EmptyTypes));

        var cellInstance = Activator.CreateInstance(cell);
        Assert.NotNull(cellInstance);

        // 验证 TableCell 属性: Row, Column, RowSpan, ColumnSpan
        var row = GetInstanceProperty(cell, "Row");
        Assert.Equal(typeof(int), row.PropertyType);
        AssertInitOnly(row);

        var column = GetInstanceProperty(cell, "Column");
        Assert.Equal(typeof(int), column.PropertyType);
        AssertInitOnly(column);

        var rowSpan = GetInstanceProperty(cell, "RowSpan");
        Assert.Equal(typeof(int), rowSpan.PropertyType);
        AssertInitOnly(rowSpan);

        var columnSpan = GetInstanceProperty(cell, "ColumnSpan");
        Assert.Equal(typeof(int), columnSpan.PropertyType);
        AssertInitOnly(columnSpan);

        // 验证 TableCell.Bounds (required, init-only)
        var bounds = GetInstanceProperty(cell, "Bounds");
        Assert.Equal("OfdrwNet.Converter.Recognition.BoundingBox", bounds.PropertyType.FullName);
        AssertInitOnly(bounds);
        AssertRequired(bounds);

        // 验证 TableCell.Content (可选, init-only)
        var content = GetInstanceProperty(cell, "Content");
        Assert.Equal(typeof(string), content.PropertyType);
        AssertInitOnly(content);

        // 验证 BoundingBox 类型
        var boundingBoxType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.BoundingBox");
        Assert.NotNull(boundingBoxType);
        var bbox = boundingBoxType!;

        Assert.True(bbox.IsClass || bbox.IsValueType, "BoundingBox should be a class or struct");
        Assert.True(bbox.IsPublic, "BoundingBox must be public");

        // 验证 BoundingBox 属性: X, Y, Width, Height
        var x = GetInstanceProperty(bbox, "X");
        Assert.True(x.PropertyType == typeof(double) || x.PropertyType == typeof(float),
            "X should be double or float");
        AssertInitOnly(x);

        var y = GetInstanceProperty(bbox, "Y");
        Assert.True(y.PropertyType == typeof(double) || y.PropertyType == typeof(float),
            "Y should be double or float");
        AssertInitOnly(y);

        var width = GetInstanceProperty(bbox, "Width");
        Assert.True(width.PropertyType == typeof(double) || width.PropertyType == typeof(float),
            "Width should be double or float");
        AssertInitOnly(width);

        var height = GetInstanceProperty(bbox, "Height");
        Assert.True(height.PropertyType == typeof(double) || height.PropertyType == typeof(float),
            "Height should be double or float");
        AssertInitOnly(height);
    }

    [Fact]
    public void TableRecognizer_SupportTypes_ShouldExposeExpectedMembers()
    {
        // 验证 PageObject 抽象类
        var pageObjectType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.PageObject");
        Assert.NotNull(pageObjectType);
        var pageObject = pageObjectType!;

        Assert.True(pageObject.IsClass, "PageObject should be a class");
        Assert.True(pageObject.IsAbstract, "PageObject should be abstract");
        Assert.True(pageObject.IsPublic, "PageObject must be public");

        // 验证 Type 属性 (抽象)
        var typeProperty = GetInstanceProperty(pageObject, "Type");
        var pageObjectTypeEnum = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.PageObjectType");
        Assert.NotNull(pageObjectTypeEnum);
        Assert.Equal(pageObjectTypeEnum!.FullName, typeProperty.PropertyType.FullName);
        Assert.True(typeProperty.GetMethod!.IsAbstract, "Type property should be abstract");

        // 验证 Bounds 属性
        var bounds = GetInstanceProperty(pageObject, "Bounds");
        Assert.Equal("OfdrwNet.Converter.Recognition.BoundingBox", bounds.PropertyType.FullName);
        Assert.True(bounds.CanWrite, "Bounds should be settable");

        // 验证 ZOrder 属性
        var zOrder = GetInstanceProperty(pageObject, "ZOrder");
        Assert.Equal(typeof(int), zOrder.PropertyType);
        Assert.True(zOrder.CanWrite, "ZOrder should be settable");

        // 验证 PageObjectType 枚举
        Assert.True(pageObjectTypeEnum.IsEnum, "PageObjectType should be an enum");
        Assert.True(pageObjectTypeEnum.IsPublic, "PageObjectType must be public");

        var enumValues = Enum.GetNames(pageObjectTypeEnum);
        Assert.Contains("Text", enumValues);
        Assert.Contains("Path", enumValues);
        Assert.Contains("Image", enumValues);

        // 验证 TextObject 类 (派生自 PageObject)
        var textObjectType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TextObject");
        Assert.NotNull(textObjectType);
        var textObject = textObjectType!;

        Assert.True(textObject.IsClass, "TextObject should be a class");
        Assert.False(textObject.IsAbstract, "TextObject should be instantiable");
        Assert.True(textObject.IsSubclassOf(pageObject), "TextObject should inherit from PageObject");

        // 验证 TextObject 属性
        var content = GetInstanceProperty(textObject, "Content");
        Assert.Equal(typeof(string), content.PropertyType);

        var fontName = GetInstanceProperty(textObject, "FontName");
        Assert.Equal(typeof(string), fontName.PropertyType);

        var fontSize = GetInstanceProperty(textObject, "FontSize");
        Assert.Equal(typeof(double), fontSize.PropertyType);

        // 验证 PathObject 类
        var pathObjectType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.PathObject");
        Assert.NotNull(pathObjectType);
        var pathObject = pathObjectType!;

        Assert.True(pathObject.IsClass, "PathObject should be a class");
        Assert.False(pathObject.IsAbstract, "PathObject should be instantiable");
        Assert.True(pathObject.IsSubclassOf(pageObject), "PathObject should inherit from PageObject");

        // 验证 PathObject 属性
        var pathData = GetInstanceProperty(pathObject, "PathData");
        Assert.Equal(typeof(string), pathData.PropertyType);

        var isStraightLine = GetInstanceProperty(pathObject, "IsStraightLine");
        Assert.Equal(typeof(bool), isStraightLine.PropertyType);

        var direction = GetInstanceProperty(pathObject, "Direction");
        var lineDirectionType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.LineDirection");
        Assert.NotNull(lineDirectionType);
        Assert.True(direction.PropertyType.IsGenericType, "Direction should be Nullable<LineDirection>");
        Assert.Equal(typeof(Nullable<>), direction.PropertyType.GetGenericTypeDefinition());
        Assert.Equal(lineDirectionType!.FullName, direction.PropertyType.GetGenericArguments()[0].FullName);

        // 验证 LineDirection 枚举
        Assert.True(lineDirectionType.IsEnum, "LineDirection should be an enum");
        Assert.True(lineDirectionType.IsPublic, "LineDirection must be public");

        var lineDirectionValues = Enum.GetNames(lineDirectionType);
        Assert.Contains("Horizontal", lineDirectionValues);
        Assert.Contains("Vertical", lineDirectionValues);
        Assert.Contains("Diagonal", lineDirectionValues);

        // 验证 TableTestHelper 静态类
        var helperType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableTestHelper");
        Assert.NotNull(helperType);
        var helper = helperType!;

        Assert.True(helper.IsClass && helper.IsAbstract && helper.IsSealed,
            "TableTestHelper should be a static class");
        Assert.True(helper.IsPublic, "TableTestHelper must be public");

        // 验证 Generate3x3Table 方法
        var generate3x3 = helper.GetMethod("Generate3x3Table", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(generate3x3);
        Assert.True(generate3x3!.ReturnType.IsGenericType, "Should return List<PageObject>");
        Assert.Equal(typeof(List<>), generate3x3.ReturnType.GetGenericTypeDefinition());
        Assert.Equal("OfdrwNet.Converter.Recognition.PageObject",
            generate3x3.ReturnType.GetGenericArguments()[0].FullName);

        var gen3x3Params = generate3x3.GetParameters();
        Assert.Equal(4, gen3x3Params.Length);
        Assert.All(gen3x3Params, p => Assert.Equal(typeof(double), p.ParameterType));

        // 验证 GenerateIrregularTable 方法
        var generateIrregular = helper.GetMethod("GenerateIrregularTable", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(generateIrregular);
        Assert.True(generateIrregular!.ReturnType.IsGenericType, "Should return List<PageObject>");
        Assert.Equal(typeof(List<>), generateIrregular.ReturnType.GetGenericTypeDefinition());
        Assert.Equal("OfdrwNet.Converter.Recognition.PageObject",
            generateIrregular.ReturnType.GetGenericArguments()[0].FullName);
        Assert.Empty(generateIrregular.GetParameters());
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

    private static void AssertRequired(PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes(inherit: false).OfType<Attribute>();
        Assert.Contains(attributes, attr => attr.GetType().FullName == typeof(RequiredMemberAttribute).FullName);
    }
}

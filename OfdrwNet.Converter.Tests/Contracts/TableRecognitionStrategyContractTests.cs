using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace OfdrwNet.Converter.Tests.Contracts;

/// <summary>
/// 契约测试: 验证 ITableRecognitionStrategy 接口符合设计规范
/// </summary>
/// <remarks>
/// 覆盖需求:
/// - FR-15: 表格识别策略抽象层 (支持规则/ML算法替换)
/// - DR-1~DR-6: 性能指标要求
///
/// 测试策略:
/// - 使用反射验证接口签名 (不实例化实现)
/// - 确保方法同步执行 (非异步,性能关键路径)
/// - 验证数据契约类型 (TableRecognitionOptions, TableRecognitionResult) 的不可变性
/// </remarks>
public class TableRecognitionStrategyContractTests
{
    [Fact]
    public void TableStrategy_Interface_ShouldExposeRecognizeContract()
    {
        // Arrange & Act: 查找 ITableRecognitionStrategy 接口
        var strategyType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.ITableRecognitionStrategy");
        Assert.NotNull(strategyType);

        var type = strategyType!;

        // Assert: 基本接口属性
        Assert.True(type.IsInterface, "ITableRecognitionStrategy should be declared as an interface");
        Assert.True(type.IsPublic, "ITableRecognitionStrategy must be public for DI discovery");

        // 验证依赖类型存在
        var pageContextType = ContractReflection.FindContractType("OfdrwNet.Converter.Domain.PageContext");
        Assert.NotNull(pageContextType);

        var optionsType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableRecognitionOptions");
        Assert.NotNull(optionsType);

        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableRecognitionResult");
        Assert.NotNull(resultType);

        // 验证 Recognize 方法 (同步方法,性能关键路径)
        var recognize = type.GetMethod("Recognize");
        Assert.NotNull(recognize);
        Assert.False(recognize!.IsStatic, "Recognize should operate on strategy instances");

        // 返回类型: TableRecognitionResult (同步返回)
        Assert.Equal("OfdrwNet.Converter.Recognition.TableRecognitionResult", recognize.ReturnType.FullName);
        Assert.False(recognize.ReturnType.IsGenericType, "Recognize should be synchronous (not Task<T>)");

        // 参数验证
        var parameters = recognize.GetParameters();
        Assert.Equal(2, parameters.Length);

        // 参数1: PageContext (必需)
        Assert.Equal("OfdrwNet.Converter.Domain.PageContext", parameters[0].ParameterType.FullName);
        Assert.False(parameters[0].IsOptional, "PageContext should be required");

        // 参数2: TableRecognitionOptions (必需)
        Assert.Equal("OfdrwNet.Converter.Recognition.TableRecognitionOptions", parameters[1].ParameterType.FullName);
        Assert.False(parameters[1].IsOptional, "TableRecognitionOptions should be required");
    }

    [Fact]
    public void TableRecognitionOptions_ShouldExposeConfidenceThresholdInitOnly()
    {
        // Arrange & Act: 查找 TableRecognitionOptions 类型
        var optionsType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableRecognitionOptions");
        Assert.NotNull(optionsType);
        var options = optionsType!;

        // Assert: 基本类型属性
        Assert.True(options.IsClass, "TableRecognitionOptions should be a class");
        Assert.True(options.IsSealed, "TableRecognitionOptions should be sealed (performance optimization)");
        Assert.False(options.IsAbstract, "TableRecognitionOptions should be instantiable");
        Assert.True(options.IsPublic, "TableRecognitionOptions must be public");

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
    public void TableRecognitionResult_ShouldExposeSuccessConfidenceAndCells()
    {
        // Arrange & Act: 查找 TableRecognitionResult 类型
        var resultType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableRecognitionResult");
        Assert.NotNull(resultType);
        var result = resultType!;

        // Assert: 基本类型属性
        Assert.True(result.IsClass, "TableRecognitionResult should be a class");
        Assert.True(result.IsSealed, "TableRecognitionResult should be sealed (performance optimization)");
        Assert.False(result.IsAbstract, "TableRecognitionResult should be instantiable");
        Assert.True(result.IsPublic, "TableRecognitionResult must be public");

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

        // 验证 Cells 属性 (init-only, 默认空数组)
        var cells = GetInstanceProperty(result, "Cells");
        Assert.True(cells.PropertyType.IsGenericType, "Cells should be IReadOnlyList<TableCell>");

        var cellsGenericDef = cells.PropertyType.GetGenericTypeDefinition();
        Assert.True(cellsGenericDef == typeof(IReadOnlyList<>) || cellsGenericDef.GetInterfaces().Contains(typeof(IReadOnlyList<>)),
            "Cells should implement IReadOnlyList<T>");

        var cellType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableCell");
        Assert.NotNull(cellType);
        Assert.Equal("OfdrwNet.Converter.Recognition.TableCell",
            cells.PropertyType.GetGenericArguments()[0].FullName);

        AssertInitOnly(cells);
        Assert.True(cells.CanRead, "Cells should be readable");

        // 验证默认值为空集合
        var defaultCells = cells.GetValue(instance);
        Assert.NotNull(defaultCells);

        var enumerable = defaultCells as System.Collections.IEnumerable;
        Assert.NotNull(enumerable);
        var count = 0;
        foreach (var _ in enumerable!) count++;
        Assert.Equal(0, count);
    }

    [Fact]
    public void TableCell_ShouldExposeRowColumnSpanAndBounds()
    {
        // Arrange & Act: 查找 TableCell 类型
        var cellType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.TableCell");
        Assert.NotNull(cellType);
        var cell = cellType!;

        // Assert: 基本类型属性
        Assert.True(cell.IsClass, "TableCell should be a class");
        Assert.False(cell.IsAbstract, "TableCell should be instantiable");
        Assert.True(cell.IsPublic, "TableCell must be public");

        // 验证无参构造函数
        var ctor = cell.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(ctor);

        // 创建实例验证
        var instance = Activator.CreateInstance(cell);
        Assert.NotNull(instance);

        // 验证 Row 属性 (init-only)
        var row = GetInstanceProperty(cell, "Row");
        Assert.Equal(typeof(int), row.PropertyType);
        AssertInitOnly(row);
        Assert.True(row.CanRead, "Row should be readable");

        // 验证 Column 属性 (init-only)
        var column = GetInstanceProperty(cell, "Column");
        Assert.Equal(typeof(int), column.PropertyType);
        AssertInitOnly(column);
        Assert.True(column.CanRead, "Column should be readable");

        // 验证 RowSpan 属性 (init-only)
        var rowSpan = GetInstanceProperty(cell, "RowSpan");
        Assert.Equal(typeof(int), rowSpan.PropertyType);
        AssertInitOnly(rowSpan);
        Assert.True(rowSpan.CanRead, "RowSpan should be readable");

        // 验证 ColumnSpan 属性 (init-only)
        var columnSpan = GetInstanceProperty(cell, "ColumnSpan");
        Assert.Equal(typeof(int), columnSpan.PropertyType);
        AssertInitOnly(columnSpan);
        Assert.True(columnSpan.CanRead, "ColumnSpan should be readable");

        // 验证 Bounds 属性 (required, init-only)
        var bounds = GetInstanceProperty(cell, "Bounds");
        Assert.Equal("OfdrwNet.Converter.Recognition.BoundingBox", bounds.PropertyType.FullName);
        AssertInitOnly(bounds);
        AssertRequired(bounds);
        Assert.True(bounds.CanRead, "Bounds should be readable");

        // 验证 Content 属性 (可选, init-only)
        var content = GetInstanceProperty(cell, "Content");
        Assert.Equal(typeof(string), content.PropertyType);
        AssertInitOnly(content);
        Assert.True(content.CanRead, "Content should be readable");
    }

    [Fact]
    public void PageContext_ShouldExposePageNumberAndSourceObjects()
    {
        // Arrange & Act: 查找 PageContext 类型
        var contextType = ContractReflection.FindContractType("OfdrwNet.Converter.Domain.PageContext");
        Assert.NotNull(contextType);
        var context = contextType!;

        // Assert: 基本类型属性
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

        var pageObjectType = ContractReflection.FindContractType("OfdrwNet.Converter.Recognition.PageObject");
        Assert.NotNull(pageObjectType);

        // 接受 List<PageObject> 或 IReadOnlyList<PageObject>
        var objectsGenericArg = sourceObjects.PropertyType.GetGenericArguments().FirstOrDefault();
        Assert.NotNull(objectsGenericArg);
        Assert.Equal("OfdrwNet.Converter.Recognition.PageObject", objectsGenericArg!.FullName);
        Assert.True(sourceObjects.CanRead, "SourceObjects should be readable");
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

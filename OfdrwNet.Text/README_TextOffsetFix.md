# 文字向下偏移修复指南

## 问题描述
OFD 文档中的文本在渲染时整体向下偏移，导致文本位置不准确。

## 修复内容

### 1. 主要修复
- **GdiTextRenderer.cs**: 添加了基线校正逻辑，使用字体度量信息计算正确的基线偏移
- **SkiaTextRenderer.cs**: 修复了 Skia 渲染中的基线对齐问题
- **新增配置系统**: 可以动态调整偏移参数

### 2. 修复原理
- **GDI 修复**: 减去基线偏移量，向上调整文本位置
- **Skia 修复**: 加上基线偏移量，向下调整到正确的基线位置
- **字体度量**: 使用字体的 Ascent 值计算精确的基线位置

### 3. 关键代码更改

#### GdiTextRenderer 修复:
```csharp
// 计算基线校正
var baselineOffset = CalculateBaselineOffset(font, fontSize);
var drawPoint = new PointF(
    boundary.X + textCode.X,
    boundary.Y + textCode.Y - baselineOffset  // 减去基线偏移
);
```

#### SkiaTextRenderer 修复:
```csharp
// 计算基线校正偏移量
var baselineOffset = CalculateSkiaBaselineOffset(paint, fontSize);
var adjustedY = boundary.Y + textCode.Y + baselineOffset;
canvas.DrawText(textCode.Text, boundary.X + textCode.X, adjustedY, paint);
```

## 使用方法

### 1. 基本使用
修复后的代码会自动应用基线校正，无需额外配置。

### 2. 高级配置
如果需要微调偏移量，可以使用配置类：

```csharp
// 调整 GDI 偏移系数（默认 0.15f）
TextRenderingConfig.Instance.GdiBaselineOffsetFactor = 0.2f;

// 调整 Skia 偏移系数（默认 0.8f）
TextRenderingConfig.Instance.SkiaBaselineOffsetFactor = 0.85f;

// 启用调试输出
TextRenderingConfig.Instance.EnableDebugOutput = true;

// 禁用基线校正（如果需要回到原始行为）
TextRenderingConfig.Instance.EnableBaselineCorrection = false;
```

### 3. 验证修复效果
使用提供的验证工具：

```csharp
// 运行验证测试
TextOffsetFixVerification.RunAllVerificationTests();

// 生成诊断报告
var report = TextPositionDiagnostic.GenerateDebugReport(textObject);
Console.WriteLine(report);
```

## 调整参数说明

### GdiBaselineOffsetFactor
- **作用**: 控制 GDI 文本向上调整的幅度
- **默认值**: 0.15f
- **调整方向**: 增大值 → 文本更向上；减小值 → 文本更向下

### SkiaBaselineOffsetFactor  
- **作用**: 控制 Skia 文本基线对齐的精度
- **默认值**: 0.8f
- **调整方向**: 增大值 → 基线更精确；减小值 → 基线偏移更小

## 测试建议

1. **对比测试**: 使用 `CompareBeforeAfterFix()` 方法查看修复前后的位置差异
2. **视觉验证**: 生成测试图像，确认文本位置是否正确
3. **不同字体测试**: 测试不同字体是否都能正确渲染
4. **不同字号测试**: 确认各种字体大小下的效果

## 故障排除

### 如果文本仍然偏移：
1. 检查 `EnableBaselineCorrection` 是否为 true
2. 调整对应的偏移系数
3. 启用调试输出查看计算的偏移值
4. 使用诊断工具分析文本对象的属性

### 如果文本位置过度校正：
1. 减小偏移系数
2. 检查原始 XML 中的坐标是否正确
3. 确认边界框解析是否正确

## 性能影响
- 基线校正计算的性能开销很小
- 字体度量信息会被缓存
- 可以通过禁用基线校正来获得最佳性能（如果不需要精确位置）

## 兼容性
- 修复保持了向后兼容性
- 可以通过配置类禁用新功能
- 不影响原有的文本提取和解析功能

# T002 Decision: Viewer.Core Project

## Decision
决定**不**创建独立的 `OfdrwNet.Viewer.Core` 项目。

## Rationale
1. 现有解决方案已经有多个项目，避免过早复杂化
2. 在 `OfdrwNet.WinFormsDemo` 中使用命名空间隔离足以满足模块化需求
3. 可在后续迭代中根据需要重构为独立项目
4. 符合宪法原则中的简洁性要求（YAGNI）

## Implementation Strategy
- 在 `src/OfdrwNet.WinFormsDemo/Viewer/` 下创建子目录结构：
  - `Rendering/` - 渲染相关类
  - `Caching/` - 缓存管理
  - `Telemetry/` - 性能监测
  - `Models/` - 数据模型
  - `Abstractions/` - 接口定义

## Future Considerations
如果需要在其他UI框架（WPF、Avalonia等）中重用逻辑，可后续抽取为独立的Core项目。

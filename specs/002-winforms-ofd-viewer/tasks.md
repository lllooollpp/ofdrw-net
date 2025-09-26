# Tasks: WinForms OFD Viewer 全功能渲染与查看

**Input**: Design documents from `/specs/002-winforms-ofd-viewer/`  
**Prerequisites**: `plan.md` (present), `research.md` (scaffold), `data-model.md` (NOT YET), `contracts/` (NOT YET)

## Execution Flow (generated)
依据 plan 与 spec 生成：设置 → 测试(失败) → 核心实现 → 集成 → 打磨。数据模型与契约将在对应任务中首次创建（TDD）。

## Phase 3.1: Setup
- [x] T001 建立 Viewer 核心结构：在 `src/OfdrwNet.WinFormsDemo/` 下创建 `Viewer` 目录与子文件：`ViewerHost.cs`, `PageViewportControl.cs`, `IViewerLogger.cs`（接口仅定义日志方法原型）。
- [x] T002 添加（可选）独立项目 `OfdrwNet.Viewer.Core`（若决定启用）：创建 `src/OfdrwNet.Viewer.Core/OfdrwNet.Viewer.Core.csproj` 及基础目录 `Rendering/`, `Caching/`, `Telemetry/`, `Abstractions/`；引用 `OfdrwNet.Reader`。
- [x] T003 [P] 在解决方案中添加测试项目 `tests/OfdrwNet.Viewer.Tests/OfdrwNet.Viewer.Tests.csproj` 引用核心(WinFormsDemo 或 Viewer.Core)；配置 `MSTest`。
- [x] T004 [P] 创建 `tests/Directory.Build.props`（若不存在）启用可空、treat warnings as errors（可选控制）。
- [x] T005 初始化基础性能测试占位：`tests/OfdrwNet.Viewer.Tests/Performance/RenderBaselineTests.cs`（空测试标记 Fact Skip）。

## Phase 3.2: Tests First (TDD) – 合同/交互与集成
（以下测试文件先写出失败断言或 NotImplemented）
- [ ] T006 定义数据模型文件骨架：`src/OfdrwNet.Viewer.Core/Abstractions/Models.cs`（或 Demo/Viewer/Models.cs）不实现逻辑，仅 public record/类签名。
- [ ] T007 [P] 合同测试：文档加载成功失败场景 → `tests/OfdrwNet.Viewer.Tests/Contracts/DocumentLoaderContractTests.cs`。
- [ ] T008 [P] 合同测试：页面渲染请求输出字段完整性 → `tests/OfdrwNet.Viewer.Tests/Contracts/PageRendererContractTests.cs`。
- [ ] T009 [P] 合同测试：缓存策略（命中/失效/容量淘汰）→ `tests/OfdrwNet.Viewer.Tests/Contracts/PageCacheContractTests.cs`。
- [ ] T010 [P] 合同测试：遥测事件（Parse/Layout/Paint 时序与字段）→ `tests/OfdrwNet.Viewer.Tests/Contracts/TelemetryContractTests.cs`。
- [ ] T011 [P] 集成测试：打开文件并渲染第一页 → `tests/OfdrwNet.Viewer.Tests/Integration/OpenAndFirstPageTests.cs`。
- [ ] T012 [P] 集成测试：翻页（前进/后退/边界）→ `tests/OfdrwNet.Viewer.Tests/Integration/PageNavigationTests.cs`。
- [ ] T013 [P] 集成测试：缩放（比例变化 & 锚点保持）→ `tests/OfdrwNet.Viewer.Tests/Integration/ZoomBehaviorTests.cs`。
- [ ] T014 [P] 集成测试：快速连续翻页不卡 UI（模拟异步调用顺序）→ `tests/OfdrwNet.Viewer.Tests/Integration/ResponsivePagingTests.cs`。
- [ ] T015 [P] 集成测试：字体缺失回退记录事件 → `tests/OfdrwNet.Viewer.Tests/Integration/FontFallbackTests.cs`。
- [ ] T016 [P] 边界测试：空/损坏文件 → `tests/OfdrwNet.Viewer.Tests/EdgeCases/InvalidFileTests.cs`。
- [ ] T017 [P] 边界测试：巨大页面尺寸内存与降级策略 → `tests/OfdrwNet.Viewer.Tests/EdgeCases/LargePageTests.cs`。
- [ ] T018 [P] 边界测试：大量矢量对象性能告警 → `tests/OfdrwNet.Viewer.Tests/EdgeCases/VectorDensityTests.cs`。
- [ ] T019 [P] 边界测试：图片解码失败占位符 → `tests/OfdrwNet.Viewer.Tests/EdgeCases/ImageDecodeFallbackTests.cs`。
- [ ] T020 [P] 边界测试：加载中关闭取消 → `tests/OfdrwNet.Viewer.Tests/EdgeCases/CancelDuringLoadTests.cs`。

## Phase 3.3: Core Implementation（仅在上述测试全部存在且失败后开始）
- [ ] T021 实现数据模型实体（DocumentModel/PageModel/RenderRequest/RenderResult/CacheEntry/TelemetryEvent）填充属性。
- [ ] T022 实现 IOfdDocumentLoader：`src/OfdrwNet.Viewer.Core/Rendering/OfdDocumentLoader.cs`（或 Demo/Viewer/），使用 `OfdrwNet.Reader` 解析；异常分类。
- [ ] T023 实现 IPageRenderer 骨架：`Rendering/PageRenderer.cs`（包含阶段计时钩子但内部逻辑先抛 NotImplemented）。
- [ ] T024 实现 子渲染：文字/矢量/图像 分层策略（`Rendering/Sub/`）并集成到 PageRenderer（先最小可用：迭代对象 -> 绘制）。
- [ ] T025 实现 IPageCache（LRU 或环形窗口）于 `Caching/PageCache.cs`，含容量 & 逐出策略。
- [ ] T026 将缓存整合进 PageRenderer（渲染前查询 / 渲染后写入）。
- [ ] T027 实现 TelemetrySink（记录到内存集合 + 可序列化），路径：`Telemetry/DefaultTelemetrySink.cs`。
- [ ] T028 将遥测注入到 Loader / PageRenderer 各阶段（Parse/Layout/Paint/Composite）。
- [ ] T029 实现字体回退提供器（IFallbackProvider）+ 简单映射表（SimSun→Arial 等）。
- [ ] T030 在文字渲染阶段加入字体回退逻辑与事件记录。
- [ ] T031 实现缩放与导航控制器：`Viewport/ViewportController.cs`（CurrentPage, Zoom, ApplyZoom(anchor)）。
- [ ] T032 将 ViewportController 与 UI 控件交互：更新显示页与缩放。
- [ ] T033 UI 控件 `PageViewportControl`：支持双缓冲绘制 + OnPaint 调用 PageRenderer（防闪烁）。
- [ ] T034 主窗体增强：菜单/工具栏（打开/导航/缩放）+ 状态栏显示页码与缩放比。
- [ ] T035 引入取消令牌机制（加载/渲染中止）。
- [ ] T036 快速翻页节流：调度最新渲染请求，丢弃过时请求。

## Phase 3.4: Integration
- [ ] T037 将日志接口 IViewerLogger 适配到 `ILogger`（若使用）或简单文件追加器：`Logging/ViewerLoggerAdapter.cs`。
- [ ] T038 在关键路径注入日志（LoadStart/LoadEnd/PageRenderStart/PageRenderEnd/CacheHit/CacheMiss/FontFallback/DecodeFail）。
- [ ] T039 性能计时聚合：启动时创建 TelemetryAggregator 提供平均/最小渲染时间 API。
- [ ] T040 增加导出当前页面（若后续确认）占位命令与日志（可先抛 NotImplemented）。
- [ ] T041 实现异常分类与用户提示（MessageBox + 统一错误码枚举）。
- [ ] T042 资源释放：文档关闭时清理缓存、释放位图、注销事件。

## Phase 3.5: Polish & Verification
- [ ] T043 [P] 针对缓存/渲染/导航/缩放 添加单元测试补集（覆盖异常和降级路径）。
- [ ] T044 [P] 性能基准测试：`tests/OfdrwNet.Viewer.Tests/Performance/RenderBenchmark.cs`（BenchmarkDotNet 可选，若引入需更新 csproj）。
- [ ] T045 [P] 文档：生成/更新 `quickstart.md`（若不存在则创建：展示最小使用示例）。
- [ ] T046 [P] 生成开发日志指南：`specs/002-winforms-ofd-viewer/logging-guide.md`（列出事件字段）。
- [ ] T047 清理未使用代码与 TODO；开启可空与警告审查（修复 CS86xx）。
- [ ] T048 安全 & 包审核：更新潜在漏洞包版本（System.Text.Json 等）并验证构建。
- [ ] T049 生成最终实现报告（类似已有报告结构）记录性能基线与差异。
- [ ] T050 回归测试：用 `tests/*.pdf` -> 转换 pipeline 复查无破坏性影响（自动或手动脚本）。

## Dependencies 概览
- Setup (T001-T005) → 所有后续任务前置。
- 测试骨架 (T006-T020) 必须在核心实现 (T021+) 前创建并失败。
- 模型 (T021) 先于 Loader/Renderer/Cache (T022-T026)。
- 遥测 (T027-T028) 依赖基础渲染骨架 (T023)。
- 字体回退 (T029-T030) 依赖文字渲染子模块 (T024)。
- 缩放/导航 (T031-T032) 依赖渲染与缓存。
- UI 控件与主窗体 (T033-T034) 依赖渲染/控制器。
- 节流 (T036) 依赖基础渲染与导航。
- 日志/聚合 (T037-T039) 依赖渲染与遥测。
- Polish 阶段 (T043+) 在集成完成后。

## Parallel Execution Suggestions
初始可并行：T003, T004, T005；测试阶段内 T007-T020 多数互不共享文件可并行；实现阶段内需谨慎串行（核心共享文件）。Polish 阶段 T043/T044/T045/T046 可并行。

## Notes
- 标记 [P] 的任务可独立运行且不写入同一文件。
- 所有测试初稿先使用 Assert.True(false, "Not implemented") 或抛 NotImplementedException 确认红灯。
- 完成每任务后提交独立 commit，保留演进清晰度。
- 若放弃创建 Viewer.Core 项目，则 T002 改为“记录决定不创建单独核心层”并继续。

## Validation Checklist
- [ ] 所有核心测试 (T007-T020) 在实现前均已存在并失败。
- [ ] 模型/缓存/渲染/遥测/日志链路均有至少一条对应测试。
- [ ] 并行任务未写入同一文件。
- [ ] 性能基准与日志事件文档存在。

# Implementation Plan: WinForms OFD Viewer 全功能渲染与查看

**Branch**: `002-winforms-ofd-viewer` | **Date**: 2025-09-25 | **Spec**: specs/002-winforms-ofd-viewer/spec.md  
**Input**: Feature specification from `/specs/002-winforms-ofd-viewer/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec (DONE)
2. Fill Technical Context (DONE - unresolved points marked)
3. Constitution Check (initial) (DONE)
4. Evaluate Constitution Check → no blocking violations
5. Phase 0 research outline (completed in this plan; real research doc to be produced if unknowns resolved) → research.md (WILL CREATE)
6. Phase 1 design outline → data-model.md, quickstart.md, contracts/ (WILL CREATE)
7. Re-check Constitution after Phase 1 (TO DO AFTER GENERATION) 
8. Describe Phase 2 task generation strategy (DONE)
9. STOP (Ready for /tasks)
```

## Summary
该特性旨在提供一个基于现有 `OfdrwNet.Reader` 库的 WinForms OFD 文档查看器，覆盖：文件加载、页导航、缩放、完整页面渲染（文字 / 矢量 / 图像 / 层）、性能与错误日志观测、缓存与（可选）预加载。将补齐 Reader 中缺失的必要解析与渲染扩展，并保证可测试性与质量门。

技术实现将遵循：模块化（Reader 扩展最小侵入）、TDD（先契约/渲染验证测试再实现）、可观测性（结构化日志 + 性能计时点）、正确性（与 mock3 OFD 样例/Java 参考对比）。

## Technical Context
**Language/Version**: C# .NET 8.0 (WinForms, net8.0-windows)  
**Primary Dependencies**: System.Drawing (GDI+ 渲染), System.Xml.Linq (OFD 结构解析), System.IO.Compression (OFD 包 ZIP), System.Text.Json (配置), （潜在）自研解析扩展模块  
**Storage**: 本地文件系统（OFD 包读取），内存缓存（页渲染缓存）  
**Testing**: xUnit / MSTest (确认现有测试框架；[NEEDS CLARIFICATION: 当前解决方案采用哪个测试框架?]) + 合同/集成测试 + 性能基准（BenchmarkDotNet 可选）  
**Target Platform**: Windows 桌面（WinForms），DPI 感知  
**Project Type**: 单一多项目解决方案（核心库 + Demo UI）  
**Performance Goals**: 首次页面渲染 < 500ms（中等复杂度文档）；翻页平均 < 200ms；缩放反馈 < 100ms；[NEEDS CLARIFICATION: 目标是否需要量化调整?]  
**Constraints**: 内存占用单页缓存 < 50MB；缓存页数默认 3；无阻塞 UI 线程；[NEEDS CLARIFICATION: 最大文档页数?]  
**Scale/Scope**: 面向中型单文件 OFD（估计 <= 500 页）[NEEDS CLARIFICATION: 是否更大规模?]

未决设计位移到 Phase 0 研究：缩放锚点策略、预加载策略、导出页面、最近文件列表、文字搜索、颜色管理、签名可视化。

## Constitution Check (Initial)
| 宪法原则 | 风险/偏差 | 缓解措施 |
|----------|-----------|----------|
| 模块化 (I) | 需要在 Reader 中添加渲染扩展点 | 通过接口/适配器注入，避免直接改核心解析路径 |
| 正确性 (II) | 缺少与 mock3/Java 行为差异比对 | 增加结构与像素 diff 测试（宽容阈值） |
| 测试优先 (III) | WinForms UI 自动化较难 | 核心渲染逻辑抽离到可测试服务；UI 层最小化逻辑 |
| 运行时稳定 (IV) | 新增缓存/异步可能引入竞态 | 使用不可变快照 + UI 线程调度封送 | 
| 可观测性 (V) | 性能点可能遗漏 | 统一 IRenderTelemetry 接口收集阶段耗时 |

初步无阻塞性违反；标记的风险将在 Phase 0/1 设计中细化。

## Project Structure (Planned additions)
保持现有解决方案结构，新增或扩展：
```
src/OfdrwNet.Reader/            # 若需：扩展渲染抽象或解析对象支持缺失特性
src/OfdrwNet.WinFormsDemo/      # Viewer UI（主窗体 + 控件）
src/OfdrwNet.Viewer.Core/       # （可选新项目）抽象出渲染协调、缓存、日志、命令
tests/OfdrwNet.Viewer.Tests/    # 单元 + 集成（渲染输出 & 行为）
tests/OfdrwNet.Reader.Extensions.Tests/ # 新增解析能力测试
```
若避免增加项目复杂度，可先不创建 `Viewer.Core`，在 Demo 内部以命名空间隔离；[NEEDS CLARIFICATION: 是否允许新建中间层项目?]

## Phase 0: Outline & Research
聚焦解析未决点 → 输出 `research.md`（含决策表）。

研究任务草案：
1. 缩放锚点策略（窗口中心 vs 鼠标）对用户体验与实现复杂度比较。
2. 页预加载策略（同步/异步/懒加载）对内存与响应性的影响。
3. 日志后端需求（仅文件 vs 可插拔 ILogger 适配）与结构化字段集合。
4. 性能基线：在 mock3 文档上测量当前 Reader 原始解析速度。
5. 文本渲染精度（字体缺失策略、fallback 映射表）。
6. 矢量路径兼容性（填充规则 EvenOdd / NonZero）。
7. 图像格式与潜在解码失败策略（支持 PNG/JPEG/BMP? α 通道处理）。
8. DPI 感知与高 DPI 插值策略（GDI+ 与可能的抗锯齿设置）。
9. 签名/印章可视化可行性（若范围外则确认延期）。
10. 文字搜索可行性（建立文字索引 vs 延迟实现）。

`research.md` 结构：
```
## Decision Matrix
- 主题
  - 决策
  - 理由
  - 备选与放弃原因
  - 后续可扩展点
```
若 NEEDS CLARIFICATION 继续存在 → Gate 阻塞进入 Phase 1（符合模板规则）。

## Phase 1: Design & Contracts
输出：`data-model.md`, `quickstart.md`, `contracts/`（UI 非网络 API，改为“交互契约/服务接口契约”）。

1. 数据模型：
	- DocumentModel（Id, PageCount, Metadata）
	- PageModel（Index, Size, Layers, Objects 摘要）
	- RenderRequest（PageIndex, Zoom, ViewportSize, CachePolicy, Token）
	- RenderResult（Bitmap/SurfaceRef, Metrics, Diagnostics）
	- CacheEntry（Key(Page+Zoom), SurfaceRef, CreatedAt, MemoryBytes）
	- FontFallbackRule（Pattern, SubstituteFont）
	- TelemetryEvent（Name, Timestamp, Duration, Tags）

2. 契约设计（以接口/伪 API 形式）：
	- IOfdDocumentLoader: Load(path) → DocumentModel
	- IPageRenderer: Render(RenderRequest) → RenderResult
	- IPageCache: TryGet / Set / Invalidate
	- ITelemetrySink: Record(TelemetryEvent)
	- IFontResolver / IFallbackProvider
	- IViewportController: ApplyZoom / NavigatePage

3. 交互契约测试（contracts/）：
	- LoaderContract.md（成功/失败场景表）
	- RendererContract.md（输入组合 → 期望属性填充）
	- CacheContract.md（LRU 或固定窗口策略验证步骤）
	- TelemetryContract.md（事件最小字段 + 顺序）

4. quickstart.md：展示 “打开 → 渲染第一页 → 翻页 → 缩放” 最小代码片段（伪调用）。

5. agent context 更新：执行 update-agent-context 脚本（稍后）以纳入新增技术描述（无外部新库预期）。

## Phase 2: Task Planning Approach
（不在本命令执行，仅描述）
任务来源：
- 每个接口契约 → 创建 + 测试任务
- 每个渲染阶段（Parse/Text/Vector/Image/Composite）→ 性能计时与日志植入任务
- 每个 Edge Case → 测试用例任务
- 缓存策略 → 算法实现 + 内存度量测试
排序：
1. 基础文档加载
2. 页面解析摘要
3. 渲染管线骨架（空实现 + 计时点）
4. 文本 / 矢量 / 图像 各子渲染器
5. 缓存 + 预加载（若确认）
6. 缩放/导航控制器
7. Fallback / 错误注入测试
8. UI 集成（WinForms 控件层）

## Phase 3+: Future Implementation
Phase 3 (/tasks) 生成 tasks.md；Phase 4 按优先级实现；Phase 5 添加性能验证脚本并输出基线。

## Complexity Tracking
| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| 可能新增 Viewer.Core 项目 | 分离 UI 与渲染协调逻辑 | 直接放在 WinFormsDemo 中将导致测试困难与逻辑耦合 |

## Progress Tracking
**Phase Status**:
- Spec Loaded: YES
- Technical Context Filled: YES (含未决项)
- Constitution Initial Check: DONE
- Phase 0 Research Planned: YES
- Phase 1 Design Planned: YES
- Post-Design Constitution Check: PENDING
- Ready for Tasks Phase: YES (待 /tasks 执行)

**Gate Status**:
- Unknowns Present: YES (需在 research.md 决策)
- Blocking Violations: NO

（后续 /tasks 前需先生成 research.md 等文件——当前 plan 指南已准备）


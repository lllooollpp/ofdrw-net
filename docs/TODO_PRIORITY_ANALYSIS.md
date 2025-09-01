# TODO 项目优先级分析和实施计划

## 概览
扫描整个项目后，发现以下主要TODO分类：

### 高优先级TODO项目（影响核心功能）

#### 1. 资源管理（ResourceManage.cs）
- **文件**: `OfdrwNet.Reader\ResourceManage.cs`
- **影响**: 文档加载和资源访问的核心功能
- **TODO项目**:
  - Line 176: 实现完整的继承参数解析逻辑
  - Line 195: 实现文档加载逻辑  
  - Line 211: 实现从OFD包中获取资源流的逻辑

#### 2. 页面对象类型（IPageBlockType.cs）
- **文件**: `OfdrwNet.Core\BasicStructure\PageObj\Layer\IPageBlockType.cs`
- **影响**: 页面内容渲染的基础类型系统
- **TODO项目**:
  - Line 34: TextObject类实现
  - Line 38: PathObject类实现
  - Line 42: ImageObject类实现
  - Line 46: CompositeObject类实现

#### 3. OFD合并器（OFDMerger.cs）
- **文件**: `OfdrwNet.Tools\OFDMerger.cs`
- **影响**: 文档合并功能
- **TODO项目**:
  - Line 354: 复制相关的资源文件（图像、字体等）

### 中优先级TODO项目

#### 4. 文档信息（CtDocInfo.cs）
- **文件**: `OfdrwNet.Core\BasicStructure\Ofd\DocInfo\CtDocInfo.cs`
- **影响**: 文档元数据管理
- **TODO项目**:
  - Line 461, 485: Keywords类型迁移
  - Line 476: 关键词添加逻辑
  - Line 570, 583: CustomDatas类型迁移

#### 5. 书签（Bookmark.cs）
- **文件**: `OfdrwNet.Core\BasicStructure\Doc\Bookmark\Bookmark.cs`
- **影响**: 文档导航功能
- **TODO项目**:
  - Line 38, 91, 106, 115: CT_Dest类型迁移
  - Line 96, 108: 书签目标设置和获取逻辑
  - Line 181: Dest复制逻辑

#### 6. 页面解析引擎（VPageParseEngine.cs）
- **文件**: `OfdrwNet.Layout\Engine\VPageParseEngine.cs`
- **影响**: 文本对象创建
- **TODO项目**:
  - Line 434: 文本对象创建逻辑（等待类型冲突解决）

## 实施计划

### 第一阶段：核心基础类型
1. 实现缺失的对象类型（TextObject, PathObject, ImageObject, CompositeObject）
2. 解决类型冲突问题

### 第二阶段：资源管理系统
1. 实现ResourceManage.cs中的核心方法
2. 确保文档加载和资源访问功能

### 第三阶段：工具和扩展功能
1. 完成OFDMerger资源复制功能
2. 实现书签和文档信息管理

### 第四阶段：优化和完善
1. 添加错误处理和验证
2. 性能优化和测试

## 依赖关系分析
- IPageBlockType.cs 是许多其他组件的基础
- ResourceManage.cs 影响整个文档处理流程
- VPageParseEngine.cs 依赖于基础对象类型的完整实现

## 预期影响
完成这些TODO将显著提升：
1. 文档渲染的准确性和完整性
2. 资源管理的可靠性
3. 文档处理工具的功能完整性
4. 整体项目的稳定性和可用性

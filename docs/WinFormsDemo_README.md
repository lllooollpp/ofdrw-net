# OFDRW.NET WinForms 演示程序

## 项目简介

本项目是基于 OFDRW.NET 库开发的 Windows Forms 应用程序，提供了一个图形化界面来演示文档转换功能。该应用程序支持将 Word、HTML 和 PDF 文档转换为 OFD 格式。

## 功能特性

- ✅ **Word 转 OFD**: 支持 `.docx` 格式的 Word 文档转换
- ✅ **HTML 转 OFD**: 支持 `.html` 和 `.htm` 格式的 HTML 文件转换  
- ✅ **PDF 转 OFD**: 支持 `.pdf` 格式的 PDF 文档转换
- ✅ **图形化界面**: 直观易用的 Windows Forms 界面
- ✅ **进度显示**: 实时显示转换进度和状态
- ✅ **文件信息**: 自动识别和显示选择文件的基本信息
- ✅ **错误处理**: 完善的异常处理和用户提示
- ✅ **异步处理**: 非阻塞的异步转换操作

## 技术架构

### 核心技术栈
- **.NET 8**: 基于最新的 .NET 8 框架
- **Windows Forms**: 传统的 Windows 桌面应用程序框架
- **OFDRW.NET**: 核心 OFD 文档处理库

### 第三方依赖
- **DocumentFormat.OpenXml** (v3.0.0): Word 文档处理
- **HtmlAgilityPack** (v1.11.54): HTML 解析和处理
- **AngleSharp** (v0.17.1): 高级 HTML 处理
- **iTextSharp** (v5.5.13.3): PDF 文档处理
- **Microsoft.Extensions.Logging** (v8.0.0): 日志记录

## 项目结构

```
OfdrwNet.WinFormsDemo/
├── Converters/                    # 转换器实现
│   ├── Word2OfdConverter.cs       # Word 转 OFD 转换器
│   ├── Html2OfdConverter.cs       # HTML 转 OFD 转换器
│   ├── Pdf2OfdConverter.cs        # PDF 转 OFD 转换器
│   └── ConversionModels.cs        # 转换相关的模型和事件
├── MainForm.cs                    # 主窗体逻辑
├── MainForm.Designer.cs           # 主窗体设计器代码
├── Program.cs                     # 程序入口点
└── OfdrwNet.WinFormsDemo.csproj   # 项目配置文件
```

## 安装和运行

### 环境要求
- **操作系统**: Windows 10/11 (支持 .NET 8)
- **.NET 运行时**: .NET 8.0 Desktop Runtime
- **开发环境**: Visual Studio 2022 或 JetBrains Rider

### 构建项目
```bash
# 进入项目目录
cd OfdrwNet.WinFormsDemo

# 还原 NuGet 包
dotnet restore

# 构建项目
dotnet build

# 运行应用程序
dotnet run
```

### 直接运行
```bash
# 在项目根目录下
dotnet run --project OfdrwNet.WinFormsDemo
```

## 使用说明

### 基本操作流程

1. **选择输入文件**
   - 点击"浏览..."按钮选择要转换的文档
   - 支持的格式：`.docx`、`.html`、`.htm`、`.pdf`
   - 应用程序会自动检测文件类型并设置转换模式

2. **设置输出文件**
   - 应用程序会自动生成输出文件名（与输入文件同目录）
   - 可以手动修改输出路径和文件名
   - 输出格式固定为 `.ofd`

3. **确认转换类型**
   - 应用程序会根据输入文件自动选择转换类型
   - 也可以手动选择：Word → OFD、HTML → OFD、PDF → OFD

4. **查看文件信息**
   - 文件信息区域会显示输入文件的详细信息
   - 包括文件大小、修改时间、页面数量等

5. **开始转换**
   - 点击"开始转换"按钮启动转换过程
   - 进度条会实时显示转换进度
   - 转换过程中可以点击"取消转换"中止操作

6. **完成转换**
   - 转换完成后会显示结果对话框
   - 包含输出文件路径、文件大小、转换耗时等信息

### 界面组件说明

- **输入文件区域**: 选择和显示要转换的源文件
- **输出文件区域**: 设置 OFD 文件的保存位置
- **转换类型区域**: 选择具体的转换模式
- **文件信息区域**: 显示选择文件的详细信息
- **进度显示区域**: 实时显示转换进度和状态消息
- **操作按钮**: 开始转换、取消转换、清空表单

## 技术实现细节

### 转换器架构
每个转换器都实现了统一的接口模式：
- **异步转换**: 使用 `async/await` 模式确保 UI 响应性
- **进度报告**: 通过事件机制实时更新转换进度
- **错误处理**: 完善的异常捕获和错误信息反馈
- **资源管理**: 正确实现 `IDisposable` 模式

### Word 转换器特性
- 基于 OpenXML SDK 解析 Word 文档结构
- 提取段落文本并保持基本格式
- 支持文档元数据处理

### HTML 转换器特性  
- 使用 HtmlAgilityPack 解析 HTML DOM 结构
- 支持标题层级识别（H1-H6）
- 提取纯文本内容并转换为 OFD 段落

### PDF 转换器特性
- 使用 iTextSharp 提取 PDF 文本内容
- 支持多页 PDF 文档处理
- 保持页面分隔标记

## 已知限制

1. **格式支持限制**
   - Word 转换仅支持 `.docx` 格式（不支持 `.doc`）
   - PDF 转换主要提取文本内容，不保留复杂布局
   - HTML 转换专注于文本内容，不处理 CSS 样式

2. **功能限制**
   - 当前版本主要处理文本内容转换
   - 不支持图像、表格等复杂元素
   - OFD 输出为简化版本，主要用于概念验证

3. **性能考虑**
   - 大文件转换可能需要较长时间
   - 内存使用与文件大小成正比

## 开发和扩展

### 添加新的转换器
1. 在 `Converters` 目录下创建新的转换器类
2. 实现 `IDisposable` 接口和标准事件模式
3. 在 `MainForm.cs` 中集成新转换器
4. 更新 `FileTypeDetector` 以支持新文件格式

### 自定义转换选项
转换器支持通过构造函数或属性设置自定义选项：
- 页面布局设置
- 字体和样式配置
- 转换质量参数

## 故障排除

### 常见问题

**问题**: 应用程序启动失败
- **解决方案**: 确保安装了 .NET 8 Desktop Runtime

**问题**: 转换过程中出现异常
- **解决方案**: 检查输入文件是否损坏，确保有足够的磁盘空间

**问题**: 输出文件无法打开
- **解决方案**: 当前版本生成的是简化的 OFD 文件，可能需要兼容的阅读器

### 日志信息
应用程序使用 Microsoft.Extensions.Logging 记录详细的运行信息，可以通过控制台查看日志输出。

## 贡献指南

欢迎提交 Issue 和 Pull Request 来改进项目：
1. Fork 项目仓库
2. 创建功能分支
3. 提交代码变更
4. 创建 Pull Request

## 许可证

本项目遵循与 OFDRW.NET 主项目相同的许可证条款。

## 版本历史

### v1.0.0 (当前版本)
- ✅ 实现基础的 Word、HTML、PDF 转 OFD 功能
- ✅ 创建完整的 Windows Forms 用户界面
- ✅ 集成进度显示和错误处理机制
- ✅ 支持异步转换操作
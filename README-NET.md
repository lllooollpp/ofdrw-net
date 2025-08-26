# OfdrwNet - OFD Reader & Writer for .NET 8

这是将 Java 版本的 OFDRW 项目转换为 .NET 8 的实现。

## 项目架构

本项目采用模块化设计，每个模块对应 Java 版本的一个模块：

### 核心模块

- **OfdrwNet.Core** - 核心基础设施
  - 对应 Java 的 `ofdrw-core`
  - 包含 OFD 文档的基础数据结构、常量定义、基本类型等
  - 实现 OFD 格式的核心元素和规范

- **OfdrwNet.Packaging** - 文件包装和容器管理
  - 对应 Java 的 `ofdrw-pkg`
  - 处理 OFD 文件的压缩包结构
  - 虚拟容器管理和文件组织

- **OfdrwNet.Reader** - 文档读取器
  - 对应 Java 的 `ofdrw-reader`
  - 解析和读取现有的 OFD 文档
  - 资源定位和页面访问

### 功能模块

- **OfdrwNet.Layout** - 布局引擎
  - 对应 Java 的 `ofdrw-layout`
  - 页面布局和内容渲染
  - 流式布局和固定布局支持

- **OfdrwNet.Font** - 字体处理
  - 对应 Java 的 `ofdrw-font`
  - 字体资源管理和字形处理

- **OfdrwNet.Graphics** - 图形绘制
  - 对应 Java 的 `ofdrw-graphics2d`
  - 基于 SkiaSharp 的图形绘制功能
  - 2D 图形处理和渲染

### 安全和转换模块

- **OfdrwNet.Crypto** - 密码学功能
  - 对应 Java 的 `ofdrw-crypto` 和 `ofdrw-gm`
  - 数字签名和加密功能
  - 国密算法支持

- **OfdrwNet.Sign** - 数字签名
  - 对应 Java 的 `ofdrw-sign`
  - 数字证书和签名操作

- **OfdrwNet.Converter** - 格式转换
  - 对应 Java 的 `ofdrw-converter`
  - OFD 与其他格式的相互转换

### 工具和测试

- **OfdrwNet.Tools** - 工具集
  - 对应 Java 的 `ofdrw-tool`
  - 文档合并、分割等实用工具

- **OfdrwNet.Tests** - 单元测试
  - 基于 xUnit 的测试框架
  - 所有模块的单元测试

- **OfdrwNet.Examples** - 示例应用
  - 控制台示例程序
  - 演示各种功能的使用方法

## 技术栈

- **.NET 8** - 目标框架
- **System.Xml.Linq** - XML 处理（替代 Java 的 DOM4J）
- **SkiaSharp** - 图形绘制（替代 Java 的 Graphics2D）
- **System.Security.Cryptography** - 密码学功能
- **xUnit** - 单元测试框架（替代 JUnit）

## 快速开始

### 安装依赖

```bash
dotnet restore
```

### 构建项目

```bash
dotnet build
```

### 运行示例

```bash
dotnet run --project OfdrwNet.Examples
```

### 运行测试

```bash
dotnet test
```

## 开发状态

- [x] 项目架构设计和解决方案创建
- [x] 核心基础设施转换
- [x] XML 处理转换
- [x] 文件容器管理转换
- [x] 基础示例程序编写
- [ ] 文档读取器转换
- [ ] 布局引擎转换
- [ ] 字体处理转换
- [ ] 密码学功能转换
- [ ] 图形绘制转换
- [ ] 格式转换转换
- [ ] 工具模块转换
- [ ] 数字签名转换
- [ ] API 统一整合
- [ ] 单元测试迁移
- [ ] NuGet 包配置

## 已实现功能

### 核心功能 (OfdrwNet.Core)
- ✅ OFD 格式常量定义
- ✅ 基础数据类型 (StId, StLoc)
- ✅ OFD 元素基类 (OfdElement)
- ✅ 简单类型元素 (OfdSimpleTypeElement)
- ✅ XML 命名空间处理
- ✅ 元素属性管理
- ✅ 子元素操作

### 文件容器管理 (OfdrwNet.Packaging)
- ✅ 虚拟容器 (VirtualContainer)
- ✅ 文件和目录管理
- ✅ XML 对象缓存
- ✅ 文件哈希验证
- ✅ 容器嵌套和路径解析
- ✅ 资源清理和刷新

### 示例程序 (OfdrwNet.Examples)
- ✅ 基础常量展示
- ✅ 数据类型使用示例
- ✅ OFD 元素创建和操作
- ✅ 虚拟容器使用演示
- ✅ 文件系统集成测试

## 使用示例

### 基础类型操作
```csharp
using OfdrwNet.Core.BasicType;

// 创建标识符
var id = new StId(123);
Console.WriteLine(id.ToString()); // "123"

// 创建位置路径
var loc = new StLoc("/Doc_0/Pages/Page_0/Content.xml");
Console.WriteLine(loc.GetFileName()); // "Content.xml"
Console.WriteLine(loc.Parent()); // "/Doc_0/Pages/Page_0"

// 路径拼接
var newLoc = loc.Parent().Cat("Resources/font.ttf");
Console.WriteLine(newLoc); // "/Doc_0/Pages/Page_0/Resources/font.ttf"
```

### OFD 元素操作
```csharp
using OfdrwNet.Core;

// 创建 OFD 元素
var element = OfdElement.GetInstance("Document");
element.AddAttribute("Version", "1.0")
       .AddOfdEntity("Title", "我的OFD文档")
       .AddOfdEntity("Creator", "OfdrwNet");

// 转换为 XML
string xml = element.ToXml();
Console.WriteLine(xml);
```

### 虚拟容器操作
```csharp
using OfdrwNet.Packaging.Container;

// 创建容器
using var container = new VirtualContainer("./MyOfdDoc");

// 创建子容器
var docContainer = container.ObtainContainer("Doc_0", 
    () => new VirtualContainer(Path.Combine("./MyOfdDoc", "Doc_0")));

// 添加对象到容器
docContainer.PutObj("Document.xml", element);

// 刷新到文件系统
container.Flush();
```

## 与 Java 版本的对应关系

| .NET 模块 | Java 模块 | 说明 |
|----------|----------|------|
| OfdrwNet.Core | ofdrw-core | 核心数据结构和常量 |
| OfdrwNet.Packaging | ofdrw-pkg | 文件包装和容器 |
| OfdrwNet.Layout | ofdrw-layout | 布局引擎 |
| OfdrwNet.Reader | ofdrw-reader | 文档读取器 |
| OfdrwNet.Font | ofdrw-font | 字体处理 |
| OfdrwNet.Graphics | ofdrw-graphics2d | 图形绘制 |
| OfdrwNet.Crypto | ofdrw-crypto + ofdrw-gm | 加密和国密 |
| OfdrwNet.Sign | ofdrw-sign | 数字签名 |
| OfdrwNet.Converter | ofdrw-converter | 格式转换 |
| OfdrwNet.Tools | ofdrw-tool | 工具集 |

## 许可证

本项目采用 Apache 2.0 许可证，与原 Java 版本保持一致。
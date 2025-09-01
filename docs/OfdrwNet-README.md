# OFDRW.NET - OFD文档处理库

OFDRW.NET 是一个完整的OFD（开放文档格式）文档处理解决方案，基于.NET 8构建，提供文档创建、编辑、渲染、转换和数字签名等功能。

## 功能特性

- 📄 **文档创建** - 支持从零开始创建OFD文档
- 🔧 **文档编辑** - 修改现有OFD文档内容
- 📖 **文档读取** - 解析和提取OFD文档信息
- 🎨 **页面布局** - 流式布局和固定布局支持
- 🖼️ **图形绘制** - 矢量图形、图片和文本渲染
- 🔄 **格式转换** - OFD与PDF、图片格式互转
- ✍️ **数字签名** - 符合GB/T 33190-2016标准的电子签名
- 💧 **水印注解** - 添加水印、注释等元素
- 📎 **附件管理** - 文档附件的添加和管理
- 🔤 **字体处理** - 完整的字体管理和映射

## 快速开始

### 安装

```bash
dotnet add package OfdrwNet
```

### 创建简单文档

```csharp
using OfdrwNet;

// 方法1: 使用便捷API
await OfdrwHelper.CreateHelloWorldAsync("hello.ofd", "你好，OFDRW.NET！");

// 方法2: 使用完整API
using var doc = new OFDDoc("document.ofd");
doc.Add(new TextParagraph("Hello World!")
{
    FontSize = 12,
    Position = new Position(50, 50)
});
await doc.CloseAsync();
```

### 读取文档

```csharp
// 打开文档
using var reader = await OfdrwHelper.OpenDocumentAsync("document.ofd");

// 获取文档信息
var info = reader.GetDocumentInfo();
Console.WriteLine($"页面数: {info.PageCount}");
Console.WriteLine($"标题: {info.Title}");

// 获取页面信息
var pageInfo = reader.GetPageInfo(1); // 第1页
Console.WriteLine($"页面大小: {pageInfo.Width}x{pageInfo.Height}mm");
```

### 编辑文档

```csharp
using var doc = await OfdrwHelper.EditDocumentAsync("input.ofd", "output.ofd");

// 添加内容到现有页面
var additionPage = doc.GetAdditionPage(1);
additionPage.Add(new TextParagraph("新增内容"));

// 添加水印
await doc.AddWatermarkAsync(new Watermark("机密文档")
{
    Opacity = 0.3,
    Rotation = 45
});

await doc.CloseAsync();
```

### 格式转换

```csharp
// OFD转PDF
await OfdrwHelper.ConvertToPdfAsync("document.ofd", "document.pdf");

// OFD转图片
await OfdrwHelper.ConvertToImagesAsync("document.ofd", "images/", "png", 300);

// PDF转OFD
await OfdrwHelper.ConvertFromPdfAsync("document.pdf", "document.ofd");
```

### 数字签名

```csharp
// 创建签名容器
var signContainer = new DefaultSignatureContainer("证书路径", "密码");

// 对文档签名
await OfdrwHelper.SignDocumentAsync(
    "unsigned.ofd", 
    "signed.ofd", 
    signContainer,
    SignMode.ContinueSign
);

// 验证签名
var results = await OfdrwHelper.VerifySignaturesAsync("signed.ofd");
foreach (var result in results)
{
    Console.WriteLine($"签名ID: {result.SignID}, 有效: {result.IsValid}");
}
```

## 详细API

### OFDDoc - 主要文档类

```csharp
// 构造函数
var doc = new OFDDoc("output.ofd");                    // 新建文档
var doc = new OFDDoc(stream);                          // 输出到流
var doc = new OFDDoc(reader, "output.ofd");           // 编辑模式

// 页面布局
doc.SetDefaultPageLayout(PageLayout.A4());

// 内容添加
doc.Add(new TextParagraph("文本"));                    // 流式布局
doc.AddVirtualPage(new VirtualPage());                // 固定布局

// 页面操作
var page = doc.GetAdditionPage(1);                     // 获取追加页面

// 注解和水印
await doc.AddAnnotationAsync(1, annotation);          // 添加注解
await doc.AddWatermarkAsync(watermark);               // 添加水印

// 附件管理
await doc.AddAttachmentAsync(attachment);             // 添加附件
await doc.DeleteAttachmentAsync("附件名");            // 删除附件

// 文档保存
await doc.CloseAsync();
```

### OfdrwHelper - 便捷API

```csharp
// 文档创建和打开
var doc = OfdrwHelper.CreateDocument("path");
var reader = await OfdrwHelper.OpenDocumentAsync("path");
var doc = await OfdrwHelper.EditDocumentAsync("in", "out");

// 格式转换
await OfdrwHelper.ConvertToPdfAsync("ofd", "pdf");
await OfdrwHelper.ConvertToImagesAsync("ofd", "dir");
await OfdrwHelper.ConvertFromPdfAsync("pdf", "ofd");

// 数字签名
await OfdrwHelper.SignDocumentAsync("in", "out", container);
var results = await OfdrwHelper.VerifySignaturesAsync("path");

// 文档信息
var info = await OfdrwHelper.GetDocumentInfoAsync("path");
var count = await OfdrwHelper.GetPageCountAsync("path");
var valid = await OfdrwHelper.IsValidOfdDocumentAsync("path");
```

### 全局配置

```csharp
// 字体配置
OfdrwConfiguration.DefaultFont = "SimSun";
OfdrwConfiguration.AddFontMapping("宋体", "SimSun");

// 性能配置
OfdrwConfiguration.EnableParallelProcessing = true;
OfdrwConfiguration.MaxDegreeOfParallelism = 4;

// 渲染配置
OfdrwConfiguration.DefaultDpi = 300;
OfdrwConfiguration.EnableAntiAliasing = true;

// 自定义配置
OfdrwConfiguration.SetCustomSetting("MyKey", "MyValue");
var value = OfdrwConfiguration.GetCustomSetting("MyKey", "Default");
```

## 页面布局

### 流式布局

```csharp
using var doc = new OFDDoc("flow-layout.ofd");

// 添加段落
doc.Add(new TextParagraph("第一段文本") { FontSize = 12 });
doc.Add(new TextParagraph("第二段文本") { FontSize = 10 });

// 添加图片
doc.Add(new ImageDiv("image.jpg") { Width = 100, Height = 80 });

await doc.CloseAsync();
```

### 固定布局

```csharp
using var doc = new OFDDoc("fixed-layout.ofd");

// 创建虚拟页面
var page = new VirtualPage(210, 297); // A4大小

// 精确定位元素
page.Add(new TextParagraph("标题") 
{
    Position = new Position(50, 50),
    FontSize = 16
});

page.Add(new ImageDiv("logo.png")
{
    Position = new Position(150, 50),
    Width = 50,
    Height = 30
});

doc.AddVirtualPage(page);
await doc.CloseAsync();
```

## 数字签名

### 创建签名

```csharp
// 使用默认签名容器
var container = new DefaultSignatureContainer();
container.SetCertificate("certificate.pfx", "password");

using var signer = new OFDSigner("input.ofd", "signed.ofd", container);
await signer.SignAsync();
```

### 验证签名

```csharp
using var verifier = new OFDSignatureVerifier("signed.ofd");

// 验证所有签名
var results = await verifier.VerifyAllAsync();

// 验证特定签名
var result = await verifier.VerifyAsync("SignID_001");

Console.WriteLine($"签名有效: {result.IsValid}");
Console.WriteLine($"签名时间: {result.SignTime}");
Console.WriteLine($"签名算法: {result.Algorithm}");
```

## 异常处理

```csharp
try
{
    using var doc = new OFDDoc("output.ofd");
    // ... 文档操作
    await doc.CloseAsync();
}
catch (OfdException ex)
{
    Console.WriteLine($"OFD操作异常: {ex.Message}");
}
catch (SignatureException ex)
{
    Console.WriteLine($"签名异常: {ex.Message}");
}
catch (ConvertException ex)
{
    Console.WriteLine($"转换异常: {ex.Message}");
}
```

## 性能优化

### 并行处理

```csharp
// 启用并行处理
OfdrwConfiguration.EnableParallelProcessing = true;
OfdrwConfiguration.MaxDegreeOfParallelism = Environment.ProcessorCount;

// 批量处理文档
var files = Directory.GetFiles("documents", "*.ofd");
await Parallel.ForEachAsync(files, async (file, ct) =>
{
    await OfdrwHelper.ConvertToPdfAsync(file, file.Replace(".ofd", ".pdf"));
});
```

### 内存管理

```csharp
// 配置内存缓存
OfdrwConfiguration.MemoryCacheSizeMB = 200;
OfdrwConfiguration.EnableFileCache = true;

// 处理大文档时使用using确保资源释放
using var reader = await OfdrwHelper.OpenDocumentAsync("large.ofd");
// ... 处理文档
```

## 许可证

本项目采用Apache 2.0许可证 - 详情请参见 [LICENSE](LICENSE) 文件。

## 贡献

欢迎贡献代码！请先阅读 [CONTRIBUTING.md](CONTRIBUTING.md) 了解贡献指南。

## 支持

如有问题请提交 [Issues](https://github.com/ofdrw/ofdrw-net/issues)。
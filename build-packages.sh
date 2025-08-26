#!/bin/bash
# OFDRW.NET NuGet 包构建和发布脚本

set -e

# 配置变量
VERSION=${VERSION:-"1.0.0"}
CONFIGURATION=${CONFIGURATION:-"Release"}
OUTPUT_DIR="./nupkgs"
NUGET_SOURCE=${NUGET_SOURCE:-"https://api.nuget.org/v3/index.json"}

echo "==========================================="
echo "OFDRW.NET NuGet 包构建脚本"
echo "版本: $VERSION"
echo "配置: $CONFIGURATION"
echo "输出目录: $OUTPUT_DIR"
echo "==========================================="

# 清理输出目录
echo "清理输出目录..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# 清理项目
echo "清理解决方案..."
dotnet clean -c $CONFIGURATION

# 恢复依赖
echo "恢复 NuGet 包..."
dotnet restore

# 构建解决方案
echo "构建解决方案..."
dotnet build -c $CONFIGURATION --no-restore

# 运行测试
echo "运行单元测试..."
dotnet test --no-build -c $CONFIGURATION --verbosity normal

# 打包各个项目
echo "创建 NuGet 包..."

# 核心包
dotnet pack OfdrwNet.Core/OfdrwNet.Core.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION
dotnet pack OfdrwNet.Packaging/OfdrwNet.Packaging.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION
dotnet pack OfdrwNet.Reader/OfdrwNet.Reader.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION
dotnet pack OfdrwNet.Layout/OfdrwNet.Layout.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION
dotnet pack OfdrwNet.Font/OfdrwNet.Font.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION
dotnet pack OfdrwNet.Crypto/OfdrwNet.Crypto.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION
dotnet pack OfdrwNet.Graphics/OfdrwNet.Graphics.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION
dotnet pack OfdrwNet.Converter/OfdrwNet.Converter.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION
dotnet pack OfdrwNet.Tools/OfdrwNet.Tools.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION
dotnet pack OfdrwNet.Sign/OfdrwNet.Sign.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION

# 主包（聚合包）
dotnet pack OfdrwNet/OfdrwNet.csproj -c $CONFIGURATION --no-build --output "$OUTPUT_DIR" /p:PackageVersion=$VERSION

echo "NuGet 包创建完成！"
echo "生成的包文件："
ls -la "$OUTPUT_DIR"/*.nupkg

# 验证包内容
echo ""
echo "验证主包内容..."
unzip -l "$OUTPUT_DIR/OfdrwNet.$VERSION.nupkg" | head -20

echo ""
echo "==========================================="
echo "包构建完成！"
echo "如需发布到 NuGet.org，请运行："
echo "dotnet nuget push \"$OUTPUT_DIR/*.nupkg\" --source \"$NUGET_SOURCE\" --api-key YOUR_API_KEY"
echo "==========================================="
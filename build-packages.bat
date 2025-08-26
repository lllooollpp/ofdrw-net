@echo off
REM OFDRW.NET NuGet 包构建和发布脚本 (Windows 版本)

setlocal enabledelayedexpansion

REM 配置变量
if "%VERSION%"=="" set VERSION=1.0.0
if "%CONFIGURATION%"=="" set CONFIGURATION=Release
set OUTPUT_DIR=.\nupkgs
if "%NUGET_SOURCE%"=="" set NUGET_SOURCE=https://api.nuget.org/v3/index.json

echo ===========================================
echo OFDRW.NET NuGet 包构建脚本
echo 版本: %VERSION%
echo 配置: %CONFIGURATION%
echo 输出目录: %OUTPUT_DIR%
echo ===========================================

REM 清理输出目录
echo 清理输出目录...
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

REM 清理项目
echo 清理解决方案...
dotnet clean -c %CONFIGURATION%
if errorlevel 1 goto :error

REM 恢复依赖
echo 恢复 NuGet 包...
dotnet restore
if errorlevel 1 goto :error

REM 构建解决方案
echo 构建解决方案...
dotnet build -c %CONFIGURATION% --no-restore
if errorlevel 1 goto :error

REM 运行测试
echo 运行单元测试...
dotnet test --no-build -c %CONFIGURATION% --verbosity normal
if errorlevel 1 goto :error

REM 打包各个项目
echo 创建 NuGet 包...

REM 核心包
dotnet pack OfdrwNet.Core\OfdrwNet.Core.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%
dotnet pack OfdrwNet.Packaging\OfdrwNet.Packaging.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%
dotnet pack OfdrwNet.Reader\OfdrwNet.Reader.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%
dotnet pack OfdrwNet.Layout\OfdrwNet.Layout.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%
dotnet pack OfdrwNet.Font\OfdrwNet.Font.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%
dotnet pack OfdrwNet.Crypto\OfdrwNet.Crypto.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%
dotnet pack OfdrwNet.Graphics\OfdrwNet.Graphics.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%
dotnet pack OfdrwNet.Converter\OfdrwNet.Converter.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%
dotnet pack OfdrwNet.Tools\OfdrwNet.Tools.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%
dotnet pack OfdrwNet.Sign\OfdrwNet.Sign.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%

REM 主包（聚合包）
dotnet pack OfdrwNet\OfdrwNet.csproj -c %CONFIGURATION% --no-build --output "%OUTPUT_DIR%" /p:PackageVersion=%VERSION%

if errorlevel 1 goto :error

echo NuGet 包创建完成！
echo 生成的包文件：
dir /b "%OUTPUT_DIR%\*.nupkg"

echo.
echo ===========================================
echo 包构建完成！
echo 如需发布到 NuGet.org，请运行：
echo dotnet nuget push "%OUTPUT_DIR%\*.nupkg" --source "%NUGET_SOURCE%" --api-key YOUR_API_KEY
echo ===========================================
goto :end

:error
echo 错误：构建过程中出现错误！
exit /b 1

:end
endlocal
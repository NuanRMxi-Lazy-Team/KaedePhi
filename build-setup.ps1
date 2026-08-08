# build-setup.ps1
# 自动构建 Inno Setup 安装包脚本（本地 IDE 使用）
# 从 Directory.Build.props 读取版本号，从 git 获取 commit hash

param(
    [string]$IsccPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    [string]$PublishDir = ""
)

$ErrorActionPreference = "Stop"

# 获取项目根目录（脚本所在目录）
$ProjectRoot = $PSScriptRoot
$PropsFile = Join-Path $ProjectRoot "Directory.Build.props"
$IssFile = Join-Path $ProjectRoot "SetupGenScript.iss"

# 检查必要文件是否存在
if (-not (Test-Path $PropsFile)) {
    Write-Error "未找到 Directory.Build.props 文件"
    exit 1
}

if (-not (Test-Path $IssFile)) {
    Write-Error "未找到 SetupGenScript.iss 文件"
    exit 1
}

if (-not (Test-Path $IsccPath)) {
    Write-Error "未找到 Inno Setup 编译器: $IsccPath"
    Write-Host "请通过 -IsccPath 参数指定 ISCC.exe 路径" -ForegroundColor Yellow
    exit 1
}

# 确定发布目录
if ([string]::IsNullOrEmpty($PublishDir)) {
    # 默认发布目录
    $PublishDir = Join-Path $ProjectRoot "KaedePhi.Tool.App\bin\PreRelease\net10.0\win-x64\publish"
}

# 检查发布目录是否存在
if (-not (Test-Path $PublishDir)) {
    Write-Error "发布目录不存在: $PublishDir"
    Write-Host "请先发布项目，或通过 -PublishDir 参数指定发布目录" -ForegroundColor Yellow
    exit 1
}

# 检查发布目录是否有内容
$PublishedFiles = Get-ChildItem $PublishDir -ErrorAction SilentlyContinue
if ($null -eq $PublishedFiles -or $PublishedFiles.Count -eq 0) {
    Write-Error "发布目录为空: $PublishDir"
    Write-Host "请先发布项目" -ForegroundColor Yellow
    exit 1
}

# 从 Directory.Build.props 读取版本号
Write-Host "正在读取版本号..." -ForegroundColor Cyan
$PropsContent = Get-Content $PropsFile -Raw
if ($PropsContent -match '<Version>([\d.]+)</Version>') {
    $BaseVersion = $Matches[1]
    Write-Host "  基础版本号: $BaseVersion" -ForegroundColor Green
} else {
    Write-Error "无法从 Directory.Build.props 读取版本号"
    exit 1
}

# 从 git 获取完整 commit hash
Write-Host "正在获取 git commit hash..." -ForegroundColor Cyan
try {
    $CommitHash = git -C $ProjectRoot rev-parse HEAD
    if ($LASTEXITCODE -ne 0) {
        throw "git 命令执行失败"
    }
    Write-Host "  Commit Hash: $CommitHash" -ForegroundColor Green
} catch {
    Write-Error "无法获取 git commit hash: $_"
    exit 1
}

# 组合完整版本号
$FullVersion = "$BaseVersion+$CommitHash"

Write-Host "`n配置信息:" -ForegroundColor Yellow
Write-Host "  版本号:     $FullVersion"
Write-Host "  项目目录:   $ProjectRoot"
Write-Host "  发布目录:   $PublishDir"

# 设置环境变量
$env:KAEPHI_VERSION = $FullVersion
$env:KAEPHI_PROJECT_DIR = $ProjectRoot
$env:KAEPHI_PUBLISH_DIR = $PublishDir

Write-Host "`n正在构建安装包..." -ForegroundColor Cyan

# 调用 Inno Setup 编译
& $IsccPath $IssFile

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n构建成功！" -ForegroundColor Green
} else {
    Write-Error "构建失败，退出码: $LASTEXITCODE"
    exit $LASTEXITCODE
}

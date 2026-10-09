# KaedePhi
我知晓，那无边的旅途。

## 前言
NuanR_Star Ciallo Team（以下简称“我们”）KaedePhi（以下简称“本软件”）其源码遵循[GNU LESSER GENERAL PUBLIC LICENSE 3.0](https://www.gnu.org/licenses/lgpl-3.0.html)开源协议发布，
作为一个刚刚起步的项目，我们不建议您将本软件代码fork进行自行二次开发，我们非常希望可以保持社区的集中性，不分化社区资源，
使使用者无需在众多分叉中寻找最合适的版本，或是担心某个分叉不再维护而导致的后续问题，
我们非常希望您向本软件的主分支提交pull request来参与开发，或是加入我们的聊天群来进行讨论，或是通过邮件来联系我们，来参与到本软件的开发中来，
总之，以上都是建议，我们完全遵循开源协议，感谢支持。

## 安装与运行
从 GitHub `Release` 下载应用安装包或便携版压缩包，也可以从源码编译。应用发布包采用框架依赖部署（FDD），不会捆绑 .NET 运行时。

### 安装运行时
- Windows：安装 [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)，再运行安装包或便携版中的 `KaedePhi.Tool.App.exe`。
- Linux：安装 [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)，解压 `linux-x64` 便携版后运行 `KaedePhi.Tool.App`。
- macOS：从 GitHub Release 下载 `KaedePhi-App-v*-macOS-x86_64-Installer.dmg`（Intel）或 `KaedePhi-App-v*-macOS-arm64-Installer.dmg`（Apple 芯片），打开后将 `KaedePhi.app` 拖入“应用程序”文件夹；首次运行前需安装 [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)。当前应用包未进行 Apple 开发者签名与公证，首次打开时请按住 Control 键点按应用并选择“打开”，再确认运行。
- 正式应用产物使用 `net10.0` FDD：提供 Windows x64、Linux x64 便携版，Windows x64 安装包，以及分别面向 Intel 和 Apple 芯片的 macOS `.dmg` 安装包。

## 注意事项
> [!CAUTION]
> <span style="color:red">**如果您使用本软件进行低质量创作，本软件将对您进行道德谴责，受限于开源协议，项目维护者无权阻止您的任何行为！**</span>  

> **本项目自1.2.0版本行为趋于稳定，欢迎各位开发者提出意见，但是在跨大版本更新时，仍然可能存在破坏性更改的可能。**

## CLI 使用
发布包中的 `KaedePhi.Tool.App` 同时提供 CLI 和 GUI。传入命令或 `--cli` 时使用 CLI，传入 `--gui` 时启动 GUI；交互式终端中直接运行也会进入 CLI。

```bash
# 查看命令和选项
KaedePhi.Tool.App --help

# 查看版本
KaedePhi.Tool.App version

# 将谱面转换为 PhiEdit .pec，输入格式会自动检测
KaedePhi.Tool.App convert --input input.json --target PhiEdit --output output.pec --format

# 转换为 Phigros v3 JSON
KaedePhi.Tool.App convert --input input.pec --target PhigrosV3 --output output.json

# 将事件渲染为 PNG，默认输出到输入文件旁的 render_output 目录
KaedePhi.Tool.App render --input input.json

# 使用工作区进行多步处理
KaedePhi.Tool.App load --input input.json --workspace demo
KaedePhi.Tool.App convert --workspace demo --target PhiEdit --output output.pec
KaedePhi.Tool.App workspace list
KaedePhi.Tool.App workspace clear --id demo
KaedePhi.Tool.App workspace clear --all

# 重置 CLI 与 GUI 共用的配置
KaedePhi.Tool.App config reset
```

从源码运行时，将上例中的程序名替换为 `dotnet run --project KaedePhi.Tool.App --`，例如：

```bash
dotnet run --project KaedePhi.Tool.App -- --help
```

## 配置与数据路径
应用使用 .NET 的本机应用数据目录：
- Windows：`%LOCALAPPDATA%\KaedePhi\config\config.yaml`、`%LOCALAPPDATA%\KaedePhi\workspaces`、`%LOCALAPPDATA%\KaedePhi\logs`
- Linux：通常为 `~/.local/share/KaedePhi/config/config.yaml`、`~/.local/share/KaedePhi/workspaces`、`~/.local/share/KaedePhi/logs`

配置文件由 CLI 和 GUI 共享。工作区只保存名为 `chart.json` 的原始谱面文件，工作区 ID 只允许字母、数字、下划线和连字符。

## 构建与测试
根目录 `global.json` 为 `dotnet test` 配置 Microsoft.Testing.Platform 测试运行器。

```bash
dotnet restore KaedePhi.sln
dotnet test KaedePhi.sln --configuration Release --no-restore
dotnet publish KaedePhi.Tool.App/KaedePhi.Tool.App.csproj \
  --configuration Release --framework net10.0 --runtime win-x64 --self-contained false
```

## .NET版本
- `KaedePhi.Core`：.NETStandard2.1、.NET8.0、.NET10.0
- `KaedePhi.Tool`：.NET8.0、.NET10.0
- `KaedePhi.Tool.App`：源码支持 .NET8.0、.NET10.0，官方应用发布目标为 .NET10.0
- `KaedePhi.Tool.Localization`：.NET8.0、.NET10.0

Core 的 `net10.0` 目标在 PhiFans、Phigros v3 和 PhiChain 的 JSON 编解码路径中使用 REDox；`net8.0` 与 `netstandard2.1` 继续使用 Newtonsoft.Json。为保持现有模型兼容和 PhiChain 流式输入行为，RePhiEdit 编解码及 PhiChain 流式反序列化仍使用 Newtonsoft.Json。目前 Newtonsoft 兼容包版本为 `1.0.1-preview`，打包稳定版 Core 会产生 NuGet `NU5104` 预览依赖警告；该包发布稳定版后应升级并清除此警告。

Core 和 Tool 的 NuGet 包分别使用 `Core-v<版本>`、`Tool-v<版本>` 标签发布，并保留 NuGet 标准的 `<包 ID>.<版本>.nupkg` 文件名；应用使用 `App-v<版本>` 标签发布。App 附件使用 `KaedePhi-App-v<版本>-<系统>-<架构>-<包类型>` 格式命名，例如 `KaedePhi-App-v1.2.0-macOS-arm64-Installer.dmg`。

## 限制说明
- FDD 便携版和安装版都要求先安装对应的 .NET 10 运行时，不能脱离运行时单独执行。
- macOS 安装包未进行 Apple 开发者签名与公证，首次启动需手动确认；Intel 与 Apple 芯片用户需下载对应架构的安装包。
- 目前正式应用产物提供 Windows x64、Linux x64 和 macOS x86_64/arm64；其他系统和架构需要自行编译验证。
- 部分目标格式不支持源格式的全部事件或缓动类型，转换时可能进行采样、拟合或压缩；大型谱面可尝试 `--stream` 降低内存占用。
- 项目仍处于早期阶段，字段、默认配置和转换行为可能变化；升级前请备份谱面和配置。

## 发布流程
GitHub Actions 是唯一权威发布入口，负责 Core、Tool 和 App 的标签、GitHub Release 及正式附件，并为 App 提供 Windows 安装程序以及分别面向 macOS x86_64 和 arm64 的 `.dmg`。GitLab CI 仅保留夜间 App 构建，不再创建发布和标签，避免两个平台并发发布导致版本、附件和标签不一致。正式 App 发布固定为 `net10.0` FDD；GitLab 夜间构建也使用同一目标框架。

## 招新
本项目需要更多人开发与维护，欢迎发送邮件到 nrlt@nuanr-mxi.com 来加入开发！  
也欢迎加入我的小群！QQ群号: 390530513

## TODO
- [x] RePhiEdit反序列化功能
- [x] RePhiEdit序列化功能
- [x] RePhiEdit基础父子线解绑
- [x] RePhiEdit旋转跟随父子线解绑
- [x] RePhiEdit层级合并
- [x] PhiEdit反序列化
- [x] PhiEdit序列化
- [x] PhiFans反序列化
- [x] PhiFans序列化  
- [x] PhiChain反序列化
- [x] PhiChain序列化
- [x] 本家谱面反序列化
- [x] 本家谱面序列化
- [x] App工具

## 开源许可证
[GNU LESSER GENERAL PUBLIC LICENSE 3.0](https://www.gnu.org/licenses/lgpl-3.0.html)

## Copyright
NuanR_Mxi Copyright © 2026 KaedePhi Project.  
NuanR_Star Copyright © 2026 KaedePhi Project.  
Kaede HikariN Copyright © 2026 KaedePhi Project.  
Kaede NuanR_Mxi Copyright © 2026 KaedePhi Project.  
Kaede NuanR_Star Copyright © 2026 KaedePhi Project.  
枫暖日明曦 Copyright © 2026 KaedePhi Project.  
枫暖日星辉 Copyright © 2026 KaedePhi Project.  
暖日明曦 Copyright © 2026 KaedePhi Project.  
暖日星辉 Copyright © 2026 KaedePhi Project.  
暖日 Copyright © 2026 KaedePhi Project.  
暖星 Copyright © 2026 KaedePhi Project.  
NuanR_Mxi Lazy Team Copyright © 2026 KaedePhi Project.  
NuanR_Star Lazy Team Copyright © 2026 KaedePhi Project.  
NuanR_Star Ciallo Team Copyright © 2026 KaedePhi Project.

## 免责声明
本软件与南京鸽游网络有限公司（厦门鸽游网络有限公司）无任何关联。  
本软件以及其维护者、贡献者不承担您使用本软件进行任何行为的责任。

## 致谢
[cmdysj](https://space.bilibili.com/252635690)  
[HLMC](https://space.bilibili.com/357681195)  
[不会特效の点缀星空](https://space.bilibili.com/1792961650)  
[PhiFans](https://github.com/PhiFans)  
[Ivan-1F](https://github.com/Ivan-1F)  
所有参与测试反馈的各位以及贡献者  
和屏幕前的你！

# 本仓库内 PNG、ICO 文件的授权及版权声明

本仓库内所使用的 PNG、ICO 等图像文件均由 MySxan 绘制，其著作权归 NuanR_Mxi 个人所有（不含字体文件）。

上述文件的使用授权范围仅限于以下用途：

1. 本项目及其相关内容；
2. 基于本项目产生的衍生作品；
3. NuanR_Star Ciallo Team 的相关项目及活动，但不包括其附属组织或关联团队，例如 MoeRain。

除上述授权范围外，未经 NuanR_Mxi 明确书面授权，任何个人或组织不得以任何形式使用、复制、修改、传播、再授权或用于其他商业或非商业用途。

禁止用途包括但不限于：

- 制作或开发其他软件、应用程序或游戏；
- 制作或运营其他网站、网页或相关服务；
- 将相关文件用于 AI 训练数据集、模型训练或其他人工智能相关用途；
- 将相关文件用于任何与本项目无关的商业或非商业项目。

本声明仅针对上述 PNG、ICO 等图像文件，其授权范围不因本仓库其他文件所采用的开源许可证或其他授权条款而自动扩大。

NuanR_Mxi All Rights Reserved.

# Font Awesome 7 Free Solid 字体授权声明

详细请参阅本仓库内 KaedePhi.Tool.App/Assets/Fonts/LICENSE.txt 文本内容。

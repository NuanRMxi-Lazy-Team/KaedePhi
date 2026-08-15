# KaedePhi
即使身处无人角落，我仍要继续向前，直到我无法前进。

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
- 正式应用产物仅发布 `net10.0` 的 `win-x64` 和 `linux-x64` FDD 包；安装版仅适用于 Windows x64。

> [!WARNING]
> <span style="color:yellow">**注意：此项目仍然处于早期阶段，字段名称与行为随时有可能更改，请斟酌后再使用！**</span>

> [!CAUTION]
> <span style="color:red">**如果您使用本软件进行低质量创作，本软件将对您进行道德谴责，受限于开源协议，项目维护者无权阻止您的任何行为！**</span>

> [!CAUTION]
> <span style="color:red">**本项目自0.4.1版本进行了大量架构重写，请自行检查对旧项目的兼容性，部分API被直接破坏，部分API改名并标记为了废弃。**</span>


## 支持格式
当前稳定支持导入和导出的格式如下：
- RePhiEdit JSON（`.json`）
- PhiEdit 谱面（`.pec`）
- Phigros v3 JSON（`.json`）
- PhiChain JSON（`.json`）

JSON 格式会根据内容自动检测，不能仅凭扩展名区分。PhiFans 和 Phigros v1 当前没有完整的导入导出实现，不属于稳定支持范围。

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
仓库固定使用稳定的 .NET SDK `10.0.302`，版本由根目录 `global.json` 约束。

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

当前仓库版本为 `0.4.5`。Core 和 Tool 的 NuGet 包分别使用 `Core-v0.4.5`、`Tool-v0.4.5` 标签发布；应用使用 `App-v0.4.5` 标签发布，并提供 `KaedePhi.Tool.App-net10.0-*-fdd.zip` 和 Windows 安装包。

## 限制说明
- FDD 便携版和安装版都要求先安装对应的 .NET 10 运行时，不能脱离运行时单独执行。
- 目前只提供 Windows x64 和 Linux x64 应用产物，其他系统和架构需要自行编译验证。
- 部分目标格式不支持源格式的全部事件或缓动类型，转换时可能进行采样、拟合或压缩；大型谱面可尝试 `--stream` 降低内存占用。
- 0.4.x 仍处于早期阶段，字段、默认配置和转换行为可能变化；升级前请备份谱面和配置。

## 发布流程
GitHub Actions 是唯一权威发布入口，负责 Core、Tool 和 App 的标签、GitHub Release 及正式附件。GitLab CI 仅保留夜间 App 构建，不再创建发布和标签，避免两个平台并发发布导致版本、附件和标签不一致。正式 App 发布固定为 `net10.0` FDD；GitLab 夜间构建也使用同一目标框架。

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
- [ ] ~~CLI工具~~（已合并为App工具）
- [ ] ~~GUI工具~~（已合并为App工具）
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

# 本仓库内PNG、ICO文件授权情况
此类文件由 MySxan 绘制，版权归 NuanR_Mxi 个人所有，授权使用范围仅限于本项目、本项目衍生品以及NuanR_Star Ciallo Team（不含附属组，如MoeRain）。  
禁止未经授权用于其他商业或非商业用途。包括但不限于：  
制作其它软件、游戏、网站、AI训练数据集。  
NuanR_Mxi All Rights Reserved.

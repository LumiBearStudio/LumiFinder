<h1 align="center">
  LumiFinder
</h1>

<p align="center">
  <strong>macOS Finder 的 Miller Columns,为 Windows 重构。</strong><br>
  为转移到 Windows 后仍然怀念列视图的高级用户而生的文件浏览器。
</p>

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/releases/latest"><img src="https://img.shields.io/github/v/release/LumiBearStudio/LumiFinder?style=for-the-badge&label=Latest" alt="最新版本"></a>
  <a href="../LICENSE"><img src="https://img.shields.io/github/license/LumiBearStudio/LumiFinder?style=for-the-badge" alt="许可证"></a>
  <a href="https://github.com/sponsors/LumiBearStudio"><img src="https://img.shields.io/badge/Sponsor-%E2%9D%A4-ff69b4?style=for-the-badge&logo=github-sponsors" alt="赞助"></a>
</p>

<p align="center">
  <a href="../README.md">English</a> |
  <a href="README.ko.md">한국어</a> |
  <a href="README.ja.md">日本語</a> |
  <strong>简体中文</strong> |
  <a href="README.zh-TW.md">繁體中文</a> |
  <a href="README.de.md">Deutsch</a> |
  <a href="README.es.md">Español</a> |
  <a href="README.fr.md">Français</a> |
  <a href="README.pt.md">Português</a>
</p>

---

![Miller Columns 导航](miller-columns.gif)

> **以本应有的方式浏览文件夹层级。**
> 单击文件夹,其内容会出现在下一列。当前位置、来源路径、目标路径 — 一目了然。无需再前进/后退点击。

![LumiFinder — Miller Columns 实战](hero.png)

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/stargazers"><img src="https://img.shields.io/github/stars/LumiBearStudio/LumiFinder?style=social" alt="GitHub Stars"></a>
</p>
<p align="center">
  如果 LumiFinder 对您有用,请点 ⭐ — 帮助其他人发现这个项目!
</p>

---

## 为什么选择 LumiFinder?

| | Windows 资源管理器 | LumiFinder |
|---|---|---|
| **Miller Columns** | 无 | 分层多列导航 |
| **多标签页** | 仅 Windows 11 (基础) | 标签拖出/重新停靠/复制/会话恢复全支持 |
| **分屏视图** | 无 | 独立视图模式的双面板 |
| **预览面板** | 基础 | 10+ 文件类型 — 图像、视频、音频、代码、十六进制、字体、PDF |
| **键盘导航** | 受限 | 30+ 快捷键、即时搜索、键盘优先设计 |
| **批量重命名** | 无 | 正则、前缀/后缀、序号 |
| **撤销/重做** | 受限 | 完整操作历史 (深度可配置) |
| **自定义强调色** | 无 | 10 个预设 + 浅色/深色/系统 主题 |
| **布局密度** | 无 | 6 级行高 + 字体/图标独立缩放 |
| **远程连接** | 无 | FTP, FTPS, SFTP — 凭据保存 |
| **工作区** | 无 | 即时保存/恢复命名标签布局 |
| **文件搁板** | 无 | Yoink 风格的拖放暂存区 |
| **云存储状态** | 基础叠加 | 实时同步徽章 (OneDrive, iCloud, Dropbox) |
| **启动速度** | 大目录慢 | 可取消的异步加载 — 零延迟 |

---

## 主要功能

### Miller Columns — 一目了然

无需失去上下文即可浏览深层文件夹层级。每列代表一个层级 — 单击文件夹,其内容出现在下一列。始终能看到当前位置和来源路径。

- 拖动列分隔线调整宽度
- 自动均衡 (Ctrl+Shift+=) 或自动适应内容 (Ctrl+Shift+-)
- 平滑水平滚动,始终保持活动列可见
- 稳定的布局 — 键盘 ↑/↓ 导航不抖动

### 四种视图模式

- **Miller Columns** (Ctrl+1) — 分层导航,LumiFinder 的标志
- **详细信息** (Ctrl+2) — 名称/日期/类型/大小可排序表格
- **列表** (Ctrl+3) — 高密度多列布局,扫描大目录
- **图标** (Ctrl+4) — 4 种尺寸 (最大 256×256) 的网格视图

![详细信息视图 + 回收站标签](details.png)

### 多标签页 + 完整会话恢复

- 无限制标签 — 每个标签独立路径/视图模式/历史
- **标签拖出/重新停靠**: 拖动标签创建新窗口,或拖回停靠 — Chrome 风格幽灵指示器、半透明窗口反馈
- **标签复制**: 用相同路径/设置克隆标签
- 自动会话保存: 关闭并重启应用 — 所有标签保持原样

### 分屏视图 — 真正的双面板

![Miller Columns + 代码预览的分屏](2.png)

- 每个面板独立导航的并排浏览
- 每个面板可使用不同视图模式 (左 Miller, 右详情)
- 每个面板独立预览面板
- 面板间拖动文件以复制/移动

### 预览面板 — 打开前先看

按 **空格** 进入 Quick Look (macOS Finder 风格):

- **方向键 + 空格导航**: 不关闭 Quick Look 浏览文件
- **窗口大小记忆**: 保留上次大小
- **图像**: JPEG, PNG, GIF, BMP, WebP, TIFF — 分辨率和元数据
- **视频**: MP4, MKV, AVI, MOV, WEBM — 播放控制
- **音频**: MP3, AAC, M4A — 艺术家/专辑/时长
- **文本 + 代码**: 30+ 扩展名,语法显示
- **PDF**: 首页预览
- **字体**: 字形样本 + 元数据
- **十六进制二进制**: 开发者用原始字节视图
- **文件夹**: 大小/项数/创建日期
- **文件哈希**: SHA256 校验和 + 一键复制 (设置中启用)

### 键盘优先设计

为不离开键盘的用户准备的 30+ 快捷键:

| 快捷键 | 操作 |
|----------|--------|
| 方向键 | 在列和项目间导航 |
| Enter | 打开文件夹 / 执行文件 |
| Space | 切换预览面板 |
| Ctrl+L / Alt+D | 编辑地址栏 |
| Ctrl+F | 搜索 |
| Ctrl+C / X / V | 复制 / 剪切 / 粘贴 |
| Ctrl+Z / Y | 撤销 / 重做 |
| Ctrl+Shift+N | 新建文件夹 |
| F2 | 重命名 (多选时批量重命名) |
| Ctrl+T / W | 新标签 / 关闭标签 |
| Ctrl+Tab / Ctrl+Shift+Tab | 标签循环 |
| Ctrl+1-4 | 切换视图模式 |
| Ctrl+Shift+E | 切换分屏视图 |
| F6 | 切换分屏面板 |
| Ctrl+Shift+S | 保存工作区 |
| Ctrl+Shift+W | 工作区面板 |
| Ctrl+Shift+H | 切换文件扩展名显示 |
| Shift+F10 | 完整原生 Shell 上下文菜单 |
| Delete | 移到回收站 |

### 主题 + 自定义

![设置 — 外观: 自定义强调色 + 布局密度](4.png)

- **浅色 / 深色 / 系统** 主题跟随
- **10 种预设强调色** — 一键覆盖任何主题的强调色 (默认 Lumi Gold)
- **6 级布局密度** — XS / S / M / L / XL / XXL 行高
- **字体/图标独立缩放** — 与行密度分开
- **9 种语言**: 英语、韩语、日语、中文 (简体/繁体)、德语、西班牙语、法语、葡萄牙语 (BR)

### 一般设置

![设置 — 一般: 分屏 + 预览选项](3.png)

- **每个面板的启动行为** — 打开系统驱动器 / 恢复上次会话 / 自定义路径,左右独立
- **启动时视图模式** — 每个面板选择 Miller Columns / 详细信息 / 列表 / 图标
- **预览面板** — 启动时启用,或用空格切换
- **文件搁板** — Yoink 风格暂存区 (可选,会话间保留)
- **系统托盘** — 关闭按钮最小化到托盘

### 开发者工具

- **Git 状态徽章**: 每文件 Modified, Added, Deleted, Untracked
- **十六进制 dump 查看器**: 前 512 字节十六进制 + ASCII
- **终端集成**: Ctrl+` 在当前路径打开终端
- **远程连接**: 加密凭据存储的 FTP/FTPS/SFTP

### 云存储集成

- **同步状态徽章**: 仅云、已同步、待上传、同步中
- **OneDrive、iCloud、Dropbox** 自动检测
- **智能缩略图**: 使用缓存预览 — 不触发意外下载

### 智能搜索

- **结构化查询**: `type:image`, `size:>100MB`, `date:today`, `ext:.pdf`
- **即时搜索**: 在任何列开始输入即可立即过滤
- **后台处理**: 搜索不会冻结 UI

### 工作区 — 保存/恢复标签布局

- **保存当前标签**: 右击标签 → "保存标签布局..." 或按 Ctrl+Shift+S
- **即时恢复**: 单击侧边栏工作区按钮或 Ctrl+Shift+W
- **管理工作区**: 恢复/重命名/删除已保存的布局
- 切换工作上下文的最佳选择 — "开发"、"图片编辑"、"文档"

### 文件搁板

- macOS Yoink 风格的拖放暂存区
- 浏览时把文件拖入搁板,需要时拖到目标位置
- 删除搁板项只移除引用 — 永远不动原始文件
- 默认禁用 — 在 **设置 > 一般 > 记住搁板项** 启用

---

## 性能

为速度而设计。每文件夹 10,000+ 项目测试通过。

- 处处异步 I/O — UI 线程从不阻塞
- 最小开销的批量属性更新
- 防抖动选择 — 快速导航时避免冗余操作
- 每标签缓存 — 即时标签切换,无需重新渲染
- 带 SemaphoreSlim 节流的并发缩略图加载

---

## 系统要求

| | |
|---|---|
| **操作系统** | Windows 10 1903+ / Windows 11 |
| **架构** | x64, ARM64 |
| **运行时** | Windows App SDK 1.8 (.NET 8) |
| **推荐** | Windows 11 (Mica 背景) |

---

## 从源码构建

```bash
# 前置要求: Visual Studio 2022 + .NET Desktop + WinUI 3 工作负载

# 克隆
git clone https://github.com/LumiBearStudio/LumiFinder.git
cd LumiFinder

# 构建
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -p:Platform=x64

# 运行单元测试
dotnet test src/LumiFiles/LumiFiles.Tests/LumiFiles.Tests.csproj -p:Platform=x64
```

> **注意**: WinUI 3 应用无法用 `dotnet run` 启动。请使用 **Visual Studio F5** (需要 MSIX 打包)。

---

## 贡献

发现 bug? 有功能请求? [创建 Issue](https://github.com/LumiBearStudio/LumiFinder/issues) — 欢迎所有反馈。

构建设置/编码规范/PR 指南见 [CONTRIBUTING.md](../CONTRIBUTING.md)。

---

## 支持项目

如果 LumiFinder 改善了您的文件管理:

- **[GitHub Sponsors](https://github.com/sponsors/LumiBearStudio)** — 请我喝杯咖啡、汉堡或牛排
- **给这个仓库 ⭐** — 帮助其他人发现
- **分享** — 给 Windows 上怀念 macOS Finder 的同事
- **报告 bug** — 每一个 issue 让 LumiFinder 更稳定

---

## 隐私 + 遥测

LumiFinder 使用 [Sentry](https://sentry.io) **仅用于崩溃报告** — 您可以关闭它。

- **收集**: 异常类型、堆栈跟踪、操作系统版本、应用版本
- **不收集**: 文件名、文件夹路径、浏览历史、个人识别信息
- **无使用分析、无跟踪、无广告**
- 崩溃报告中的所有文件路径在发送前自动清理
- `SendDefaultPii = false` — 无 IP 地址或用户标识符
- **退出**: 设置 > 高级 > "崩溃报告" 切换以完全禁用
- 源代码开放 — 在 [`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs) 验证

详情见 [隐私政策](../PRIVACY.md)。

---

## 许可证

本项目使用 [GNU General Public License v3.0](../LICENSE) 许可证。

**商标**: "LumiFinder" 名称和官方徽标是 LumiBear Studio 的商标。Fork 必须使用不同的名称和徽标。完整商标政策见 [LICENSE.md](../LICENSE.md)。

---

<p align="center">
  <a href="https://github.com/sponsors/LumiBearStudio">赞助</a> ·
  <a href="../PRIVACY.md">隐私政策</a> ·
  <a href="../OpenSourceLicenses.md">开源许可证</a> ·
  <a href="https://github.com/LumiBearStudio/LumiFinder/issues">Bug 报告 + 功能请求</a>
</p>

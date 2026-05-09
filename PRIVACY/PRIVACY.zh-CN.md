# LumiFinder — 隐私政策

**最后更新: 2026年5月9日**

<p align="center">
  <a href="../PRIVACY.md">English</a> |
  <a href="PRIVACY.ko.md">한국어</a> |
  <a href="PRIVACY.ja.md">日本語</a> |
  <strong>简体中文</strong> |
  <a href="PRIVACY.zh-TW.md">繁體中文</a> |
  <a href="PRIVACY.de.md">Deutsch</a> |
  <a href="PRIVACY.es.md">Español</a> |
  <a href="PRIVACY.fr.md">Français</a> |
  <a href="PRIVACY.pt.md">Português</a>
</p>

---

## 概述

LumiFinder("本应用")是 LumiBear Studio 开发的 Windows 文件浏览器。我们致力于保护您的隐私。本政策说明我们收集哪些数据、如何保护以及您如何控制。

## 我们收集的数据

### 崩溃报告 (Sentry)

本应用使用 [Sentry](https://sentry.io) 进行自动崩溃报告。当应用崩溃或发生未处理的错误时,**可能**发送以下数据:

- **错误详情**: 异常类型、消息和堆栈跟踪
- **设备信息**: 操作系统版本、CPU 架构、崩溃时的内存使用情况
- **应用信息**: 应用版本、运行时版本、构建配置

崩溃报告**仅**用于识别和修复 bug。**不**包括:

- 文件名、文件夹名或文件内容
- 用户帐户信息
- 浏览历史或导航路径
- 任何可识别个人身份的信息 (PII)

### 崩溃报告中的隐私保护

在发送任何崩溃报告之前,自动应用多层 PII 清洗:

- **用户名屏蔽** — Windows 用户文件夹路径(`C:\Users\<您的用户名>\...`)被检测,用户名部分在传输前被替换。同样适用于 UNC 路径(`\\服务器\共享\Users\<用户名>\...`)。
- **`SendDefaultPii = false`** — 完全禁用 Sentry SDK 自动收集 IP 地址、服务器名和用户标识符。
- **不含文件内容** — 堆栈跟踪不含文件或文件夹内容,仅包含行号和方法名。

您可以在开源代码中亲自验证实现:
[`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

### 本地设置

本应用使用 Windows `ApplicationData.LocalSettings` 在您的设备上本地存储用户偏好(主题、语言、最近文件夹、收藏夹、自定义强调色等)。此数据**永远不会**传输到任何服务器。

## 我们不收集的数据

- 不收集个人信息(姓名、电子邮件、地址)
- 不收集文件系统内容或文件元数据
- 不收集使用分析或遥测
- 不收集位置数据
- 不收集广告标识符
- 不与第三方共享营销数据

## 网络访问

本应用仅在以下情况需要互联网:

- **崩溃报告** (Sentry) — 自动错误报告,可禁用 (见下方"如何选择退出")
- **FTP / FTPS / SFTP 连接** — 仅在用户明确配置时
- **NuGet 包恢复** — 仅在开发构建时(终端用户不会执行)

## 如何选择退出崩溃报告

无需断开互联网即可在应用内直接禁用崩溃报告:

1. 打开**设置** (侧边栏左下角)
2. 导航到**高级**
3. 关闭**崩溃报告**开关

更改立即生效。退出后,在任何情况下都不会发送崩溃报告。Sentry 服务器上已存在的过去报告仍将按标准 90 天保留计划过期。

## 数据存储和保留

- **Sentry 服务器**: 崩溃报告存储在 Sentry 的**德国法兰克福(欧盟)**数据中心 — 为符合 GDPR 而选择。报告在 **90 天**后自动删除。
- **本地设置**: 仅存储在您的设备上。卸载应用时一并删除。

## 作为数据处理者的 Sentry (GDPR)

根据 GDPR,Sentry 作为崩溃报告的数据处理者运作。Sentry 自身的隐私实践和安全措施详情:

- **Sentry 隐私政策**: [https://sentry.io/privacy/](https://sentry.io/privacy/)
- **Sentry 安全性**: [https://sentry.io/security/](https://sentry.io/security/)
- **Sentry GDPR**: [https://sentry.io/legal/dpa/](https://sentry.io/legal/dpa/)

LumiBear Studio 已审核 Sentry 的数据处理条款,并选择了欧盟区域(`o4510949994266624.ingest.de.sentry.io`),确保崩溃数据不会在没有适当保障的情况下离开欧洲经济区(EEA)。

## 儿童隐私

本应用不会有意收集 13 岁以下儿童的数据。本应用不针对儿童,也不收集任何可识别儿童身份的个人信息。

## 您的权利

由于我们不收集个人数据,因此没有可访问、修改或删除的个人数据。具体而言:

- **访问权/可携带权**: 不适用 — 我们没有保留任何个人数据。
- **删除权**: 不适用 — 我们没有保留任何个人数据。本地设置可通过卸载应用删除。
- **选择退出权**: 在 设置 > 高级 > 崩溃报告 中可用 (见上方"如何选择退出")。

## 开源

LumiFinder 在 GPL v3.0 许可下开源。欢迎您自己检查、审计或修改代码:

- **源代码**: [https://github.com/LumiBearStudio/LumiFinder](https://github.com/LumiBearStudio/LumiFinder)
- **使用的开源库**: 见 [OpenSourceLicenses.md](../OpenSourceLicenses.md)

## 联系

如对本隐私政策有疑问、发现违规或希望行使上述权利:

- **GitHub Issues**: [https://github.com/LumiBearStudio/LumiFinder/issues](https://github.com/LumiBearStudio/LumiFinder/issues)
- **安全披露**: 见 [SECURITY.md](../SECURITY.md)

## 本政策的变更

随着应用发展或法律要求变化,我们可能会更新本政策。重大变更将通过 GitHub Releases 公布。每次更新都会更新本文档顶部的"最后更新"日期。版本历史在 [Git 历史](https://github.com/LumiBearStudio/LumiFinder/commits/main/PRIVACY.md)中永久可用。

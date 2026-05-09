# LumiFinder — 隱私政策

**最後更新: 2026年5月9日**

<p align="center">
  <a href="../PRIVACY.md">English</a> |
  <a href="PRIVACY.ko.md">한국어</a> |
  <a href="PRIVACY.ja.md">日本語</a> |
  <a href="PRIVACY.zh-CN.md">简体中文</a> |
  <strong>繁體中文</strong> |
  <a href="PRIVACY.de.md">Deutsch</a> |
  <a href="PRIVACY.es.md">Español</a> |
  <a href="PRIVACY.fr.md">Français</a> |
  <a href="PRIVACY.pt.md">Português</a>
</p>

---

## 概述

LumiFinder("本應用")是 LumiBear Studio 開發的 Windows 檔案瀏覽器。我們致力於保護您的隱私。本政策說明我們蒐集哪些資料、如何保護以及您如何控制。

## 我們蒐集的資料

### 當機回報 (Sentry)

本應用使用 [Sentry](https://sentry.io) 進行自動當機回報。當應用當機或發生未處理的錯誤時,**可能**傳送以下資料:

- **錯誤詳情**: 例外類型、訊息和呼叫堆疊
- **裝置資訊**: 作業系統版本、CPU 架構、當機時的記憶體使用情況
- **應用資訊**: 應用版本、執行階段版本、組建設定

當機回報**僅**用於識別和修正 bug。**不**包括:

- 檔案名稱、資料夾名稱或檔案內容
- 使用者帳戶資訊
- 瀏覽歷史或導覽路徑
- 任何可識別個人身分的資訊 (PII)

### 當機回報中的隱私保護

在傳送任何當機回報之前,自動套用多層 PII 清洗:

- **使用者名稱遮罩** — Windows 使用者資料夾路徑(`C:\Users\<您的使用者名稱>\...`)被偵測,使用者名稱部分在傳輸前被替換。同樣適用於 UNC 路徑(`\\伺服器\共用\Users\<使用者名稱>\...`)。
- **`SendDefaultPii = false`** — 完全停用 Sentry SDK 自動蒐集 IP 位址、伺服器名稱和使用者識別碼。
- **不含檔案內容** — 呼叫堆疊不含檔案或資料夾內容,僅包含行號和方法名稱。

您可以在開源程式碼中親自驗證實作:
[`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

### 本機設定

本應用使用 Windows `ApplicationData.LocalSettings` 在您的裝置上本地儲存使用者偏好(主題、語言、最近資料夾、我的最愛、自訂強調色等)。此資料**永遠不會**傳輸到任何伺服器。

## 我們不蒐集的資料

- 不蒐集個人資訊(姓名、電子郵件、地址)
- 不蒐集檔案系統內容或檔案中繼資料
- 不蒐集使用分析或遙測
- 不蒐集位置資料
- 不蒐集廣告識別碼
- 不與第三方共用行銷資料

## 網路存取

本應用僅在以下情況需要網際網路:

- **當機回報** (Sentry) — 自動錯誤回報,可停用 (見下方「如何選擇退出」)
- **FTP / FTPS / SFTP 連線** — 僅在使用者明確設定時
- **NuGet 套件還原** — 僅在開發組建時(終端使用者不會執行)

## 如何選擇退出當機回報

無需中斷網際網路即可在應用內直接停用當機回報:

1. 開啟**設定** (側邊欄左下角)
2. 導覽至**進階**
3. 關閉**當機回報**切換

變更立即生效。退出後,在任何情況下都不會傳送當機回報。Sentry 伺服器上已存在的過去回報仍將依標準 90 天保留排程過期。

## 資料儲存和保留

- **Sentry 伺服器**: 當機回報儲存在 Sentry 的**德國法蘭克福(歐盟)**資料中心 — 為符合 GDPR 而選擇。回報在 **90 天**後自動刪除。
- **本機設定**: 僅儲存在您的裝置上。解除安裝應用時一併刪除。

## 作為資料處理者的 Sentry (GDPR)

依 GDPR,Sentry 作為當機回報的資料處理者運作。Sentry 自身的隱私實務和安全措施詳情:

- **Sentry 隱私政策**: [https://sentry.io/privacy/](https://sentry.io/privacy/)
- **Sentry 安全性**: [https://sentry.io/security/](https://sentry.io/security/)
- **Sentry GDPR**: [https://sentry.io/legal/dpa/](https://sentry.io/legal/dpa/)

LumiBear Studio 已審核 Sentry 的資料處理條款,並選擇了歐盟區域(`o4510949994266624.ingest.de.sentry.io`),確保當機資料不會在沒有適當保障的情況下離開歐洲經濟區(EEA)。

## 兒童隱私

本應用不會有意蒐集 13 歲以下兒童的資料。本應用不針對兒童,也不蒐集任何可識別兒童身分的個人資訊。

## 您的權利

由於我們不蒐集個人資料,因此沒有可存取、修改或刪除的個人資料。具體而言:

- **存取權/可攜帶權**: 不適用 — 我們沒有保留任何個人資料。
- **刪除權**: 不適用 — 我們沒有保留任何個人資料。本機設定可透過解除安裝應用刪除。
- **選擇退出權**: 在 設定 > 進階 > 當機回報 中可用 (見上方「如何選擇退出」)。

## 開源

LumiFinder 在 GPL v3.0 授權下開源。歡迎您自己檢查、稽核或修改程式碼:

- **原始碼**: [https://github.com/LumiBearStudio/LumiFinder](https://github.com/LumiBearStudio/LumiFinder)
- **使用的開源函式庫**: 見 [OpenSourceLicenses.md](../OpenSourceLicenses.md)

## 連絡

如對本隱私政策有疑問、發現違規或希望行使上述權利:

- **GitHub Issues**: [https://github.com/LumiBearStudio/LumiFinder/issues](https://github.com/LumiBearStudio/LumiFinder/issues)
- **安全揭露**: 見 [SECURITY.md](../SECURITY.md)

## 本政策的變更

隨著應用發展或法律要求變化,我們可能會更新本政策。重大變更將透過 GitHub Releases 公布。每次更新都會更新本文件頂部的「最後更新」日期。版本歷史在 [Git 歷史](https://github.com/LumiBearStudio/LumiFinder/commits/main/PRIVACY.md)中永久可用。

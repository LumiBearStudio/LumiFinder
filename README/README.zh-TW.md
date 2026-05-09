<h1 align="center">
  LumiFinder
</h1>

<p align="center">
  <strong>macOS Finder 的 Miller Columns,為 Windows 重構。</strong><br>
  為轉移到 Windows 後仍然懷念列檢視的高級使用者而生的檔案瀏覽器。
</p>

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/releases/latest"><img src="https://img.shields.io/github/v/release/LumiBearStudio/LumiFinder?style=for-the-badge&label=Latest" alt="最新版本"></a>
  <a href="../LICENSE"><img src="https://img.shields.io/github/license/LumiBearStudio/LumiFinder?style=for-the-badge" alt="授權"></a>
  <a href="https://github.com/sponsors/LumiBearStudio"><img src="https://img.shields.io/badge/Sponsor-%E2%9D%A4-ff69b4?style=for-the-badge&logo=github-sponsors" alt="贊助"></a>
</p>

<p align="center">
  <a href="../README.md">English</a> |
  <a href="README.ko.md">한국어</a> |
  <a href="README.ja.md">日本語</a> |
  <a href="README.zh-CN.md">简体中文</a> |
  <strong>繁體中文</strong> |
  <a href="README.de.md">Deutsch</a> |
  <a href="README.es.md">Español</a> |
  <a href="README.fr.md">Français</a> |
  <a href="README.pt.md">Português</a>
</p>

---

![Miller Columns 導覽](miller-columns.gif)

> **以本應有的方式瀏覽資料夾層級。**
> 點擊資料夾,其內容會出現在下一欄。目前位置、來源路徑、目標路徑 — 一目瞭然。無需再前進/後退點擊。

![LumiFinder — Miller Columns 實戰](hero.png)

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/stargazers"><img src="https://img.shields.io/github/stars/LumiBearStudio/LumiFinder?style=social" alt="GitHub Stars"></a>
</p>
<p align="center">
  如果 LumiFinder 對您有用,請點 ⭐ — 幫助其他人發現這個專案!
</p>

---

## 為什麼選擇 LumiFinder?

| | Windows 檔案總管 | LumiFinder |
|---|---|---|
| **Miller Columns** | 無 | 階層多欄導覽 |
| **多分頁** | 僅 Windows 11 (基礎) | 分頁拖出/重新停靠/複製/工作階段還原全支援 |
| **分割檢視** | 無 | 獨立檢視模式的雙窗格 |
| **預覽窗格** | 基礎 | 10+ 檔案類型 — 圖片、影片、音訊、程式碼、十六進位、字型、PDF |
| **鍵盤導覽** | 受限 | 30+ 快捷鍵、即時搜尋、鍵盤優先設計 |
| **批次重新命名** | 無 | 正規式、前綴/後綴、序號 |
| **復原/重做** | 受限 | 完整操作歷史 (深度可設定) |
| **自訂強調色** | 無 | 10 個預設 + 淺色/深色/系統 主題 |
| **配置密度** | 無 | 6 級列高 + 字型/圖示獨立縮放 |
| **遠端連線** | 無 | FTP, FTPS, SFTP — 認證資訊儲存 |
| **工作區** | 無 | 即時儲存/還原命名分頁配置 |
| **檔案 Shelf** | 無 | Yoink 風格的拖放暫存區 |
| **雲端狀態** | 基礎重疊 | 即時同步徽章 (OneDrive, iCloud, Dropbox) |
| **啟動速度** | 大目錄慢 | 可取消的非同步載入 — 零延遲 |

---

## 主要功能

### Miller Columns — 一目瞭然

無需失去脈絡即可瀏覽深層資料夾階層。每欄代表一個層級 — 點擊資料夾,其內容出現在下一欄。始終能看到目前位置和來源路徑。

- 拖動欄分隔線調整寬度
- 自動均衡 (Ctrl+Shift+=) 或自動配合內容 (Ctrl+Shift+-)
- 平滑水平捲動,始終保持作用中欄可見
- 穩定配置 — 鍵盤 ↑/↓ 導覽不抖動

### 四種檢視模式

- **Miller Columns** (Ctrl+1) — 階層導覽,LumiFinder 的標誌
- **詳細資料** (Ctrl+2) — 名稱/日期/類型/大小可排序表格
- **清單** (Ctrl+3) — 高密度多欄配置,掃描大目錄
- **圖示** (Ctrl+4) — 4 種尺寸 (最大 256×256) 的格線檢視

![詳細資料檢視 + 資源回收筒分頁](details.png)

### 多分頁 + 完整工作階段還原

- 無限制分頁 — 每個分頁獨立路徑/檢視模式/歷史
- **分頁拖出/重新停靠**: 拖動分頁建立新視窗,或拖回停靠 — Chrome 風格幽靈指示器、半透明視窗回饋
- **分頁複製**: 用相同路徑/設定複製分頁
- 自動工作階段儲存: 關閉並重啟應用 — 所有分頁保持原樣

### 分割檢視 — 真正的雙窗格

![Miller Columns + 程式碼預覽的分割檢視](2.png)

- 每個窗格獨立導覽的並排瀏覽
- 每個窗格可使用不同檢視模式 (左 Miller, 右詳情)
- 每個窗格獨立預覽窗格
- 窗格間拖動檔案以複製/移動

### 預覽窗格 — 開啟前先看

按 **空白鍵** 進入 Quick Look (macOS Finder 風格):

- **方向鍵 + 空白鍵導覽**: 不關閉 Quick Look 瀏覽檔案
- **視窗大小記憶**: 保留上次大小
- **圖片**: JPEG, PNG, GIF, BMP, WebP, TIFF — 解析度和中繼資料
- **影片**: MP4, MKV, AVI, MOV, WEBM — 播放控制
- **音訊**: MP3, AAC, M4A — 演出者/專輯/長度
- **文字 + 程式碼**: 30+ 副檔名,語法顯示
- **PDF**: 首頁預覽
- **字型**: 字符樣本 + 中繼資料
- **十六進位二進位**: 開發者用原始位元組檢視
- **資料夾**: 大小/項目數/建立日期
- **檔案雜湊**: SHA256 校驗 + 一鍵複製 (設定中啟用)

### 鍵盤優先設計

為不離開鍵盤的使用者準備的 30+ 快捷鍵:

| 快捷鍵 | 操作 |
|----------|--------|
| 方向鍵 | 在欄和項目間導覽 |
| Enter | 開啟資料夾 / 執行檔案 |
| Space | 切換預覽窗格 |
| Ctrl+L / Alt+D | 編輯網址列 |
| Ctrl+F | 搜尋 |
| Ctrl+C / X / V | 複製 / 剪下 / 貼上 |
| Ctrl+Z / Y | 復原 / 重做 |
| Ctrl+Shift+N | 新資料夾 |
| F2 | 重新命名 (多選時批次重新命名) |
| Ctrl+T / W | 新分頁 / 關閉分頁 |
| Ctrl+Tab / Ctrl+Shift+Tab | 分頁循環 |
| Ctrl+1-4 | 切換檢視模式 |
| Ctrl+Shift+E | 切換分割檢視 |
| F6 | 切換分割窗格 |
| Ctrl+Shift+S | 儲存工作區 |
| Ctrl+Shift+W | 工作區面板 |
| Ctrl+Shift+H | 切換檔案副檔名顯示 |
| Shift+F10 | 完整原生 Shell 內容功能表 |
| Delete | 移到資源回收筒 |

### 主題 + 自訂

![設定 — 外觀: 自訂強調色 + 配置密度](4.png)

- **淺色 / 深色 / 系統** 主題跟隨
- **10 種預設強調色** — 一鍵覆蓋任何主題的強調色 (預設 Lumi Gold)
- **6 級配置密度** — XS / S / M / L / XL / XXL 列高
- **字型/圖示獨立縮放** — 與列密度分開
- **9 種語言**: 英文、韓文、日文、中文 (簡體/繁體)、德文、西班牙文、法文、葡萄牙文 (BR)

### 一般設定

![設定 — 一般: 分割檢視 + 預覽選項](3.png)

- **每個窗格的啟動行為** — 開啟系統磁碟機 / 還原上次工作階段 / 自訂路徑,左右獨立
- **啟動時檢視模式** — 每個窗格選擇 Miller Columns / 詳細資料 / 清單 / 圖示
- **預覽窗格** — 啟動時啟用,或用空白鍵切換
- **檔案 Shelf** — Yoink 風格暫存區 (可選,工作階段間保留)
- **系統匣** — 關閉按鈕最小化到系統匣

### 開發者工具

- **Git 狀態徽章**: 每檔案 Modified, Added, Deleted, Untracked
- **十六進位 dump 檢視器**: 前 512 位元組十六進位 + ASCII
- **終端機整合**: Ctrl+` 在目前路徑開啟終端機
- **遠端連線**: 加密認證資訊儲存的 FTP/FTPS/SFTP

### 雲端儲存整合

- **同步狀態徽章**: 僅雲端、已同步、待上傳、同步中
- **OneDrive、iCloud、Dropbox** 自動偵測
- **智慧縮圖**: 使用快取預覽 — 不觸發意外下載

### 智慧搜尋

- **結構化查詢**: `type:image`, `size:>100MB`, `date:today`, `ext:.pdf`
- **即時搜尋**: 在任何欄開始輸入即可立即篩選
- **背景處理**: 搜尋不會凍結 UI

### 工作區 — 儲存/還原分頁配置

- **儲存目前分頁**: 右擊分頁 → "儲存分頁配置..." 或按 Ctrl+Shift+S
- **即時還原**: 點擊側邊欄工作區按鈕或 Ctrl+Shift+W
- **管理工作區**: 還原/重新命名/刪除已儲存的配置
- 切換工作脈絡的最佳選擇 — "開發"、"圖片編輯"、"文件"

### 檔案 Shelf

- macOS Yoink 風格的拖放暫存區
- 瀏覽時把檔案拖入 Shelf,需要時拖到目標位置
- 刪除 Shelf 項目只移除參照 — 永遠不動原始檔案
- 預設停用 — 在 **設定 > 一般 > 記住 Shelf 項目** 啟用

---

## 效能

為速度而設計。每資料夾 10,000+ 項目測試通過。

- 處處非同步 I/O — UI 執行緒從不封鎖
- 最小負荷的批次屬性更新
- 防抖動選取 — 快速導覽時避免重複作業
- 每分頁快取 — 即時分頁切換,無需重新繪製
- 帶 SemaphoreSlim 節流的並行縮圖載入

---

## 系統需求

| | |
|---|---|
| **作業系統** | Windows 10 1903+ / Windows 11 |
| **架構** | x64, ARM64 |
| **執行階段** | Windows App SDK 1.8 (.NET 8) |
| **建議** | Windows 11 (Mica 背景) |

---

## 從原始碼建置

```bash
# 前置需求: Visual Studio 2022 + .NET Desktop + WinUI 3 工作負載

# 複製
git clone https://github.com/LumiBearStudio/LumiFinder.git
cd LumiFinder

# 建置
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -p:Platform=x64

# 執行單元測試
dotnet test src/LumiFiles/LumiFiles.Tests/LumiFiles.Tests.csproj -p:Platform=x64
```

> **注意**: WinUI 3 應用無法用 `dotnet run` 啟動。請使用 **Visual Studio F5** (需要 MSIX 封裝)。

---

## 貢獻

發現 bug? 有功能請求? [建立 Issue](https://github.com/LumiBearStudio/LumiFinder/issues) — 歡迎所有意見。

建置設定/編碼規範/PR 指南見 [CONTRIBUTING.md](../CONTRIBUTING.md)。

---

## 支援專案

如果 LumiFinder 改善了您的檔案管理:

- **[GitHub Sponsors](https://github.com/sponsors/LumiBearStudio)** — 請我喝杯咖啡、漢堡或牛排
- **給這個儲存庫 ⭐** — 幫助其他人發現
- **分享** — 給 Windows 上懷念 macOS Finder 的同事
- **回報 bug** — 每一個 issue 讓 LumiFinder 更穩定

---

## 隱私 + 遙測

LumiFinder 使用 [Sentry](https://sentry.io) **僅用於當機回報** — 您可以關閉它。

- **收集**: 例外類型、呼叫堆疊、作業系統版本、應用程式版本
- **不收集**: 檔案名稱、資料夾路徑、瀏覽歷史、個人識別資訊
- **無使用分析、無追蹤、無廣告**
- 當機回報中的所有檔案路徑在傳送前自動清理
- `SendDefaultPii = false` — 無 IP 位址或使用者識別碼
- **退出**: 設定 > 進階 > "當機回報" 切換以完全停用
- 原始碼開放 — 在 [`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs) 驗證

詳情見 [隱私權政策](../PRIVACY.md)。

---

## 授權

本專案使用 [GNU General Public License v3.0](../LICENSE) 授權。

**商標**: "LumiFinder" 名稱和官方標誌是 LumiBear Studio 的商標。Fork 必須使用不同的名稱和標誌。完整商標政策見 [LICENSE.md](../LICENSE.md)。

---

<p align="center">
  <a href="https://github.com/sponsors/LumiBearStudio">贊助</a> ·
  <a href="../PRIVACY.md">隱私權政策</a> ·
  <a href="../OpenSourceLicenses.md">開源授權</a> ·
  <a href="https://github.com/LumiBearStudio/LumiFinder/issues">Bug 回報 + 功能請求</a>
</p>

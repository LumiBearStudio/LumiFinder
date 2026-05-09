<h1 align="center">
  LumiFinder
</h1>

<p align="center">
  <strong>macOS Finder's Miller Columns, reimagined for Windows.</strong><br>
  For power users who switched to Windows but never stopped missing column view.
</p>

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/releases/latest"><img src="https://img.shields.io/github/v/release/LumiBearStudio/LumiFinder?style=for-the-badge&label=Latest" alt="Latest Release"></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/LumiBearStudio/LumiFinder?style=for-the-badge" alt="License"></a>
  <a href="https://github.com/sponsors/LumiBearStudio"><img src="https://img.shields.io/badge/Sponsor-%E2%9D%A4-ff69b4?style=for-the-badge&logo=github-sponsors" alt="Sponsor"></a>
</p>

<p align="center">
  <strong>English</strong> |
  <a href="README/README.ko.md">한국어</a> |
  <a href="README/README.ja.md">日本語</a> |
  <a href="README/README.zh-CN.md">简体中文</a> |
  <a href="README/README.zh-TW.md">繁體中文</a> |
  <a href="README/README.de.md">Deutsch</a> |
  <a href="README/README.es.md">Español</a> |
  <a href="README/README.fr.md">Français</a> |
  <a href="README/README.pt.md">Português</a>
</p>

---

![Miller Columns navigation](README/miller-columns.gif)

> **Navigate folder hierarchies the way they were meant to be navigated.**
> Click a folder, its contents appear in the next column. You always see where you are, where you came from, and where you're going — all at once. No more clicking back and forth.

![LumiFinder — Miller Columns in Action](README/hero.png)

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/stargazers"><img src="https://img.shields.io/github/stars/LumiBearStudio/LumiFinder?style=social" alt="GitHub Stars"></a>
</p>
<p align="center">
  If you find LumiFinder useful, consider giving it a ⭐ — it helps others discover this project!
</p>

---

## Why LumiFinder?

| | Windows Explorer | LumiFinder |
|---|---|---|
| **Miller Columns** | No | Yes — hierarchical multi-column navigation |
| **Multi-Tab** | Windows 11 only (basic) | Full tabs with tear-off, re-docking, duplication, session restore |
| **Split View** | No | Dual-pane with independent view modes |
| **Preview Panel** | Basic | 10+ file types — images, video, audio, code, hex, fonts, PDF |
| **Keyboard Navigation** | Limited | 30+ shortcuts, type-ahead search, full keyboard-first design |
| **Batch Rename** | No | Regex, prefix/suffix, sequential numbering |
| **Undo/Redo** | Limited | Full operation history (configurable depth) |
| **Custom Accent** | No | 10 preset colors + light / dark / system theme |
| **Layout Density** | No | 6-level row height + independent font/icon scale |
| **Remote Connections** | No | FTP, FTPS, SFTP with saved credentials |
| **Workspaces** | No | Save & restore named tab layouts instantly |
| **File Shelf** | No | Yoink-style drag & drop staging area |
| **Cloud Status** | Basic overlay | Real-time sync badges (OneDrive, iCloud, Dropbox) |
| **Startup Speed** | Slow on large directories | Async loading with cancellation — zero lag |

---

## Features

### Miller Columns — See Everything at Once

Navigate deep folder hierarchies without losing context. Each column represents one level — click a folder and its contents appear in the next column. You always see where you are and where you came from.

- Draggable column separators for custom widths
- Auto-equalize columns (Ctrl+Shift+=) or auto-fit to content (Ctrl+Shift+-)
- Smooth horizontal scrolling that keeps the active column visible
- Stable layout — no scroll jitter on keyboard ↑/↓ navigation

### Four View Modes

- **Miller Columns** (Ctrl+1) — Hierarchical navigation, LumiFinder's signature
- **Details** (Ctrl+2) — Sortable table with name, date, type, size columns
- **List** (Ctrl+3) — Dense multi-column layout for scanning large directories
- **Icons** (Ctrl+4) — Grid view with 4 size options up to 256x256 thumbnails

### Multi-Tab with Full Session Restore

- Open unlimited tabs, each with its own path, view mode, and history
- **Tab tear-off & re-docking**: Drag a tab out to create a new window, drag it back to re-dock — Chrome-style ghost tab indicator and semi-transparent window feedback
- **Tab duplication**: Clone a tab with its exact path and settings
- Session auto-save: Close the app, reopen it — every tab exactly where you left it

### Split View — True Dual-Pane

![Split view with Miller Columns + Code Preview](README/2.png)

- Side-by-side file browsing with independent navigation per pane
- Each pane can use a different view mode (Miller left, Details right)
- Separate preview panels for each pane
- Drag files between panes for copy/move operations

### Preview Panel — Know Before You Open

Press **Space** for Quick Look (macOS Finder style):

- **Arrow key & Space navigation**: Browse files without closing Quick Look
- **Window size persistence**: Quick Look remembers its last size
- **Images**: JPEG, PNG, GIF, BMP, WebP, TIFF with resolution and metadata
- **Video**: MP4, MKV, AVI, MOV, WEBM with playback controls
- **Audio**: MP3, AAC, M4A with artist, album, duration info
- **Text & Code**: 30+ extensions with syntax display
- **PDF**: First page preview
- **Fonts**: Glyph samples with metadata
- **Hex Binary**: Raw byte view for developers
- **Folders**: Size, item count, creation date
- **File hash**: SHA256 checksum display with one-click copy (opt-in via Settings)

### Keyboard-First Design

30+ keyboard shortcuts for users who keep their hands on the keyboard:

| Shortcut | Action |
|----------|--------|
| Arrow Keys | Navigate columns and items |
| Enter | Open folder or execute file |
| Space | Toggle preview panel |
| Ctrl+L / Alt+D | Edit address bar |
| Ctrl+F | Search |
| Ctrl+C / X / V | Copy / Cut / Paste |
| Ctrl+Z / Y | Undo / Redo |
| Ctrl+Shift+N | New folder |
| F2 | Rename (batch rename if multi-select) |
| Ctrl+T / W | New tab / Close tab |
| Ctrl+Tab / Ctrl+Shift+Tab | Cycle tabs forward / backward |
| Ctrl+1-4 | Switch view mode |
| Ctrl+Shift+E | Toggle split view |
| F6 | Switch split view pane |
| Ctrl+Shift+S | Save workspace |
| Ctrl+Shift+W | Open workspace palette |
| Ctrl+Shift+H | Toggle file extensions |
| Shift+F10 | Full native shell context menu |
| Delete | Move to Recycle Bin |

### Themes & Customization

![Settings — Appearance with Custom Accent + Layout Density](README/4.png)

- **Light / Dark / System** theme tracking
- **10 Preset Accent Colors** — override the accent of any theme with one click (Lumi Gold default)
- **6-Level Layout Density** — XS / S / M / L / XL / XXL row heights
- **Independent Font/Icon Scale** — separate from row density
- **9 Languages**: English, Korean, Japanese, Chinese (Simplified/Traditional), German, Spanish, French, Portuguese (BR)

### General Settings

![Settings — General with Split View + Preview options](README/3.png)

- **Per-pane startup behavior** — Open System Drive / Restore last session / Custom path, independently for left & right
- **Startup view mode** — choose Miller Columns / Details / List / Icons per pane
- **Preview panel** — enable on startup or toggle on demand with Space
- **File Shelf** — opt-in Yoink-style staging shelf, with optional persistence across sessions
- **System tray** — minimize to tray instead of closing

### Developer Tools

- **Git status badges**: Modified, Added, Deleted, Untracked per file
- **Hex dump viewer**: First 512 bytes in hex + ASCII
- **Terminal integration**: Ctrl+` opens terminal at current path
- **Remote connections**: FTP/FTPS/SFTP with encrypted credential storage

### Cloud Storage Integration

- **Sync status badges**: Cloud-only, Synced, Pending Upload, Syncing
- **OneDrive, iCloud, Dropbox** detection out of the box
- **Smart thumbnails**: Uses cached previews — never triggers unwanted downloads

### Smart Search

- **Structured queries**: `type:image`, `size:>100MB`, `date:today`, `ext:.pdf`
- **Type-ahead**: Start typing in any column to filter instantly
- **Background processing**: Search never freezes the UI

### Workspace — Save & Restore Tab Layouts

- **Save current tabs**: Right-click any tab → "Save tab layout..." or press Ctrl+Shift+S
- **Restore instantly**: Click the workspace button in the sidebar or press Ctrl+Shift+W
- **Manage workspaces**: Restore, rename, or delete saved layouts from the workspace menu
- Perfect for switching between work contexts — "Development", "Photo Editing", "Documents"

### File Shelf

- macOS Yoink-style drag & drop staging area
- Drag files into the Shelf while you navigate, drop them where you need
- Deleting a Shelf item only removes the reference — your original file is never touched
- Disabled by default — enable in **Settings > General > Remember Shelf items**

---

## Performance

Engineered for speed. Tested with 10,000+ items per folder.

- Async I/O everywhere — nothing blocks the UI thread
- Batch property updates with minimal overhead
- Debounced selection prevents redundant work during rapid navigation
- Per-tab caching — instant tab switching, no re-rendering
- Concurrent thumbnail loading with SemaphoreSlim throttling

---

## System Requirements

| | |
|---|---|
| **OS** | Windows 10 version 1903+ / Windows 11 |
| **Architecture** | x64, ARM64 |
| **Runtime** | Windows App SDK 1.8 (.NET 8) |
| **Recommended** | Windows 11 for Mica backdrop |

---

## Build from Source

```bash
# Prerequisites: Visual Studio 2022 with .NET Desktop + WinUI 3 workloads

# Clone
git clone https://github.com/LumiBearStudio/LumiFinder.git
cd LumiFinder

# Build
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -p:Platform=x64

# Run unit tests
dotnet test src/LumiFiles/LumiFiles.Tests/LumiFiles.Tests.csproj -p:Platform=x64
```

> **Note**: WinUI 3 apps cannot be launched via `dotnet run`. Use **Visual Studio F5** (MSIX packaging required).

---

## Contributing

Found a bug? Have a feature request? [Open an issue](https://github.com/LumiBearStudio/LumiFinder/issues) — all feedback welcome.

See [CONTRIBUTING.md](CONTRIBUTING.md) for build setup, coding conventions, and PR guidelines.

---

## Support the Project

If LumiFinder makes your file management better, consider:

- **[Sponsor on GitHub](https://github.com/sponsors/LumiBearStudio)** — buy me a coffee, a hamburger, or a steak
- **Star this repo** to help others discover it
- **Share** with colleagues who miss macOS Finder on Windows
- **Report bugs** — every issue report makes LumiFinder more stable

---

## Privacy & Telemetry

LumiFinder uses [Sentry](https://sentry.io) for **crash reporting only** — and you can turn it off.

- **What we collect**: Exception type, stack trace, OS version, app version
- **What we DON'T collect**: File names, folder paths, browsing history, personal information
- **No usage analytics, no tracking, no ads**
- All file paths in crash reports are automatically scrubbed before sending
- `SendDefaultPii = false` — no IP addresses or user identifiers
- **Opt-out**: Settings > Advanced > "Crash Reporting" toggle to disable completely
- Source code is open — verify yourself in [`CrashReportingService.cs`](src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

See [Privacy Policy](PRIVACY.md) for full details.

---

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

**Trademark**: The "LumiFinder" name and official logo are trademarks of LumiBear Studio. Forks must use a different name and logo. See [LICENSE.md](LICENSE.md) for full trademark policy.

---

<p align="center">
  <a href="https://github.com/sponsors/LumiBearStudio">Sponsor</a> ·
  <a href="PRIVACY.md">Privacy Policy</a> ·
  <a href="OpenSourceLicenses.md">Open Source Licenses</a> ·
  <a href="https://github.com/LumiBearStudio/LumiFinder/issues">Bug Reports & Feature Requests</a>
</p>

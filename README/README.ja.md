<h1 align="center">
  LumiFinder
</h1>

<p align="center">
  <strong>macOS Finder のミラーカラム、Windows に再構築。</strong><br>
  Windows に乗り換えてもカラムビューを忘れられないパワーユーザーのためのファイラー。
</p>

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/releases/latest"><img src="https://img.shields.io/github/v/release/LumiBearStudio/LumiFinder?style=for-the-badge&label=Latest" alt="最新リリース"></a>
  <a href="../LICENSE"><img src="https://img.shields.io/github/license/LumiBearStudio/LumiFinder?style=for-the-badge" alt="ライセンス"></a>
  <a href="https://github.com/sponsors/LumiBearStudio"><img src="https://img.shields.io/badge/Sponsor-%E2%9D%A4-ff69b4?style=for-the-badge&logo=github-sponsors" alt="スポンサー"></a>
</p>

<p align="center">
  <a href="../README.md">English</a> |
  <a href="README.ko.md">한국어</a> |
  <strong>日本語</strong> |
  <a href="README.zh-CN.md">简体中文</a> |
  <a href="README.zh-TW.md">繁體中文</a> |
  <a href="README.de.md">Deutsch</a> |
  <a href="README.es.md">Español</a> |
  <a href="README.fr.md">Français</a> |
  <a href="README.pt.md">Português</a>
</p>

---

![ミラーカラムでのナビゲーション](miller-columns.gif)

> **本来あるべき方法でフォルダ階層をナビゲートしましょう。**
> フォルダをクリックすると、その内容が次のカラムに表示されます。今どこにいて、どこから来て、どこへ向かうのか — 一目瞭然。もう前後にクリックする必要はありません。

![LumiFinder — ミラーカラムの動作](hero.png)

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/stargazers"><img src="https://img.shields.io/github/stars/LumiBearStudio/LumiFinder?style=social" alt="GitHub Stars"></a>
</p>
<p align="center">
  LumiFinder が役立ったら ⭐ を — 他の人がこのプロジェクトを発見するのを助けてくれます！
</p>

---

## なぜ LumiFinder なのか?

| | Windows エクスプローラー | LumiFinder |
|---|---|---|
| **ミラーカラム** | なし | 階層型マルチカラムナビゲーション |
| **マルチタブ** | Windows 11 のみ (基本) | タブの切り離し/再ドック/複製/セッション復元の完全サポート |
| **分割表示** | なし | 独立したビューモードのデュアルペイン |
| **プレビューパネル** | 基本 | 10種類以上 — 画像、動画、音声、コード、Hex、フォント、PDF |
| **キーボード操作** | 限定的 | 30以上のショートカット、タイプアヘッド検索、キーボードファースト設計 |
| **一括リネーム** | なし | 正規表現、プレフィックス/サフィックス、連番 |
| **元に戻す/やり直す** | 限定的 | 完全な操作履歴 (深さ設定可) |
| **カスタムアクセント** | なし | 10色のプリセット + ライト/ダーク/システム テーマ |
| **レイアウト密度** | なし | 6段階の行高 + フォント/アイコンの独立スケール |
| **リモート接続** | なし | FTP, FTPS, SFTP — 認証情報保存 |
| **ワークスペース** | なし | 名前付きタブレイアウトを瞬時に保存/復元 |
| **ファイルシェルフ** | なし | Yoink スタイルのドラッグ&ドロップ一時置き場 |
| **クラウドステータス** | 基本オーバーレイ | リアルタイム同期バッジ (OneDrive, iCloud, Dropbox) |
| **起動速度** | 大きなディレクトリで遅い | キャンセル可能な非同期読み込み — 遅延ゼロ |

---

## 機能

### ミラーカラム — 全部を一目で

深いフォルダ階層をコンテキストを失うことなくナビゲート。各カラムが1階層を表現 — フォルダをクリックするとその内容が次のカラムに表示されます。今どこにいて、どこから来たかが常に見えます。

- カラム区切り線をドラッグして幅調整
- 自動均等化 (Ctrl+Shift+=) または内容に自動フィット (Ctrl+Shift+-)
- アクティブカラムを常に表示するスムーズな水平スクロール
- 安定したレイアウト — キーボード ↑/↓ ナビゲーションでスクロール揺れなし

### 4つのビューモード

- **ミラーカラム** (Ctrl+1) — 階層型ナビゲーション、LumiFinder のシグネチャ
- **詳細** (Ctrl+2) — 名前/日付/種類/サイズでソート可能な表
- **リスト** (Ctrl+3) — 大量のファイルをスキャンする高密度マルチカラム
- **アイコン** (Ctrl+4) — 256×256 まで4サイズのグリッドビュー

### マルチタブ + セッション完全復元

- 無制限のタブ — 各タブは独自のパス/ビューモード/履歴
- **タブの切り離し/再ドック**: タブをドラッグして新ウィンドウ作成、または再ドック — Chrome スタイルのゴーストインジケーター、半透明ウィンドウフィードバック
- **タブ複製**: 正確なパス/設定でタブを複製
- セッション自動保存: アプリを閉じて再開 — すべてのタブが元のまま

### 分割表示 — 真のデュアルペイン

![ミラーカラム + コードプレビューの分割表示](2.png)

- ペインごとの独立したナビゲーションのサイドバイサイド
- 各ペインに異なるビューモード (左ミラー、右詳細)
- ペインごとに別のプレビューパネル
- ペイン間でファイルをドラッグしてコピー/移動

### プレビューパネル — 開く前に確認

**スペース**で Quick Look (macOS Finder スタイル):

- **矢印キー + スペースナビゲーション**: Quick Look を閉じずにファイルを閲覧
- **ウィンドウサイズ記憶**: 最後のサイズを保持
- **画像**: JPEG, PNG, GIF, BMP, WebP, TIFF — 解像度とメタデータ
- **動画**: MP4, MKV, AVI, MOV, WEBM — 再生コントロール
- **音声**: MP3, AAC, M4A — アーティスト/アルバム/長さ
- **テキスト + コード**: 30以上の拡張子、構文表示
- **PDF**: 最初のページプレビュー
- **フォント**: グリフサンプル + メタデータ
- **Hex バイナリ**: 開発者向けの生バイトビュー
- **フォルダ**: サイズ/項目数/作成日
- **ファイルハッシュ**: SHA256 チェックサム + ワンクリックコピー (設定で有効化)

### キーボードファースト設計

手をキーボードから離さないユーザーのための30以上のショートカット:

| ショートカット | 動作 |
|----------|--------|
| 矢印キー | カラム/項目をナビゲート |
| Enter | フォルダを開く / ファイルを実行 |
| Space | プレビューパネルの切り替え |
| Ctrl+L / Alt+D | アドレスバー編集 |
| Ctrl+F | 検索 |
| Ctrl+C / X / V | コピー / 切り取り / 貼り付け |
| Ctrl+Z / Y | 元に戻す / やり直す |
| Ctrl+Shift+N | 新しいフォルダ |
| F2 | 名前変更 (複数選択時は一括) |
| Ctrl+T / W | 新しいタブ / タブを閉じる |
| Ctrl+Tab / Ctrl+Shift+Tab | タブを循環 |
| Ctrl+1-4 | ビューモード切替 |
| Ctrl+Shift+E | 分割表示の切り替え |
| F6 | 分割ペイン切替 |
| Ctrl+Shift+S | ワークスペース保存 |
| Ctrl+Shift+W | ワークスペースパレット |
| Ctrl+Shift+H | ファイル拡張子表示の切替 |
| Shift+F10 | 完全なネイティブシェルコンテキストメニュー |
| Delete | ごみ箱に移動 |

### テーマ + カスタマイズ

![設定 — 外観: カスタムアクセント + レイアウト密度](4.png)

- **ライト / ダーク / システム** テーマ追従
- **10色のプリセットアクセント** — どのテーマでもワンクリックでアクセント色変更 (Lumi Gold デフォルト)
- **6段階レイアウト密度** — XS / S / M / L / XL / XXL の行高
- **フォント/アイコンの独立スケール** — 行密度とは別
- **9言語**: 英語、韓国語、日本語、中国語 (簡体/繁体)、ドイツ語、スペイン語、フランス語、ポルトガル語 (BR)

### 一般設定

![設定 — 一般: 分割表示 + プレビューオプション](3.png)

- **ペインごとの起動動作** — システムドライブを開く / 最後のセッション復元 / カスタムパス、左右独立
- **起動時のビューモード** — ペインごとにミラーカラム / 詳細 / リスト / アイコンを選択
- **プレビューパネル** — 起動時に有効化、またはスペースで切替
- **ファイルシェルフ** — Yoink スタイルの一時置き場 (オプトイン、セッション間で保持可)
- **システムトレイ** — 閉じるボタンでトレイに最小化

### 開発者ツール

- **Git ステータスバッジ**: ファイル単位の Modified, Added, Deleted, Untracked
- **Hex ダンプビューア**: 最初の 512 バイトを Hex + ASCII で表示
- **ターミナル統合**: Ctrl+` で現在のパスにターミナルを開く
- **リモート接続**: 暗号化された認証情報保存の FTP/FTPS/SFTP

### クラウドストレージ統合

- **同期ステータスバッジ**: クラウドのみ、同期済み、アップロード待ち、同期中
- **OneDrive, iCloud, Dropbox** を自動検出
- **スマートサムネイル**: キャッシュされたプレビューを使用 — 意図しないダウンロードをトリガーしない

### スマート検索

- **構造化クエリ**: `type:image`, `size:>100MB`, `date:today`, `ext:.pdf`
- **タイプアヘッド**: どのカラムでも入力開始で即座にフィルタ
- **バックグラウンド処理**: 検索が UI を止めない

### ワークスペース — タブレイアウトの保存/復元

- **現在のタブを保存**: タブ右クリック → "タブレイアウトを保存..." または Ctrl+Shift+S
- **即座に復元**: サイドバーのワークスペースボタン または Ctrl+Shift+W
- **ワークスペース管理**: 保存されたレイアウトの復元/名前変更/削除
- 作業コンテキストの切替に最適 — "開発", "写真編集", "ドキュメント"

### ファイルシェルフ

- macOS Yoink スタイルのドラッグ&ドロップ一時置き場
- ナビゲートしながらシェルフにファイルを入れ、必要な場所にドロップ
- シェルフ項目の削除は参照のみを消す — 元のファイルには触れません
- デフォルトで無効 — **設定 > 一般 > シェルフ項目を記憶**で有効化

---

## パフォーマンス

速度を追求して設計。フォルダあたり 10,000+ 項目でテスト済み。

- どこでも非同期 I/O — UI スレッドをブロックしない
- 最小オーバーヘッドのバッチプロパティ更新
- デバウンスされた選択 — 高速ナビゲーション中の冗長作業を防止
- タブごとのキャッシング — 即座のタブ切替、再レンダリングなし
- SemaphoreSlim スロットリングの同時サムネイル読み込み

---

## システム要件

| | |
|---|---|
| **OS** | Windows 10 1903+ / Windows 11 |
| **アーキテクチャ** | x64, ARM64 |
| **ランタイム** | Windows App SDK 1.8 (.NET 8) |
| **推奨** | Mica バックドロップのため Windows 11 |

---

## ソースからビルド

```bash
# 前提: Visual Studio 2022 + .NET Desktop + WinUI 3 ワークロード

# クローン
git clone https://github.com/LumiBearStudio/LumiFinder.git
cd LumiFinder

# ビルド
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -p:Platform=x64

# ユニットテスト実行
dotnet test src/LumiFiles/LumiFiles.Tests/LumiFiles.Tests.csproj -p:Platform=x64
```

> **注意**: WinUI 3 アプリは `dotnet run` では起動できません。**Visual Studio F5** を使ってください (MSIX パッケージング必要)。

---

## 貢献

バグを見つけましたか? 機能リクエストがありますか? [Issue を立ててください](https://github.com/LumiBearStudio/LumiFinder/issues) — すべてのフィードバック歓迎します。

ビルド設定/コーディング規約/PR ガイドラインは [CONTRIBUTING.md](../CONTRIBUTING.md) 参照。

---

## プロジェクト支援

LumiFinder があなたのファイル管理を良くしたなら:

- **[GitHub Sponsors](https://github.com/sponsors/LumiBearStudio)** — コーヒー、ハンバーガー、ステーキを
- **このリポジトリに ⭐** — 他の人の発見を助ける
- **共有** — Windows で macOS Finder を恋しく思う同僚に
- **バグ報告** — 一件一件の Issue が LumiFinder を安定させる

---

## プライバシー + テレメトリー

LumiFinder は [Sentry](https://sentry.io) を **クラッシュレポート専用**で使用 — 無効化できます。

- **収集する**: 例外タイプ、スタックトレース、OS バージョン、アプリバージョン
- **収集しない**: ファイル名、フォルダパス、ナビゲーション履歴、個人識別情報
- **使用分析/トラッキング/広告なし**
- クラッシュレポートのすべてのファイルパスは送信前に自動スクラブ
- `SendDefaultPii = false` — IP アドレスやユーザー識別子なし
- **オプトアウト**: 設定 > 詳細 > "クラッシュレポート" トグル
- ソース公開 — [`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs) で直接確認

詳細は [プライバシーポリシー](../PRIVACY.md) 参照。

---

## ライセンス

このプロジェクトは [GNU General Public License v3.0](../LICENSE) でライセンスされています。

**商標**: "LumiFinder" の名前と公式ロゴは LumiBear Studio の商標です。フォークは別の名前とロゴを使用してください。完全な商標ポリシーは [LICENSE.md](../LICENSE.md) 参照。

---

<p align="center">
  <a href="https://github.com/sponsors/LumiBearStudio">スポンサー</a> ·
  <a href="../PRIVACY.md">プライバシーポリシー</a> ·
  <a href="../OpenSourceLicenses.md">オープンソースライセンス</a> ·
  <a href="https://github.com/LumiBearStudio/LumiFinder/issues">バグ報告 + 機能リクエスト</a>
</p>

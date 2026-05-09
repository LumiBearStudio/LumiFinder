# LumiFinder — プライバシーポリシー

**最終更新: 2026年5月9日**

<p align="center">
  <a href="../PRIVACY.md">English</a> |
  <a href="PRIVACY.ko.md">한국어</a> |
  <strong>日本語</strong> |
  <a href="PRIVACY.zh-CN.md">简体中文</a> |
  <a href="PRIVACY.zh-TW.md">繁體中文</a> |
  <a href="PRIVACY.de.md">Deutsch</a> |
  <a href="PRIVACY.es.md">Español</a> |
  <a href="PRIVACY.fr.md">Français</a> |
  <a href="PRIVACY.pt.md">Português</a>
</p>

---

## 概要

LumiFinder(「本アプリ」)は、LumiBear Studio が開発した Windows 用ファイラーです。私たちはユーザーのプライバシー保護に最善を尽くします。本ポリシーでは、どのようなデータを収集し、どう保護し、どのように制御できるかを説明します。

## 収集するデータ

### クラッシュレポート (Sentry)

本アプリは自動クラッシュレポートに [Sentry](https://sentry.io) を使用します。アプリのクラッシュまたは未処理のエラー発生時、以下のデータが**送信される可能性があります**:

- **エラー詳細**: 例外タイプ、メッセージ、スタックトレース
- **デバイス情報**: OS バージョン、CPU アーキテクチャ、クラッシュ時のメモリ使用量
- **アプリ情報**: アプリバージョン、ランタイムバージョン、ビルド構成

クラッシュレポートは**バグの特定と修正のためだけ**に使用されます。以下は**含まれません**:

- ファイル名、フォルダ名、ファイル内容
- ユーザーアカウント情報
- ナビゲーション履歴やパス
- 個人を識別できる情報 (PII)

### クラッシュレポートのプライバシー保護

クラッシュレポートが送信される前に、複数層の PII スクラビングが自動的に適用されます:

- **ユーザー名マスキング** — Windows のユーザーフォルダパス(`C:\Users\<ユーザー名>\...`)を検出し、ユーザー名部分を送信前に置換します。UNC パス(`\\サーバー\共有\Users\<ユーザー名>\...`)にも同様に適用。
- **`SendDefaultPii = false`** — Sentry SDK による IP アドレス、サーバー名、ユーザー識別子の自動収集を完全に無効化。
- **ファイル内容を含まず** — スタックトレースはファイル/フォルダの内容を含まず、行番号とメソッド名のみ。

オープンソースのコードで自分自身で検証可能:
[`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

### ローカル設定

本アプリは、ユーザー設定(テーマ、言語、最近のフォルダ、お気に入り、カスタムアクセント色など)を Windows `ApplicationData.LocalSettings` でローカルに保存します。このデータは**いかなるサーバーにも送信されません**。

## 収集しないデータ

- 個人情報なし(名前、メール、住所)
- ファイルシステム内容またはメタデータなし
- 使用分析またはテレメトリーなし
- 位置データなし
- 広告識別子なし
- マーケティング目的の第三者データ共有なし

## ネットワークアクセス

本アプリは以下の用途でのみインターネットを使用します:

- **クラッシュレポート** (Sentry) — 自動エラーレポート、無効化可能 (下記「オプトアウト方法」参照)
- **FTP / FTPS / SFTP 接続** — ユーザーが明示的に設定した場合のみ
- **NuGet パッケージ復元** — 開発ビルド時のみ (エンドユーザーでは動作しない)

## クラッシュレポートのオプトアウト方法

クラッシュレポートはインターネット接続を切断せずにアプリ内で直接無効化できます:

1. **設定** を開く (サイドバー左下)
2. **詳細設定** に移動
3. **クラッシュレポート** トグルを OFF

変更は即座に反映されます。オプトアウト後はいかなる状況でもクラッシュレポートは送信されません。すでに Sentry サーバーにある過去のレポートは標準 90 日の保管期限で自動的に削除されます。

## データの保存と保管

- **Sentry サーバー**: クラッシュレポートは Sentry の **ドイツ・フランクフルト (EU)** データセンターに保存されます — GDPR 準拠のために選択。**90 日**後に自動削除。
- **ローカル設定**: ユーザーのデバイスにのみ保存。アプリのアンインストールで削除されます。

## データ処理者としての Sentry (GDPR)

Sentry は GDPR の下でクラッシュレポートのデータ処理者(Data Processor)として動作します。Sentry 自身のプライバシー慣行とセキュリティ対策の詳細:

- **Sentry プライバシーポリシー**: [https://sentry.io/privacy/](https://sentry.io/privacy/)
- **Sentry セキュリティ**: [https://sentry.io/security/](https://sentry.io/security/)
- **Sentry GDPR**: [https://sentry.io/legal/dpa/](https://sentry.io/legal/dpa/)

LumiBear Studio は Sentry のデータ処理条項をレビューし、クラッシュデータが適切な保護措置なしに欧州経済領域(EEA)を離れないよう EU リージョン(`o4510949994266624.ingest.de.sentry.io`)を選択しました。

## 子どものプライバシー

本アプリは 13 歳未満の子どものデータを意図的に収集しません。本アプリは子どもを対象とせず、子どもを識別できるいかなる個人情報も収集しません。

## ユーザーの権利

私たちは個人データを収集しないため、アクセス/変更/削除する個人データはありません。具体的には:

- **アクセス/移動権**: 該当なし — 私たちが保有する個人データなし。
- **削除権**: 該当なし — 私たちが保有する個人データなし。ローカル設定はアプリのアンインストールで削除可能。
- **オプトアウト権**: 設定 > 詳細設定 > クラッシュレポートで利用可能 (上記「オプトアウト方法」参照)。

## オープンソース

LumiFinder は GPL v3.0 ライセンスでオープンソースです。コードを直接検査/監査/修正できます:

- **ソースコード**: [https://github.com/LumiBearStudio/LumiFinder](https://github.com/LumiBearStudio/LumiFinder)
- **使用しているオープンソースライブラリ**: [OpenSourceLicenses.md](../OpenSourceLicenses.md) 参照

## お問い合わせ

本プライバシーポリシーへの質問、違反の発見、上記権利の行使要求:

- **GitHub Issues**: [https://github.com/LumiBearStudio/LumiFinder/issues](https://github.com/LumiBearStudio/LumiFinder/issues)
- **セキュリティ報告**: [SECURITY.md](../SECURITY.md) 参照

## 本ポリシーの変更

アプリの進化または法的要件の変化に応じて、本ポリシーを更新することがあります。重要な変更は GitHub Releases で告知されます。各更新は本文書上部の「最終更新」日を更新します。バージョン履歴は[Git 履歴](https://github.com/LumiBearStudio/LumiFinder/commits/main/PRIVACY.md)で永続的に確認可能です。

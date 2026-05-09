# LumiFinder — Privacy Policy

**Last Updated: May 9, 2026**

<p align="center">
  <strong>English</strong> |
  <a href="PRIVACY/PRIVACY.ko.md">한국어</a> |
  <a href="PRIVACY/PRIVACY.ja.md">日本語</a> |
  <a href="PRIVACY/PRIVACY.zh-CN.md">简体中文</a> |
  <a href="PRIVACY/PRIVACY.zh-TW.md">繁體中文</a> |
  <a href="PRIVACY/PRIVACY.de.md">Deutsch</a> |
  <a href="PRIVACY/PRIVACY.es.md">Español</a> |
  <a href="PRIVACY/PRIVACY.fr.md">Français</a> |
  <a href="PRIVACY/PRIVACY.pt.md">Português</a>
</p>

---

## Overview

LumiFinder ("the App") is a file explorer application for Windows developed by LumiBear Studio. We are committed to protecting your privacy. This policy explains what data we collect, how we protect it, and how you can control it.

## Data We Collect

### Crash Reports (Sentry)

The App uses [Sentry](https://sentry.io) for automated crash reporting. When the App crashes or encounters an unhandled error, the following data **may** be sent:

- **Error details**: Exception type, message, and stack trace
- **Device info**: OS version, CPU architecture, memory usage at time of crash
- **App info**: App version, runtime version, build configuration

Crash reports are used **solely** to identify and fix bugs. They do **not** include:

- File names, folder names, or file contents
- User account information
- Browsing history or navigation paths
- Any personally identifiable information (PII)

### Privacy Protections in Crash Reports

Before any crash report is sent, multiple layers of PII scrubbing are applied automatically:

- **Username masking** — Windows user folder paths (`C:\Users\<your-username>\...`) are detected and the username portion is replaced before transmission. The same applies to UNC paths (`\\server\share\Users\<username>\...`).
- **`SendDefaultPii = false`** — Sentry SDK's automatic collection of IP addresses, server names, and user identifiers is fully disabled.
- **No file contents** — Stack traces never contain file or folder contents; only line numbers and method names.

You can verify the implementation yourself in the open-source code:
[`CrashReportingService.cs`](src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

### Local Settings

The App stores user preferences (theme, language, recent folders, favorites, custom accent color, etc.) locally on your device using Windows `ApplicationData.LocalSettings`. This data is **never** transmitted to any server.

## Data We Do NOT Collect

- No personal information (name, email, address)
- No file system contents or file metadata
- No usage analytics or telemetry
- No location data
- No advertising identifiers
- No data shared with third parties for marketing

## Network Access

The App requires internet access only for:

- **Crash reporting** (Sentry) — automatic error reports, can be disabled (see "How to Opt Out" below)
- **FTP / FTPS / SFTP connections** — only when explicitly configured by the user
- **NuGet package restore** — during development builds only (does not run for end users)

## How to Opt Out of Crash Reporting

Crash reporting can be disabled directly from the App without disconnecting from the internet:

1. Open **Settings** (sidebar bottom-left)
2. Navigate to **Advanced**
3. Toggle **Crash Reporting** off

The change takes effect immediately. After opting out, no crash reports will be sent under any circumstances. Past reports already on Sentry's servers will still expire on the standard 90-day retention schedule.

## Data Storage and Retention

- **Sentry servers**: Crash reports are stored in Sentry's **Frankfurt, Germany (EU)** data center — chosen for GDPR compliance. Reports are automatically deleted after **90 days**.
- **Local settings**: Stored only on your device. Removed when you uninstall the App.

## Sentry as Data Processor (GDPR)

Sentry acts as a Data Processor for crash reports under the GDPR. For details on Sentry's own privacy practices and security measures:

- **Sentry Privacy Policy**: [https://sentry.io/privacy/](https://sentry.io/privacy/)
- **Sentry Security**: [https://sentry.io/security/](https://sentry.io/security/)
- **Sentry GDPR**: [https://sentry.io/legal/dpa/](https://sentry.io/legal/dpa/)

LumiBear Studio has reviewed Sentry's data processing terms and selected the EU region (`o4510949994266624.ingest.de.sentry.io`) to ensure crash data does not leave the European Economic Area without appropriate safeguards.

## Children's Privacy

The App does not knowingly collect any data from children under the age of 13. The App does not target children and does not collect any personal information that could identify a child.

## Your Rights

Since we do not collect personal data, there is no personal data to access, modify, or delete. Specifically:

- **Right to access / portability**: Not applicable — no personal data is held by us.
- **Right to deletion**: Not applicable — no personal data is held by us. Local settings can be removed by uninstalling the App.
- **Right to opt out**: Available in Settings > Advanced > Crash Reporting (see "How to Opt Out" above).

## Open Source

LumiFinder is open source under the GPL v3.0 license. You are welcome to inspect, audit, or modify the code yourself:

- **Source code**: [https://github.com/LumiBearStudio/LumiFinder](https://github.com/LumiBearStudio/LumiFinder)
- **Open-source libraries used**: See [OpenSourceLicenses.md](OpenSourceLicenses.md)

## Contact

If you have questions about this privacy policy, found a violation, or want to exercise rights described above:

- **GitHub Issues**: [https://github.com/LumiBearStudio/LumiFinder/issues](https://github.com/LumiBearStudio/LumiFinder/issues)
- **Security disclosure**: See [SECURITY.md](SECURITY.md)

## Changes to This Policy

We may update this policy from time to time as the App evolves or as legal requirements change. Material changes will be announced via GitHub Releases. Each update bumps the "Last Updated" date at the top of this document. The version history is permanently available in the [Git history of this file](https://github.com/LumiBearStudio/LumiFinder/commits/main/PRIVACY.md).

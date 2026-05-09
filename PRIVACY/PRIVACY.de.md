# LumiFinder — Datenschutzerklärung

**Letzte Aktualisierung: 9. Mai 2026**

<p align="center">
  <a href="../PRIVACY.md">English</a> |
  <a href="PRIVACY.ko.md">한국어</a> |
  <a href="PRIVACY.ja.md">日本語</a> |
  <a href="PRIVACY.zh-CN.md">简体中文</a> |
  <a href="PRIVACY.zh-TW.md">繁體中文</a> |
  <strong>Deutsch</strong> |
  <a href="PRIVACY.es.md">Español</a> |
  <a href="PRIVACY.fr.md">Français</a> |
  <a href="PRIVACY.pt.md">Português</a>
</p>

---

## Überblick

LumiFinder („die App") ist ein von LumiBear Studio entwickelter Datei-Explorer für Windows. Wir verpflichten uns dem Schutz Ihrer Privatsphäre. Diese Richtlinie erklärt, welche Daten wir erfassen, wie wir sie schützen und wie Sie sie steuern können.

## Daten, die wir erfassen

### Crash-Berichte (Sentry)

Die App nutzt [Sentry](https://sentry.io) für automatische Crash-Berichte. Wenn die App abstürzt oder einen unbehandelten Fehler hat, **können** folgende Daten gesendet werden:

- **Fehlerdetails**: Exception-Typ, Nachricht und Stack-Trace
- **Geräteinfos**: OS-Version, CPU-Architektur, Speichernutzung zum Crash-Zeitpunkt
- **App-Info**: App-Version, Runtime-Version, Build-Konfiguration

Crash-Berichte werden **ausschließlich** zur Fehlerbehebung verwendet. Sie enthalten **nicht**:

- Dateinamen, Ordnernamen oder Dateiinhalte
- Benutzerkonto-Informationen
- Browserverlauf oder Navigationspfade
- Personenbezogene Daten (PII)

### Datenschutz in Crash-Berichten

Bevor ein Crash-Bericht gesendet wird, werden mehrere PII-Bereinigungs-Schichten automatisch angewendet:

- **Benutzername-Maskierung** — Windows-Benutzerordnerpfade (`C:\Users\<Ihr-Benutzername>\...`) werden erkannt, und der Benutzername-Teil wird vor der Übertragung ersetzt. Gleiches gilt für UNC-Pfade (`\\server\share\Users\<Benutzername>\...`).
- **`SendDefaultPii = false`** — Die automatische Erfassung von IP-Adressen, Servernamen und Benutzerkennungen durch das Sentry SDK ist vollständig deaktiviert.
- **Keine Dateiinhalte** — Stack-Traces enthalten niemals Datei- oder Ordnerinhalte; nur Zeilennummern und Methodennamen.

Sie können die Implementierung im Open-Source-Code selbst überprüfen:
[`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

### Lokale Einstellungen

Die App speichert Benutzereinstellungen (Theme, Sprache, zuletzt verwendete Ordner, Favoriten, eigene Akzentfarbe usw.) lokal auf Ihrem Gerät über Windows `ApplicationData.LocalSettings`. Diese Daten werden **niemals** an einen Server übertragen.

## Daten, die wir NICHT erfassen

- Keine personenbezogenen Daten (Name, E-Mail, Adresse)
- Keine Dateisysteminhalte oder Dateimetadaten
- Keine Nutzungsanalyse oder Telemetrie
- Keine Standortdaten
- Keine Werbe-IDs
- Keine Datenweitergabe an Dritte zu Marketingzwecken

## Netzwerkzugriff

Die App benötigt Internetzugang nur für:

- **Crash-Berichte** (Sentry) — automatische Fehlerberichte, deaktivierbar (siehe „Wie Sie sich abmelden" unten)
- **FTP / FTPS / SFTP-Verbindungen** — nur wenn vom Benutzer ausdrücklich konfiguriert
- **NuGet-Paket-Wiederherstellung** — nur bei Entwicklungs-Builds (läuft nicht für Endbenutzer)

## Wie Sie sich von Crash-Berichten abmelden

Crash-Berichte können direkt in der App deaktiviert werden, ohne die Internetverbindung zu trennen:

1. **Einstellungen** öffnen (Seitenleiste unten links)
2. Zu **Erweitert** navigieren
3. **Crash-Berichte** ausschalten

Die Änderung wird sofort wirksam. Nach dem Opt-out werden unter keinen Umständen Crash-Berichte gesendet. Bereits auf Sentrys Servern gespeicherte vergangene Berichte verfallen weiterhin nach dem Standard-90-Tage-Aufbewahrungsplan.

## Datenspeicherung und Aufbewahrung

- **Sentry-Server**: Crash-Berichte werden im **Frankfurt, Deutschland (EU)** Rechenzentrum von Sentry gespeichert — gewählt für DSGVO-Konformität. Berichte werden nach **90 Tagen** automatisch gelöscht.
- **Lokale Einstellungen**: Nur auf Ihrem Gerät gespeichert. Werden bei der Deinstallation der App entfernt.

## Sentry als Auftragsverarbeiter (DSGVO)

Sentry agiert als Auftragsverarbeiter (Data Processor) für Crash-Berichte unter der DSGVO. Details zu Sentrys eigenen Datenschutzpraktiken und Sicherheitsmaßnahmen:

- **Sentry-Datenschutzerklärung**: [https://sentry.io/privacy/](https://sentry.io/privacy/)
- **Sentry-Sicherheit**: [https://sentry.io/security/](https://sentry.io/security/)
- **Sentry-DSGVO**: [https://sentry.io/legal/dpa/](https://sentry.io/legal/dpa/)

LumiBear Studio hat Sentrys Datenverarbeitungsbedingungen überprüft und die EU-Region (`o4510949994266624.ingest.de.sentry.io`) gewählt, um sicherzustellen, dass Crash-Daten den Europäischen Wirtschaftsraum (EWR) nicht ohne angemessene Schutzmaßnahmen verlassen.

## Datenschutz für Kinder

Die App erfasst wissentlich keine Daten von Kindern unter 13 Jahren. Die App richtet sich nicht an Kinder und erfasst keine personenbezogenen Daten, die ein Kind identifizieren könnten.

## Ihre Rechte

Da wir keine personenbezogenen Daten erfassen, gibt es keine personenbezogenen Daten, auf die zugegriffen, die geändert oder gelöscht werden können. Konkret:

- **Recht auf Zugriff / Übertragbarkeit**: Nicht anwendbar — keine personenbezogenen Daten bei uns gespeichert.
- **Recht auf Löschung**: Nicht anwendbar — keine personenbezogenen Daten bei uns gespeichert. Lokale Einstellungen können durch Deinstallation der App entfernt werden.
- **Recht auf Widerspruch**: Verfügbar in Einstellungen > Erweitert > Crash-Berichte (siehe „Wie Sie sich abmelden" oben).

## Open Source

LumiFinder ist Open Source unter der GPL v3.0-Lizenz. Sie können den Code gerne selbst inspizieren, prüfen oder modifizieren:

- **Quellcode**: [https://github.com/LumiBearStudio/LumiFinder](https://github.com/LumiBearStudio/LumiFinder)
- **Verwendete Open-Source-Bibliotheken**: Siehe [OpenSourceLicenses.md](../OpenSourceLicenses.md)

## Kontakt

Bei Fragen zu dieser Datenschutzerklärung, Verstößen oder zur Ausübung der oben beschriebenen Rechte:

- **GitHub Issues**: [https://github.com/LumiBearStudio/LumiFinder/issues](https://github.com/LumiBearStudio/LumiFinder/issues)
- **Sicherheits-Offenlegung**: Siehe [SECURITY.md](../SECURITY.md)

## Änderungen dieser Richtlinie

Wir können diese Richtlinie von Zeit zu Zeit aktualisieren, wenn sich die App weiterentwickelt oder rechtliche Anforderungen ändern. Wesentliche Änderungen werden über GitHub Releases bekannt gegeben. Jede Aktualisierung erhöht das Datum „Letzte Aktualisierung" oben in diesem Dokument. Die Versionshistorie ist dauerhaft in der [Git-Historie](https://github.com/LumiBearStudio/LumiFinder/commits/main/PRIVACY.md) verfügbar.

<h1 align="center">
  LumiFinder
</h1>

<p align="center">
  <strong>Die Miller-Spalten von macOS Finder, neu gedacht für Windows.</strong><br>
  Für Power-User, die zu Windows gewechselt sind, aber die Spaltenansicht nie vergessen haben.
</p>

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/releases/latest"><img src="https://img.shields.io/github/v/release/LumiBearStudio/LumiFinder?style=for-the-badge&label=Latest" alt="Letzter Release"></a>
  <a href="../LICENSE"><img src="https://img.shields.io/github/license/LumiBearStudio/LumiFinder?style=for-the-badge" alt="Lizenz"></a>
  <a href="https://github.com/sponsors/LumiBearStudio"><img src="https://img.shields.io/badge/Sponsor-%E2%9D%A4-ff69b4?style=for-the-badge&logo=github-sponsors" alt="Sponsor"></a>
</p>

<p align="center">
  <a href="../README.md">English</a> |
  <a href="README.ko.md">한국어</a> |
  <a href="README.ja.md">日本語</a> |
  <a href="README.zh-CN.md">简体中文</a> |
  <a href="README.zh-TW.md">繁體中文</a> |
  <strong>Deutsch</strong> |
  <a href="README.es.md">Español</a> |
  <a href="README.fr.md">Français</a> |
  <a href="README.pt.md">Português</a>
</p>

---

![Miller-Spalten-Navigation](miller-columns.gif)

> **Navigieren Sie Ordnerhierarchien so, wie es eigentlich gemeint war.**
> Klicken Sie auf einen Ordner, sein Inhalt erscheint in der nächsten Spalte. Sie sehen immer, wo Sie sind, woher Sie kommen und wohin Sie gehen — alles auf einmal. Schluss mit dem Vor- und Zurück-Klicken.

![LumiFinder — Miller-Spalten in Aktion](hero.png)

<p align="center">
  <a href="https://github.com/LumiBearStudio/LumiFinder/stargazers"><img src="https://img.shields.io/github/stars/LumiBearStudio/LumiFinder?style=social" alt="GitHub Stars"></a>
</p>
<p align="center">
  Wenn Ihnen LumiFinder gefällt, geben Sie uns einen ⭐ — es hilft anderen, das Projekt zu entdecken!
</p>

---

## Warum LumiFinder?

| | Windows-Explorer | LumiFinder |
|---|---|---|
| **Miller-Spalten** | Nein | Ja — hierarchische Mehrspalten-Navigation |
| **Multi-Tab** | Nur Windows 11 (Basis) | Vollständige Tabs mit Ablösen, Neu-Andocken, Duplizieren, Sitzungswiederherstellung |
| **Geteilte Ansicht** | Nein | Zwei Bereiche mit unabhängigen Ansichtsmodi |
| **Vorschaufenster** | Basis | 10+ Dateitypen — Bilder, Video, Audio, Code, Hex, Schriftarten, PDF |
| **Tastaturnavigation** | Begrenzt | 30+ Shortcuts, Type-Ahead-Suche, Keyboard-First-Design |
| **Stapel-Umbenennung** | Nein | Regex, Präfix/Suffix, fortlaufende Nummerierung |
| **Rückgängig/Wiederherstellen** | Begrenzt | Vollständige Verlaufshistorie (Tiefe konfigurierbar) |
| **Eigene Akzentfarbe** | Nein | 10 Voreinstellungen + Hell-/Dunkel-/System-Theme |
| **Layout-Dichte** | Nein | 6 Stufen Zeilenhöhe + unabhängige Schrift-/Symbol-Skala |
| **Remote-Verbindungen** | Nein | FTP, FTPS, SFTP mit gespeicherten Anmeldedaten |
| **Workspaces** | Nein | Speichern und sofortiges Wiederherstellen benannter Tab-Layouts |
| **File Shelf** | Nein | Yoink-artige Drag-and-Drop-Ablagefläche |
| **Cloud-Status** | Einfaches Overlay | Echtzeit-Synchronisierungs-Badges (OneDrive, iCloud, Dropbox) |
| **Startgeschwindigkeit** | Langsam bei großen Verzeichnissen | Asynchrones Laden mit Abbruch — keine Verzögerung |

---

## Funktionen

### Miller-Spalten — Alles auf einen Blick

Navigieren Sie tiefe Ordnerhierarchien, ohne den Kontext zu verlieren. Jede Spalte repräsentiert eine Ebene — klicken Sie auf einen Ordner, und sein Inhalt erscheint in der nächsten Spalte. Sie sehen immer, wo Sie sind und woher Sie kommen.

- Ziehbare Spaltentrenner für individuelle Breiten
- Auto-Egalisierung (Strg+Umschalt+=) oder Auto-Anpassung an Inhalt (Strg+Umschalt+-)
- Sanftes horizontales Scrollen, das die aktive Spalte sichtbar hält
- Stabiles Layout — kein Scroll-Wackeln bei Tastaturnavigation ↑/↓

### Vier Ansichtsmodi

- **Miller-Spalten** (Strg+1) — Hierarchische Navigation, LumiFinders Markenzeichen
- **Details** (Strg+2) — Sortierbare Tabelle mit Name, Datum, Typ, Größe
- **Liste** (Strg+3) — Dichtes Mehrspalten-Layout zum Scannen großer Verzeichnisse
- **Symbole** (Strg+4) — Rasteransicht mit 4 Größen bis 256×256

![Details-Ansichtsmodus mit Papierkorb-Tab](details.png)

### Multi-Tab mit vollständiger Sitzungswiederherstellung

- Unbegrenzte Tabs, jeder mit eigenem Pfad, Ansichtsmodus und Verlauf
- **Tab-Ablösen und Neu-Andocken**: Tab herausziehen für ein neues Fenster, zurückziehen zum Andocken — Chrome-artiger Geist-Indikator und halbtransparentes Fenster-Feedback
- **Tab-Duplizierung**: Tab mit identischem Pfad und Einstellungen klonen
- Automatische Sitzungs­speicherung: App schließen, neu öffnen — alle Tabs genau wie zuvor

### Geteilte Ansicht — Echtes Dual-Panel

![Geteilte Ansicht mit Miller-Spalten + Code-Vorschau](2.png)

- Nebeneinander-Datei-Browsing mit unabhängiger Navigation pro Bereich
- Jeder Bereich kann einen anderen Ansichtsmodus verwenden (Miller links, Details rechts)
- Separate Vorschau-Panels für jeden Bereich
- Dateien zwischen Bereichen ziehen für Kopieren/Verschieben

### Vorschau-Panel — Wissen, bevor Sie öffnen

Drücken Sie **Leertaste** für Quick Look (macOS Finder-Stil):

- **Pfeiltasten- und Leertasten-Navigation**: Dateien durchblättern, ohne Quick Look zu schließen
- **Fenstergröße bleibt erhalten**: Quick Look merkt sich die letzte Größe
- **Bilder**: JPEG, PNG, GIF, BMP, WebP, TIFF mit Auflösung und Metadaten
- **Video**: MP4, MKV, AVI, MOV, WEBM mit Wiedergabe-Steuerung
- **Audio**: MP3, AAC, M4A mit Künstler, Album, Dauer
- **Text und Code**: 30+ Erweiterungen mit Syntax-Anzeige
- **PDF**: Vorschau der ersten Seite
- **Schriftarten**: Glyphen-Beispiele mit Metadaten
- **Hex-Binärdateien**: Roh-Byte-Ansicht für Entwickler
- **Ordner**: Größe, Anzahl Elemente, Erstellungsdatum
- **Datei-Hash**: SHA256-Prüfsumme mit Ein-Klick-Kopie (in Einstellungen aktivierbar)

### Keyboard-First-Design

30+ Tastaturkürzel für Nutzer, die ihre Hände an der Tastatur lassen:

| Shortcut | Aktion |
|----------|--------|
| Pfeiltasten | Spalten und Elemente navigieren |
| Eingabe | Ordner öffnen oder Datei ausführen |
| Leertaste | Vorschau-Panel umschalten |
| Strg+L / Alt+D | Adressleiste bearbeiten |
| Strg+F | Suche |
| Strg+C / X / V | Kopieren / Ausschneiden / Einfügen |
| Strg+Z / Y | Rückgängig / Wiederherstellen |
| Strg+Umschalt+N | Neuer Ordner |
| F2 | Umbenennen (Stapel bei Mehrfachauswahl) |
| Strg+T / W | Neuer Tab / Tab schließen |
| Strg+Tab / Strg+Umschalt+Tab | Tabs durchschalten |
| Strg+1-4 | Ansichtsmodus wechseln |
| Strg+Umschalt+E | Geteilte Ansicht umschalten |
| F6 | Bereich der geteilten Ansicht wechseln |
| Strg+Umschalt+S | Workspace speichern |
| Strg+Umschalt+W | Workspace-Palette öffnen |
| Strg+Umschalt+H | Dateierweiterungen umschalten |
| Umschalt+F10 | Vollständiges natives Shell-Kontextmenü |
| Entf | In Papierkorb verschieben |

### Themes und Anpassung

![Einstellungen — Erscheinungsbild mit eigener Akzentfarbe + Layout-Dichte](4.png)

- **Hell- / Dunkel- / System-Theme**-Verfolgung
- **10 voreingestellte Akzentfarben** — überschreiben Sie die Akzentfarbe jedes Themes mit einem Klick (Lumi Gold Standard)
- **6 Stufen Layout-Dichte** — XS / S / M / L / XL / XXL Zeilenhöhen
- **Unabhängige Schrift-/Symbol-Skala** — getrennt von Zeilendichte
- **9 Sprachen**: Englisch, Koreanisch, Japanisch, Chinesisch (Vereinfacht/Traditionell), Deutsch, Spanisch, Französisch, Portugiesisch (BR)

### Allgemeine Einstellungen

![Einstellungen — Allgemein mit geteilter Ansicht und Vorschau-Optionen](3.png)

- **Startverhalten pro Bereich** — Systemlaufwerk öffnen / Letzte Sitzung wiederherstellen / Eigener Pfad, links und rechts unabhängig
- **Startansichtsmodus** — Miller-Spalten / Details / Liste / Symbole pro Bereich wählen
- **Vorschau-Panel** — beim Start aktivieren oder bei Bedarf mit Leertaste umschalten
- **File Shelf** — optionale Yoink-artige Ablagefläche, optional sitzungs­übergreifend
- **System-Tray** — beim Schließen in den Tray minimieren

### Entwickler-Tools

- **Git-Status-Badges**: Modified, Added, Deleted, Untracked pro Datei
- **Hex-Dump-Viewer**: Erste 512 Bytes in Hex + ASCII
- **Terminal-Integration**: Strg+` öffnet Terminal im aktuellen Pfad
- **Remote-Verbindungen**: FTP/FTPS/SFTP mit verschlüsselter Anmeldedaten-Speicherung

### Cloud-Speicher-Integration

- **Synchronisations-Badges**: Nur-Cloud, Synchronisiert, Upload ausstehend, Synchronisiert
- **OneDrive, iCloud, Dropbox** automatisch erkannt
- **Smarte Vorschaubilder**: nutzen zwischengespeicherte Vorschauen — keine ungewollten Downloads

### Smarte Suche

- **Strukturierte Abfragen**: `type:image`, `size:>100MB`, `date:today`, `ext:.pdf`
- **Type-Ahead**: In jeder Spalte tippen filtert sofort
- **Hintergrund-Verarbeitung**: Suche friert die UI nie ein

### Workspace — Tab-Layouts speichern und wiederherstellen

- **Aktuelle Tabs speichern**: Rechtsklick auf einen Tab → "Tab-Layout speichern…" oder Strg+Umschalt+S
- **Sofort wiederherstellen**: Workspace-Schaltfläche in der Seitenleiste oder Strg+Umschalt+W
- **Workspaces verwalten**: Gespeicherte Layouts wiederherstellen, umbenennen oder löschen
- Perfekt zum Wechseln zwischen Arbeitskontexten — "Entwicklung", "Bildbearbeitung", "Dokumente"

### File Shelf

- macOS-Yoink-artige Drag-and-Drop-Ablagefläche
- Dateien während der Navigation in das Shelf ziehen, dann an gewünschtem Ort ablegen
- Löschen eines Shelf-Eintrags entfernt nur die Referenz — die Originaldatei bleibt unberührt
- Standardmäßig deaktiviert — in **Einstellungen > Allgemein > Shelf-Einträge merken** aktivieren

---

## Performance

Auf Geschwindigkeit ausgelegt. Mit 10.000+ Elementen pro Ordner getestet.

- Asynchrone I/O überall — der UI-Thread wird nie blockiert
- Stapel-Eigenschaftsupdates mit minimalem Overhead
- Entprellte Auswahl verhindert redundante Arbeit bei schneller Navigation
- Tab-spezifisches Caching — sofortiger Tab-Wechsel ohne Neu-Rendering
- Gleichzeitiges Vorschau-Laden mit SemaphoreSlim-Throttling

---

## Systemanforderungen

| | |
|---|---|
| **Betriebssystem** | Windows 10 1903+ / Windows 11 |
| **Architektur** | x64, ARM64 |
| **Laufzeit** | Windows App SDK 1.8 (.NET 8) |
| **Empfohlen** | Windows 11 für Mica-Hintergrund |

---

## Aus dem Quellcode bauen

```bash
# Voraussetzungen: Visual Studio 2022 mit .NET Desktop + WinUI 3 Workloads

# Klonen
git clone https://github.com/LumiBearStudio/LumiFinder.git
cd LumiFinder

# Bauen
dotnet build src/LumiFiles/LumiFiles/LumiFiles.csproj -p:Platform=x64

# Unit-Tests ausführen
dotnet test src/LumiFiles/LumiFiles.Tests/LumiFiles.Tests.csproj -p:Platform=x64
```

> **Hinweis**: WinUI 3-Apps können nicht über `dotnet run` gestartet werden. Verwenden Sie **Visual Studio F5** (MSIX-Paketierung erforderlich).

---

## Mitwirken

Bug gefunden? Feature-Wunsch? [Issue eröffnen](https://github.com/LumiBearStudio/LumiFinder/issues) — jedes Feedback willkommen.

Build-Setup, Konventionen und PR-Richtlinien siehe [CONTRIBUTING.md](../CONTRIBUTING.md).

---

## Projekt unterstützen

Wenn LumiFinder Ihre Dateiverwaltung verbessert:

- **[GitHub Sponsors](https://github.com/sponsors/LumiBearStudio)** — spendieren Sie einen Kaffee, einen Burger oder ein Steak
- **Geben Sie diesem Repo ⭐** — hilft anderen, es zu entdecken
- **Teilen** — mit Kollegen, die macOS Finder unter Windows vermissen
- **Bugs melden** — jedes Issue macht LumiFinder stabiler

---

## Datenschutz und Telemetrie

LumiFinder nutzt [Sentry](https://sentry.io) **nur für Crash-Reporting** — und Sie können es ausschalten.

- **Was wir erfassen**: Exception-Typ, Stack-Trace, OS-Version, App-Version
- **Was wir NICHT erfassen**: Dateinamen, Ordnerpfade, Browser-Verlauf, persönliche Informationen
- **Keine Nutzungs-Analyse, kein Tracking, keine Werbung**
- Alle Dateipfade in Crash-Reports werden vor dem Senden automatisch entfernt
- `SendDefaultPii = false` — keine IP-Adressen oder Benutzer-IDs
- **Opt-out**: Einstellungen > Erweitert > "Crash Reporting"-Schalter zum vollständigen Deaktivieren
- Quellcode offen — überprüfen Sie es selbst in [`CrashReportingService.cs`](../src/LumiFiles/LumiFiles/Services/CrashReportingService.cs)

Vollständige Details siehe [Datenschutzerklärung](../PRIVACY.md).

---

## Lizenz

Dieses Projekt ist unter der [GNU General Public License v3.0](../LICENSE) lizenziert.

**Markenzeichen**: Der Name "LumiFinder" und das offizielle Logo sind Markenzeichen von LumiBear Studio. Forks müssen einen anderen Namen und ein anderes Logo verwenden. Vollständige Markenrichtlinie siehe [LICENSE.md](../LICENSE.md).

---

<p align="center">
  <a href="https://github.com/sponsors/LumiBearStudio">Sponsor</a> ·
  <a href="../PRIVACY.md">Datenschutz</a> ·
  <a href="../OpenSourceLicenses.md">Open-Source-Lizenzen</a> ·
  <a href="https://github.com/LumiBearStudio/LumiFinder/issues">Bug-Reports und Feature-Wünsche</a>
</p>

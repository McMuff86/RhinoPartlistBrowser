# ProjectGoals

Dieses Dokument beschreibt die Ziele des Projekts und dient als Leitfaden für den AI Coding Agent. Es kann bei Bedarf angepasst werden, um die Entwicklung des Projekts zu reflektieren.

---

## Startanweisung für den AI Coding Agent

**Wenn du unsicher bist, wie du vorgehen sollst, lies dieses `ProjectGoals.md`-File, um die Projektziele und nächsten Schritte zu verstehen.** Falls erforderlich, passe dieses Dokument an, um neue Erkenntnisse oder Änderungen im Projektverlauf zu dokumentieren. Stelle sicher, dass Anpassungen klar kommentiert werden, z. B. mit Datum und Grund der Änderung.

---

## Projektziele

### Überblick
Das Ziel dieses Projekts ist die Entwicklung eines RhinoPlugins, das ein Browserfenster öffnet und darin die Bill of Materials (BOM) anzeigt, die durch ein Skript erzeugt wird. Der bestehende Python-Code soll nach C# portiert werden, wobei die Projektstruktur klar dokumentiert und flexibel gestaltet wird, um eine nachvollziehbare Entwicklung zu gewährleisten.

### Spezifische Ziele
1. **RhinoPlugin-Entwicklung**:
   - Erstellung eines Plugins für Rhino, das ein Browserfenster öffnet.
   - Anzeige der Bill of Materials (BOM) im Browserfenster.
   - Erzeugung der BOM durch ein Skript, das in C# implementiert wird.

2. **Code-Portierung**:
   - Portierung des vorhandenen Python-Codes nach C#.
   - Sicherstellung, dass der portierte Code funktional und mit Rhino kompatibel ist.

3. **Dokumentation der Projektstruktur**:
   - Erstellung und Aktualisierung einer Übersicht der Ordner- und Dateistruktur in `projektstruktur.txt`.
   - Jeder neue Unterordner erhält eine `README.md`-Datei mit spezifischen Anweisungen und Informationen.

4. **Denkprozess nachvollziehbar machen**:
   - Dokumentation jedes Entwicklungsschritts im Ordner `thoughtprocess`.
   - Verwendung von fortlaufend nummerierten Text- und Code-Dateien (z. B. `001-thoughts.txt`, `002-code.cs`), um den Entwicklungsverlauf nachvollziehbar zu gestalten.

5. **Code-Entwicklung**:
   - Schrittweise Verbesserung des Codes basierend auf dem dokumentierten Denkprozess.
   - Nutzung des `examplecode`-Ordners für Referenzmaterial wie den ursprünglichen Python-Code, C#-Beispiele und Rhino-spezifische Snippets.

---

## Projektstruktur

Die folgende Struktur wird empfohlen, um das Projekt organisiert zu halten:

- **`projektstruktur.txt`**: Detaillierte Übersicht der Ordner- und Dateistruktur.
- **`thoughtprocess/`**: Ordner für die Denkprozess-Dokumentation.
  - Beispiel: `001-initial-thoughts.txt`, `002-python-analysis.txt`, `003-csharp-port.cs`.
- **`examplecode/`**: Ordner für Referenzmaterial.
  - Unterordner: `python/` (ursprünglicher Python-Code), `csharp/` (portierter Code und Beispiele), `rhino/` (Rhino-spezifische Snippets).
- **`src/`**: Hauptordner für den Quellcode des RhinoPlugins.
  - Enthält die C#-Dateien des Plugins.
- **`README.md`**: In jedem Unterordner zur Erklärung des Inhalts.

**Hinweis**: Eine detaillierte Beschreibung findest du in `projektstruktur.txt`. Erstelle diese Datei als ersten Schritt, um die Struktur zu definieren.

---

## Hinweis auf den `examplecode`-Ordner

Der Ordner `examplecode` enthält nützliche Ressourcen für die Entwicklung:

- **`python/`**: Der ursprüngliche Python-Code als Referenz für die Portierung.
- **`csharp/`**: C#-Beispiele für die BOM-Erzeugung und Browserfenster-Integration.
- **`rhino/`**: Codesnippets für die Rhino-Plugin-Entwicklung (z. B. Browserfenster öffnen).

**Anweisung für den Agent**: Nutze den `examplecode`-Ordner als Ausgangspunkt oder Inspiration. Dokumentiere im `thoughtprocess`-Ordner, wenn du auf diesen Code zurückgreifst, und erläutere, wie er angepasst wurde.

---

## Anpassung des Bootstrap-Files

Falls Änderungen an diesem Dokument erforderlich sind, um neue Erkenntnisse oder Projektänderungen zu reflektieren, füge einen Kommentar mit Datum und Grund der Änderung hinzu. Beispiel:

-- Änderung am 2023-10-15: Hinzufügung eines Ziels für die Integration einer UI-Komponente --

## Nächste Schritte

1. **Projektstruktur einrichten**:
   - Erstelle die oben beschriebenen Ordner und die `projektstruktur.txt`-Datei.
   - Füge eine `README.md`-Datei in jedem Unterordner hinzu.

2. **Denkprozess starten**:
   - Analysiere den bestehenden Python-Code und dokumentiere die Logik im `thoughtprocess`-Ordner (z. B. `001-python-analysis.txt`).

3. **Code-Portierung beginnen**:
   - Starte mit der Portierung des Python-Codes nach C# und speichere erste Entwürfe in `examplecode/csharp/`.
   - Dokumentiere jeden Schritt (z. B. `002-csharp-port.cs`).

4. **RhinoPlugin entwickeln**:
   - Implementiere die Grundfunktion des Plugins (Browserfenster öffnen) in `src/`.
   - Integriere die BOM-Anzeige schrittweise.

---

Dieses Bootstrap-File bietet eine solide Grundlage für dein Projekt und stellt sicher, dass alle Ziele klar definiert und die Entwicklung strukturiert bleibt. Viel Erfolg bei der Umsetzung!

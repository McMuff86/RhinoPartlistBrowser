using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Net;
using System.Windows.Forms;
using Rhino;

namespace RhinoPartlistBrowser
{
    /// <summary>
    /// Klasse zur Anzeige des Assembly-Baums im Browser
    /// </summary>
    public class BrowserDisplay
    {
        private string _htmlFilePath;
        private string _outputDirectory;
        private List<string> _tempFiles = new List<string>();
        
        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="outputDir">Optionaler Ausgabeordner für temporäre Dateien</param>
        public BrowserDisplay(string outputDir = null)
        {
            if (string.IsNullOrEmpty(outputDir))
            {
                // Standardmäßig im temporären Verzeichnis
                _outputDirectory = Path.Combine(Path.GetTempPath(), "RhinoPartlistBrowser");
            }
            else
            {
                _outputDirectory = outputDir;
            }

            // Verzeichnis erstellen, falls es nicht existiert
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            // Temporäre HTML-Datei erstellen
            _htmlFilePath = Path.Combine(_outputDirectory, $"assembly_tree_{DateTime.Now:yyyyMMddHHmmss}.html");
            _tempFiles.Add(_htmlFilePath);
        }

        /// <summary>
        /// Zeigt den Assembly-Baum in einem Browser an
        /// </summary>
        /// <param name="assemblyTreeData">JSON-String mit den Baumdaten</param>
        /// <param name="attachments">Liste von Dateipfaden für Anhänge (z.B. PDFs, Bilder)</param>
        public void DisplayAssemblyTreeInBrowser(string assemblyTreeData, List<string> attachments = null)
        {
            try
            {
                // HTML-Inhalt generieren
                string htmlContent = GenerateHtmlContent(assemblyTreeData, attachments);

                // HTML-Datei speichern
                File.WriteAllText(_htmlFilePath, htmlContent);

                // Browser öffnen mit richtiger Methode für verschiedene .NET Versionen
                try
                {
                    // Methode 1: Verwenden der URL-Klasse (bevorzugt)
                    System.Diagnostics.Process.Start(new ProcessStartInfo
                    {
                        FileName = _htmlFilePath,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Methode 2: Alternative für ältere .NET-Versionen
                    var uri = new Uri(_htmlFilePath);
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start \"\" \"{uri.AbsoluteUri}\"",
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                }

                RhinoApp.WriteLine($"Assembly-Baum wurde im Browser geöffnet: {_htmlFilePath}");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Anzeigen im Browser: {ex.Message}");
                if (MessageBox.Show($"Fehler beim Anzeigen im Browser: {ex.Message}\n\nMöchten Sie den HTML-Code in die Zwischenablage kopieren?", 
                    "Fehler", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    try
                    {
                        string htmlContent = GenerateHtmlContent(assemblyTreeData, attachments);
                        Clipboard.SetText(htmlContent);
                        RhinoApp.WriteLine("HTML-Code wurde in die Zwischenablage kopiert.");
                    }
                    catch (Exception clipEx)
                    {
                        RhinoApp.WriteLine($"Fehler beim Kopieren in die Zwischenablage: {clipEx.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Generiert den HTML-Inhalt für die Anzeige des Assembly-Baums
        /// </summary>
        /// <param name="assemblyTreeData">JSON-String mit den Baumdaten</param>
        /// <param name="attachments">Liste von Dateipfaden für Anhänge (z.B. PDFs, Bilder)</param>
        /// <returns>Der generierte HTML-Inhalt</returns>
        private string GenerateHtmlContent(string assemblyTreeData, List<string> attachments = null)
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("  <meta charset=\"utf-8\">");
            html.AppendLine("  <title>Assembly Tree</title>");
            html.AppendLine("  <style>");
            html.AppendLine("    body {");
            html.AppendLine("      font-family: Arial, sans-serif;");
            html.AppendLine("      background-color: #f4f4f4;");
            html.AppendLine("      color: #333;");
            html.AppendLine("      padding: 20px;");
            html.AppendLine("    }");
            html.AppendLine("    h1 {");
            html.AppendLine("      color: #0055a5;");
            html.AppendLine("    }");
            html.AppendLine("    p.summary {");
            html.AppendLine("      font-size: 1.1em;");
            html.AppendLine("      margin-bottom: 20px;");
            html.AppendLine("    }");
            html.AppendLine("    ul {");
            html.AppendLine("      list-style-type: none;");
            html.AppendLine("      padding-left: 20px;");
            html.AppendLine("    }");
            html.AppendLine("    li {");
            html.AppendLine("      margin: 5px 0;");
            html.AppendLine("      background: #fff;");
            html.AppendLine("      padding: 8px 12px;");
            html.AppendLine("      border-radius: 4px;");
            html.AppendLine("      box-shadow: 0 1px 2px rgba(0,0,0,0.1);");
            html.AppendLine("    }");
            html.AppendLine("    .toggle {");
            html.AppendLine("      cursor: pointer;");
            html.AppendLine("    }");
            html.AppendLine("    .toggle::before {");
            html.AppendLine("      content: \"▸ \";");
            html.AppendLine("      color: #0055a5;");
            html.AppendLine("    }");
            html.AppendLine("    .expanded::before {");
            html.AppendLine("      content: \"▾ \";");
            html.AppendLine("      color: #0055a5;");
            html.AppendLine("    }");
            html.AppendLine("    a {");
            html.AppendLine("      color: #0055a5;");
            html.AppendLine("      text-decoration: none;");
            html.AppendLine("    }");
            html.AppendLine("    a:hover {");
            html.AppendLine("      text-decoration: underline;");
            html.AppendLine("    }");
            html.AppendLine("    .attachment-section {");
            html.AppendLine("      margin-top: 30px;");
            html.AppendLine("    }");
            html.AppendLine("    .attachment-section h2 {");
            html.AppendLine("      color: #0055a5;");
            html.AppendLine("    }");
            html.AppendLine("    .embedded-image {");
            html.AppendLine("      max-width: 90%;");
            html.AppendLine("      margin: 10px 0;");
            html.AppendLine("      border: 1px solid #ccc;");
            html.AppendLine("      padding: 4px;");
            html.AppendLine("      border-radius: 4px;");
            html.AppendLine("    }");
            html.AppendLine("    .export-buttons {");
            html.AppendLine("      margin: 20px 0;");
            html.AppendLine("    }");
            html.AppendLine("    button {");
            html.AppendLine("      background-color: #0055a5;");
            html.AppendLine("      color: white;");
            html.AppendLine("      border: none;");
            html.AppendLine("      padding: 8px 16px;");
            html.AppendLine("      margin-right: 10px;");
            html.AppendLine("      border-radius: 4px;");
            html.AppendLine("      cursor: pointer;");
            html.AppendLine("    }");
            html.AppendLine("    button:hover {");
            html.AppendLine("      background-color: #003d7a;");
            html.AppendLine("    }");
            html.AppendLine("  </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("  <h1>Assembly Tree for <span id=\"assembly-name\">AssemblyName</span></h1>");
            html.AppendLine("  <p class=\"summary\">Total Subassemblies: <span id=\"total-subassemblies\">0</span>, Total Parts: <span id=\"total-parts\">0</span></p>");
            
            html.AppendLine("  <div class=\"export-buttons\">");
            html.AppendLine("    <button onclick=\"saveAsHTML()\">Als HTML speichern</button>");
            html.AppendLine("    <button onclick=\"printTree()\">Drucken</button>");
            html.AppendLine("  </div>");
            
            html.AppendLine("  <ul id=\"assembly-tree\">");
            html.AppendLine("    <!-- Hier wird die Baumstruktur dynamisch eingefügt -->");
            html.AppendLine("  </ul>");

            html.AppendLine("  <div class=\"attachment-section\" id=\"attachment-container\" style=\"display: none;\">");
            html.AppendLine("    <h2>Anhänge</h2>");
            html.AppendLine("    <ul id=\"attachments-list\">");
            html.AppendLine("      <!-- Hier werden Anhänge eingefügt -->");
            html.AppendLine("    </ul>");
            html.AppendLine("  </div>");

            html.AppendLine("  <script>");
            html.AppendLine("    document.addEventListener(\"DOMContentLoaded\", function(){");
            html.AppendLine("      // Tree-Daten aus dem JSON-String laden");
            html.AppendLine($"      const treeData = {assemblyTreeData};");
            
            html.AppendLine("      // Funktion zum Berechnen der Zusammenfassung");
            html.AppendLine("      function computeSummary(node) {");
            html.AppendLine("        if (!node.children || node.children.length === 0) {");
            html.AppendLine("          return { parts: node.count, subassemblies: 0 };");
            html.AppendLine("        } else {");
            html.AppendLine("          let totalParts = 0;");
            html.AppendLine("          let totalSubassemblies = 0;");
            html.AppendLine("          ");
            html.AppendLine("          for (const child of node.children) {");
            html.AppendLine("            if (child.children && child.children.length > 0) {");
            html.AppendLine("              totalSubassemblies += child.count;");
            html.AppendLine("            }");
            html.AppendLine("            const childSummary = computeSummary(child);");
            html.AppendLine("            totalParts += childSummary.parts;");
            html.AppendLine("            totalSubassemblies += childSummary.subassemblies;");
            html.AppendLine("          }");
            html.AppendLine("          ");
            html.AppendLine("          return { parts: totalParts, subassemblies: totalSubassemblies };");
            html.AppendLine("        }");
            html.AppendLine("      }");

            html.AppendLine("      // Funktion zum Generieren der HTML-Liste");
            html.AppendLine("      function generateHtmlList(node) {");
            html.AppendLine("        const summary = node.children && node.children.length > 0 ? computeSummary(node) : null;");
            html.AppendLine("        let text = `${node.name} [${node.type}] x${node.count}`;");
            html.AppendLine("        ");
            html.AppendLine("        // DisplayName hinzufügen, wenn vorhanden und es ein Leaf-Node ist");
            html.AppendLine("        if (node.displayName && (!node.children || node.children.length === 0)) {");
            html.AppendLine("          text += ` (${node.displayName})`;");
            html.AppendLine("        }");
            html.AppendLine("        ");
            html.AppendLine("        // UserAttributes hinzufügen, wenn vorhanden und es ein Leaf-Node ist");
            html.AppendLine("        // Unabhängig davon, ob ein DisplayName vorhanden ist");
            html.AppendLine("        if (node.userAttributes && (!node.children || node.children.length === 0)) {");
            html.AppendLine("          let attrText = '';");
            html.AppendLine("          for (const [key, value] of Object.entries(node.userAttributes)) {");
            html.AppendLine("            attrText += `${key}: ${value}, `;");
            html.AppendLine("          }");
            html.AppendLine("          if (attrText) {");
            html.AppendLine("            // Entfernt das letzte Komma und Leerzeichen");
            html.AppendLine("            attrText = attrText.slice(0, -2);");
            html.AppendLine("            text += ` [${attrText}]`;");
            html.AppendLine("          }");
            html.AppendLine("        }");
            html.AppendLine("        ");
            html.AppendLine("        if (summary) {");
            html.AppendLine("          text += ` (Subassemblies: ${summary.subassemblies}, Parts: ${summary.parts})`;");
            html.AppendLine("        }");
            html.AppendLine("        ");
            html.AppendLine("        let html = \"<li>\";");
            html.AppendLine("        ");
            html.AppendLine("        if (node.children && node.children.length > 0) {");
            html.AppendLine("          html += `<span class=\"toggle\">${text}</span><ul style=\"display:none;\">`;");
            html.AppendLine("          ");
            html.AppendLine("          for (const child of node.children) {");
            html.AppendLine("            html += generateHtmlList(child);");
            html.AppendLine("          }");
            html.AppendLine("          ");
            html.AppendLine("          html += \"</ul>\";");
            html.AppendLine("        } else {");
            html.AppendLine("          html += `<span>${text}</span>`;");
            html.AppendLine("        }");
            html.AppendLine("        ");
            html.AppendLine("        html += \"</li>\";");
            html.AppendLine("        return html;");
            html.AppendLine("      }");

            html.AppendLine("      // Funktion zum Rendern des Baums");
            html.AppendLine("      function renderTree(data) {");
            html.AppendLine("        document.getElementById('assembly-name').textContent = data.name;");
            html.AppendLine("        ");
            html.AppendLine("        const summary = computeSummary(data);");
            html.AppendLine("        document.getElementById('total-subassemblies').textContent = summary.subassemblies;");
            html.AppendLine("        document.getElementById('total-parts').textContent = summary.parts;");
            html.AppendLine("        ");
            html.AppendLine("        const treeContainer = document.getElementById('assembly-tree');");
            html.AppendLine("        treeContainer.innerHTML = generateHtmlList(data);");
            html.AppendLine("        ");
            html.AppendLine("        // Event-Listener für Toggle-Elemente hinzufügen");
            html.AppendLine("        const toggles = document.querySelectorAll(\"span.toggle\");");
            html.AppendLine("        toggles.forEach(function(toggle) {");
            html.AppendLine("          toggle.addEventListener(\"click\", function(e) {");
            html.AppendLine("            const next = toggle.nextElementSibling;");
            html.AppendLine("            if (next && next.tagName.toLowerCase() === \"ul\") {");
            html.AppendLine("              if (next.style.display === \"none\") {");
            html.AppendLine("                next.style.display = \"block\";");
            html.AppendLine("                toggle.classList.add(\"expanded\");");
            html.AppendLine("              } else {");
            html.AppendLine("                next.style.display = \"none\";");
            html.AppendLine("                toggle.classList.remove(\"expanded\");");
            html.AppendLine("              }");
            html.AppendLine("            }");
            html.AppendLine("            e.stopPropagation();");
            html.AppendLine("          });");
            html.AppendLine("        });");
            html.AppendLine("      }");

            // Anhänge hinzufügen
            if (attachments != null && attachments.Count > 0)
            {
                html.AppendLine("      // Anhänge hinzufügen");
                html.AppendLine("      const attachments = [");
                
                for (int i = 0; i < attachments.Count; i++)
                {
                    string path = attachments[i];
                    string filename = Path.GetFileName(path);
                    string ext = Path.GetExtension(path).ToLower();
                    
                    if (ext == ".pdf")
                    {
                        string relPath = MakePdfRelative(path);
                        html.AppendLine($"        {{ type: 'pdf', path: '{relPath}', name: '{filename}' }}{(i < attachments.Count - 1 ? "," : "")}");
                    }
                    else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp")
                    {
                        string base64 = ConvertImageToBase64(path);
                        html.AppendLine($"        {{ type: 'image', data: '{base64}', name: '{filename}' }}{(i < attachments.Count - 1 ? "," : "")}");
                    }
                }
                
                html.AppendLine("      ];");
                
                html.AppendLine("      // Funktion zum Hinzufügen von Anhängen");
                html.AppendLine("      function addAttachments(attachments) {");
                html.AppendLine("        if (attachments && attachments.length > 0) {");
                html.AppendLine("          const attachmentContainer = document.getElementById('attachment-container');");
                html.AppendLine("          attachmentContainer.style.display = 'block';");
                html.AppendLine("          ");
                html.AppendLine("          const attachmentsList = document.getElementById('attachments-list');");
                html.AppendLine("          ");
                html.AppendLine("          for (const attachment of attachments) {");
                html.AppendLine("            const li = document.createElement('li');");
                html.AppendLine("            ");
                html.AppendLine("            if (attachment.type === 'pdf') {");
                html.AppendLine("              // Relativen Pfad verwenden - die PDF liegt im selben Verzeichnis wie die HTML-Datei");
                html.AppendLine("              li.innerHTML = `<a href=\"${attachment.path}\" target=\"_blank\">${attachment.name}</a>`;");
                html.AppendLine("            } else if (attachment.type === 'image') {");
                html.AppendLine("              li.innerHTML = `<div>${attachment.name}</div><img class=\"embedded-image\" src=\"${attachment.data}\" alt=\"${attachment.name}\">`;");
                html.AppendLine("            }");
                html.AppendLine("            ");
                html.AppendLine("            attachmentsList.appendChild(li);");
                html.AppendLine("          }");
                html.AppendLine("        }");
                html.AppendLine("      }");
                
                html.AppendLine("      // Anhänge hinzufügen");
                html.AppendLine("      addAttachments(attachments);");
            }

            html.AppendLine("      // Funktion zum Speichern als HTML");
            html.AppendLine("      window.saveAsHTML = function() {");
            html.AppendLine("        const htmlContent = document.documentElement.outerHTML;");
            html.AppendLine("        const blob = new Blob([htmlContent], { type: 'text/html' });");
            html.AppendLine("        const link = document.createElement('a');");
            html.AppendLine("        link.href = URL.createObjectURL(blob);");
            html.AppendLine("        link.download = `Assembly_Tree_${document.getElementById('assembly-name').textContent}.html`;");
            html.AppendLine("        link.click();");
            html.AppendLine("      };");
            
            html.AppendLine("      // Funktion zum Drucken");
            html.AppendLine("      window.printTree = function() {");
            html.AppendLine("        window.print();");
            html.AppendLine("      };");

            html.AppendLine("      // Tree rendern");
            html.AppendLine("      renderTree(treeData);");

            // PDF-Hilfe hinzufügen
            html.AppendLine("      // Hilfefunktion für PDF-Links");
            html.AppendLine("      function handlePdfError() {");
            html.AppendLine("        const pdfLinks = document.querySelectorAll('a[href$=\".pdf\"]');");
            html.AppendLine("        pdfLinks.forEach(link => {");
            html.AppendLine("          link.addEventListener('click', function(e) {");
            html.AppendLine("            // Timer setzen, um zu überprüfen, ob der Link geöffnet wurde");
            html.AppendLine("            setTimeout(() => {");
            html.AppendLine("              // Hinweis für den Benutzer, falls die Dateien nicht geöffnet werden können");
            html.AppendLine("              if (confirm('Falls die PDF-Datei nicht geöffnet wurde: Möchten Sie Hilfe zur manuellen Öffnung erhalten?')) {");
            html.AppendLine("                const pdfFileName = link.innerText;");
            html.AppendLine("                const helpText = `Die PDF-Datei \"${pdfFileName}\" konnte nicht automatisch geöffnet werden.\\n\\n` +");
            html.AppendLine("                                 `Sie finden die Datei im selben Verzeichnis wie diese HTML-Datei.\\n\\n` +");
            html.AppendLine("                                 `Verzeichnis: ${window.location.pathname.substring(0, window.location.pathname.lastIndexOf('\\\\'))}`");
            html.AppendLine("                alert(helpText);");
            html.AppendLine("              }");
            html.AppendLine("            }, 1000); // 1 Sekunde Verzögerung");
            html.AppendLine("          }, {once: true}); // Event nur einmal pro Link auslösen");
            html.AppendLine("        });");
            html.AppendLine("      }");
            html.AppendLine("      handlePdfError();");

            html.AppendLine("    });");  // Ende des DOMContentLoaded-Event-Handlers
            html.AppendLine("  </script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        /// <summary>
        /// Konvertiert ein Bild in einen Base64-String für die Einbettung in HTML
        /// </summary>
        /// <param name="imagePath">Pfad zum Bild</param>
        /// <returns>Base64-kodierter String des Bildes mit Daten-URL</returns>
        private string ConvertImageToBase64(string imagePath)
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                string base64String = Convert.ToBase64String(imageBytes);
                string mimeType = "image/jpeg"; // Standard

                string extension = Path.GetExtension(imagePath).ToLower();
                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                        mimeType = "image/jpeg";
                        break;
                    case ".png":
                        mimeType = "image/png";
                        break;
                    case ".gif":
                        mimeType = "image/gif";
                        break;
                    case ".bmp":
                        mimeType = "image/bmp";
                        break;
                }

                return $"data:{mimeType};base64,{base64String}";
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Konvertieren des Bildes: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Kopiert eine PDF-Datei in das Ausgabeverzeichnis und gibt einen relativen Pfad zurück
        /// </summary>
        /// <param name="pdfPath">Originalpfad der PDF-Datei</param>
        /// <returns>Relativer Pfad zur kopierten PDF-Datei</returns>
        private string MakePdfRelative(string pdfPath)
        {
            try
            {
                string fileName = Path.GetFileName(pdfPath);
                string targetPath = Path.Combine(_outputDirectory, fileName);
                
                // Nur kopieren, wenn die Datei nicht bereits dort ist
                if (!File.Exists(targetPath) || !FileEquals(pdfPath, targetPath))
                {
                    File.Copy(pdfPath, targetPath, true);
                    _tempFiles.Add(targetPath); // Für spätere Bereinigung merken
                }
                
                // Einfach den Dateinamen zurückgeben für relative Referenzierung
                return fileName;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Kopieren der PDF-Datei: {ex.Message}");
                return Path.GetFileName(pdfPath); // Im Fehlerfall nur den Dateinamen
            }
        }

        /// <summary>
        /// Vergleicht zwei Dateien auf Gleichheit
        /// </summary>
        private bool FileEquals(string path1, string path2)
        {
            if (!File.Exists(path1) || !File.Exists(path2))
                return false;

            if (new FileInfo(path1).Length != new FileInfo(path2).Length)
                return false;

            // Für große Dateien könnte man hier eine andere Strategie verfolgen
            return File.ReadAllBytes(path1).Equals(File.ReadAllBytes(path2));
        }

        /// <summary>
        /// Erstellt einen Assembly-Baum aus den übergebenen Daten
        /// </summary>
        /// <param name="treeData">Die Baumdaten als JSON-String oder Objekt</param>
        /// <returns>Der generierte HTML-String</returns>
        public string CreateAssemblyTree(object treeData)
        {
            string jsonData = treeData as string ?? Newtonsoft.Json.JsonConvert.SerializeObject(treeData, Newtonsoft.Json.Formatting.Indented);
            return GenerateHtmlContent(jsonData);
        }

        /// <summary>
        /// Bereinigt temporäre Dateien
        /// </summary>
        public void CleanupTempFiles()
        {
            foreach (string tempFile in _tempFiles)
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"Fehler beim Löschen der temporären Datei {tempFile}: {ex.Message}");
                }
            }
            
            _tempFiles.Clear();
        }
    }
} 
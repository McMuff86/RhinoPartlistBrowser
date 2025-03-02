using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using Rhino;
using Newtonsoft.Json;

namespace RhinoPartlistBrowser
{
    /// <summary>
    /// Klasse zum Anzeigen des Assembly-Baums in einem Browserfenster
    /// </summary>
    public class BrowserDisplay
    {
        private string _htmlFilePath;
        private string _outputDirectory;
        private List<string> _tempFiles = new List<string>();

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="outputDir">Optional: Benutzerdefiniertes Ausgabeverzeichnis</param>
        public BrowserDisplay(string outputDir = null)
        {
            // Ausgabeverzeichnis für die HTML-Dateien
            if (string.IsNullOrEmpty(outputDir))
            {
                _outputDirectory = Path.Combine(Path.GetTempPath(), "RhinoPartlistBrowser");
            }
            else
            {
                _outputDirectory = outputDir;
            }

            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        /// <summary>
        /// Generiert eine HTML-Datei aus den Assembly-Baum-Daten und öffnet sie im Browser
        /// </summary>
        /// <param name="assemblyTreeData">JSON-String mit den Assembly-Baum-Daten</param>
        /// <param name="attachments">Liste von Dateipfaden zu Anhängen (PDF, JPG, PNG)</param>
        public void DisplayAssemblyTreeInBrowser(string assemblyTreeData, List<string> attachments = null)
        {
            try
            {
                // HTML-Template laden und mit Daten füllen
                string htmlContent = GenerateHtmlContent(assemblyTreeData, attachments);
                
                // HTML-Datei im Ausgabeverzeichnis speichern
                _htmlFilePath = Path.Combine(_outputDirectory, $"assembly_tree_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                File.WriteAllText(_htmlFilePath, htmlContent);
                _tempFiles.Add(_htmlFilePath);
                
                // Anhänge kopieren, wenn ein benutzerdefiniertes Ausgabeverzeichnis verwendet wird
                if (attachments != null && !_outputDirectory.StartsWith(Path.GetTempPath()))
                {
                    foreach (var attachment in attachments)
                    {
                        if (File.Exists(attachment))
                        {
                            string destFile = Path.Combine(_outputDirectory, Path.GetFileName(attachment));
                            File.Copy(attachment, destFile, true);
                        }
                    }
                }
                
                // Browser öffnen
                Process.Start(new ProcessStartInfo
                {
                    FileName = _htmlFilePath,
                    UseShellExecute = true
                });
                
                RhinoApp.WriteLine($"Assembly-Baum wurde im Browser geöffnet: {_htmlFilePath}");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Öffnen des Browsers: {ex.Message}");
            }
        }

        /// <summary>
        /// Generiert den HTML-Inhalt für die Assembly-Baum-Anzeige
        /// </summary>
        /// <param name="assemblyTreeData">JSON-String mit den Assembly-Baum-Daten</param>
        /// <param name="attachments">Liste von Dateipfaden zu Anhängen (PDF, JPG, PNG)</param>
        /// <returns>HTML-Inhalt als String</returns>
        private string GenerateHtmlContent(string assemblyTreeData, List<string> attachments = null)
        {
            StringBuilder attachmentsScript = new StringBuilder();
            
            if (attachments != null && attachments.Count > 0)
            {
                attachmentsScript.AppendLine("      addAttachments([");
                
                for (int i = 0; i < attachments.Count; i++)
                {
                    string file = attachments[i];
                    string fileName = Path.GetFileName(file);
                    string ext = Path.GetExtension(file).ToLower();
                    
                    if (ext == ".pdf")
                    {
                        attachmentsScript.AppendLine($"        {{ type: 'pdf', path: '{fileName}', name: '{fileName}' }},");
                    }
                    else if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                    {
                        try
                        {
                            string base64Data = ConvertImageToBase64(file);
                            string mimeType = ext == ".png" ? "image/png" : "image/jpeg";
                            attachmentsScript.AppendLine($"        {{ type: 'image', data: 'data:{mimeType};base64,{base64Data}', name: '{fileName}' }},");
                        }
                        catch (Exception ex)
                        {
                            RhinoApp.WriteLine($"Fehler beim Einbetten des Bildes {fileName}: {ex.Message}");
                        }
                    }
                }
                
                attachmentsScript.AppendLine("      ]);");
            }

            return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>Assembly Tree</title>
  <style>
    body {{
      font-family: Arial, sans-serif;
      background-color: #f4f4f4;
      color: #333;
      padding: 20px;
    }}
    h1 {{
      color: #0055a5;
    }}
    p.summary {{
      font-size: 1.1em;
      margin-bottom: 20px;
    }}
    ul {{
      list-style-type: none;
      padding-left: 20px;
    }}
    li {{
      margin: 5px 0;
      background: #fff;
      padding: 8px 12px;
      border-radius: 4px;
      box-shadow: 0 1px 2px rgba(0,0,0,0.1);
    }}
    .toggle {{
      cursor: pointer;
    }}
    .toggle::before {{
      content: ""▸ "";
      color: #0055a5;
    }}
    .expanded::before {{
      content: ""▾ "";
      color: #0055a5;
    }}
    a {{
      color: #0055a5;
      text-decoration: none;
    }}
    a:hover {{
      text-decoration: underline;
    }}
    .attachment-section {{
      margin-top: 30px;
    }}
    .attachment-section h2 {{
      color: #0055a5;
    }}
    .embedded-image {{
      max-width: 90%;
      margin: 10px 0;
      border: 1px solid #ccc;
      padding: 4px;
      border-radius: 4px;
    }}
  </style>
</head>
<body>
  <h1>Assembly Tree for <span id=""assembly-name"">AssemblyName</span></h1>
  <p class=""summary"">Total Subassemblies: <span id=""total-subassemblies"">0</span>, Total Parts: <span id=""total-parts"">0</span></p>
  
  <ul id=""assembly-tree"">
    <!-- Hier wird die Baumstruktur dynamisch eingefügt -->
  </ul>

  <div class=""attachment-section"" id=""attachment-container"" style=""display: none;"">
    <h2>Attachments</h2>
    <ul id=""attachments-list"">
      <!-- Hier werden Anhänge eingefügt -->
    </ul>
  </div>

  <script>
    document.addEventListener(""DOMContentLoaded"", function(){{
      // Assembly-Baum-Daten als JSON
      const treeData = {assemblyTreeData};

      // Funktion zum Berechnen der Zusammenfassung
      function computeSummary(node) {{
        if (!node.children || node.children.length === 0) {{
          return {{ parts: node.count, subassemblies: 0 }};
        }} else {{
          let totalParts = 0;
          let totalSubassemblies = 0;
          
          for (const child of node.children) {{
            if (child.children && child.children.length > 0) {{
              totalSubassemblies += child.count;
            }}
            const childSummary = computeSummary(child);
            totalParts += childSummary.parts;
            totalSubassemblies += childSummary.subassemblies;
          }}
          
          return {{ parts: totalParts, subassemblies: totalSubassemblies }};
        }}
      }}

      // Funktion zum Generieren der HTML-Liste
      function generateHtmlList(node) {{
        const summary = node.children && node.children.length > 0 ? computeSummary(node) : null;
        let text = `${{node.name}} [${{node.type}}] x${{node.count}}`;
        
        if (summary) {{
          text += ` (Subassemblies: ${{summary.subassemblies}}, Parts: ${{summary.parts}})`;
        }}
        
        let html = ""<li>"";
        
        if (node.children && node.children.length > 0) {{
          html += `<span class=""toggle"">${{text}}</span><ul style=""display:none;"">`; 
          
          for (const child of node.children) {{
            html += generateHtmlList(child);
          }}
          
          html += ""</ul>"";
        }} else {{
          html += `<span>${{text}}</span>`;
        }}
        
        html += ""</li>"";
        return html;
      }}

      // Funktion zum Rendern des Baums
      function renderTree(data) {{
        document.getElementById('assembly-name').textContent = data.name;
        
        const summary = computeSummary(data);
        document.getElementById('total-subassemblies').textContent = summary.subassemblies;
        document.getElementById('total-parts').textContent = summary.parts;
        
        const treeContainer = document.getElementById('assembly-tree');
        treeContainer.innerHTML = generateHtmlList(data);
        
        // Event-Listener für Toggle-Elemente hinzufügen
        const toggles = document.querySelectorAll(""span.toggle"");
        toggles.forEach(function(toggle) {{
          toggle.addEventListener(""click"", function(e) {{
            const next = toggle.nextElementSibling;
            if (next && next.tagName.toLowerCase() === ""ul"") {{
              if (next.style.display === ""none"") {{
                next.style.display = ""block"";
                toggle.classList.add(""expanded"");
              }} else {{
                next.style.display = ""none"";
                toggle.classList.remove(""expanded"");
              }}
            }}
            e.stopPropagation();
          }});
        }});
      }}

      // Funktion zum Hinzufügen von Anhängen
      function addAttachments(attachments) {{
        if (attachments && attachments.length > 0) {{
          const attachmentContainer = document.getElementById('attachment-container');
          attachmentContainer.style.display = 'block';
          
          const attachmentsList = document.getElementById('attachments-list');
          
          for (const attachment of attachments) {{
            const li = document.createElement('li');
            
            if (attachment.type === 'pdf') {{
              li.innerHTML = `<a href=""${{attachment.path}}"" target=""_blank"">${{attachment.name}}</a>`;
            }} else if (attachment.type === 'image') {{
              li.innerHTML = `<img class=""embedded-image"" src=""${{attachment.data}}"" alt=""${{attachment.name}}"">`;
            }}
            
            attachmentsList.appendChild(li);
          }}
        }}
      }}

      // Assembly-Baum rendern
      renderTree(treeData);
      
      // Anhänge hinzufügen (wenn vorhanden)
      {attachmentsScript}
    }});
  </script>
</body>
</html>";
        }

        /// <summary>
        /// Konvertiert ein Bild in Base64
        /// </summary>
        /// <param name="imagePath">Pfad zum Bild</param>
        /// <returns>Base64-String</returns>
        private string ConvertImageToBase64(string imagePath)
        {
            using (Image image = Image.FromFile(imagePath))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // Bestimme das Format basierend auf der Dateiendung
                    ImageFormat format = ImageFormat.Jpeg;
                    string ext = Path.GetExtension(imagePath).ToLower();
                    if (ext == ".png")
                    {
                        format = ImageFormat.Png;
                    }
                    
                    // Speichere das Bild im MemoryStream
                    image.Save(memoryStream, format);
                    byte[] imageBytes = memoryStream.ToArray();
                    
                    // Konvertiere zu Base64
                    return Convert.ToBase64String(imageBytes);
                }
            }
        }

        /// <summary>
        /// Erstellt einen Assembly-Baum aus den JSON-Daten
        /// </summary>
        /// <param name="jsonData">JSON-String mit den Assembly-Baum-Daten</param>
        /// <returns>Assembly-Baum als JSON-String</returns>
        public string CreateAssemblyTree(object treeData)
        {
            return JsonConvert.SerializeObject(treeData);
        }

        /// <summary>
        /// Löscht temporäre Dateien
        /// </summary>
        public void CleanupTempFiles()
        {
            try
            {
                foreach (string file in _tempFiles)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                _tempFiles.Clear();
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Löschen temporärer Dateien: {ex.Message}");
            }
        }
    }
} 
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace RhinoPartlistBrowser
{
    /// <summary>
    /// Rhino-Befehl zum Anzeigen der Stückliste eines ausgewählten Blocks
    /// </summary>
    public class RParts : Command
    {
        public RParts()
        {
            // Rhino erstellt nur eine Instanz jeder Befehlsklasse, daher ist es sicher,
            // eine Referenz in einer statischen Eigenschaft zu speichern.
            Instance = this;
        }

        /// <summary>Die einzige Instanz dieses Befehls.</summary>
        public static RParts Instance { get; private set; }

        /// <summary>Der Name des Befehls, wie er in der Rhino-Befehlszeile erscheint.</summary>
        public override string EnglishName => "RParts";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // 1. Auswahl des Masterblocks
            RhinoApp.WriteLine("Bitte wählen Sie einen Block aus.");
            ObjRef objRef = null;
            var rc = RhinoGet.GetOneObject("Wählen Sie einen Block aus", false, ObjectType.InstanceReference, out objRef);
            if (rc != Result.Success || objRef == null)
                return rc;

            // Prüfen, ob das ausgewählte Objekt ein Block ist
            if (!(objRef.Object() is InstanceObject instanceObject))
            {
                RhinoApp.WriteLine("Das ausgewählte Objekt ist kein Block.");
                return Result.Failure;
            }

            // 2. Option: Explodieren der terminalen Blockdefinitionen?
            var explode = false;
            var optResult = RhinoGet.GetBool("Terminale Blocks in einzelne Geometrieobjekte zerlegen?", true, "Nein", "Ja", ref explode);
            if (optResult != Result.Success)
                return optResult;

            // 3. Auswahl eines Ausgabeverzeichnisses via FolderBrowserDialog
            string outputFolder = null;
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Wählen Sie ein Ausgabeverzeichnis";
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    outputFolder = folderDialog.SelectedPath;
                }
                else
                {
                    RhinoApp.WriteLine("Kein Ausgabeverzeichnis ausgewählt. Es wird ein temporäres Verzeichnis verwendet.");
                }
            }

            try
            {
                // BOMGenerator erstellen und Stückliste generieren
                BOMGenerator bomGenerator = new BOMGenerator(doc);
                string bomData = bomGenerator.GenerateBOM(instanceObject, explode);

                // 4. Option: Anhänge hinzufügen?
                List<string> attachments = null;
                bool addAttachments = false;
                var attachResult = RhinoGet.GetBool("Anhänge hinzufügen (PDF, JPG, PNG)?", true, "Nein", "Ja", ref addAttachments);
                if (attachResult == Result.Success && addAttachments)
                {
                    attachments = new List<string>();
                    using (var openFileDialog = new OpenFileDialog())
                    {
                        openFileDialog.Title = "Wählen Sie Dateien zum Anhängen aus";
                        openFileDialog.Filter = "Unterstützte Dateien (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png|PDF-Dateien (*.pdf)|*.pdf|JPEG-Dateien (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG-Dateien (*.png)|*.png|Alle Dateien (*.*)|*.*";
                        openFileDialog.Multiselect = true;

                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            attachments.AddRange(openFileDialog.FileNames);
                        }
                    }
                }

                // BrowserDisplay erstellen und Assembly-Baum im Browser anzeigen
                BrowserDisplay browserDisplay = new BrowserDisplay(outputFolder);
                browserDisplay.DisplayAssemblyTreeInBrowser(bomData, attachments);

                RhinoApp.WriteLine("Assembly-Baum wurde im Browser geöffnet.");
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler: {ex.Message}");
                return Result.Failure;
            }
        }
    }
} 
using System;
using System.Collections.Generic;
using System.IO;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;

namespace RhinoPartlistBrowser
{
    public class RParts : Command
    {
        public RParts()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static RParts Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "RParts";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Auswahl eines Blocks
            InstanceObject instanceObject = null;
            
            // Startpunkt des Befehls
            var go = new GetObject();
            go.SetCommandPrompt("Block-Instanz für Stückliste auswählen");
            go.GeometryFilter = ObjectType.InstanceReference;
            go.SubObjectSelect = false;
            go.EnablePreSelect(true, true);

            while (true)
            {
                GetResult getResult = go.Get();
                if (getResult == GetResult.Cancel)
                    return Result.Cancel;

                if (getResult == GetResult.Object)
                {
                    ObjRef objRef = go.Object(0);
                    instanceObject = objRef.Object() as InstanceObject;
                    if (instanceObject != null)
                        break;
                }

                RhinoApp.WriteLine("Bitte wählen Sie eine Block-Instanz.");
            }

            // Block explodieren Option
            bool explode = false;
            var optionResult = RhinoGet.GetBool("Blöcke explodieren", false, "Nein", "Ja", ref explode);
            if (optionResult != Result.Success)
                return Result.Cancel;

            // Anhänge hinzufügen Option
            bool includeAttachments = false;
            optionResult = RhinoGet.GetBool("Anhänge hinzufügen (PDF, Bilder)", false, "Nein", "Ja", ref includeAttachments);
            if (optionResult != Result.Success)
                return Result.Cancel;

            // BOM-Generator erstellen
            var bomGenerator = new BOMGenerator(doc);

            // Baumstruktur generieren
            string treeJson = bomGenerator.GenerateBOM(instanceObject, explode);

            // Anhänge sammeln, wenn gewünscht
            List<string> attachmentFiles = null;
            if (includeAttachments)
            {
                attachmentFiles = GetAttachmentFiles();
                if (attachmentFiles == null)
                    return Result.Cancel;
            }

            // Exportieren
            bool exportToDisk = false;
            optionResult = RhinoGet.GetBool("Exportdateien speichern", false, "Nein", "Ja", ref exportToDisk);
            if (optionResult != Result.Success)
                return Result.Cancel;

            // Ausgabeverzeichnis, falls Export gewünscht
            string outputDir = null;
            if (exportToDisk)
            {
                // Ausgabeverzeichnis auswählen
                string defaultPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                outputDir = SelectFolder("Ausgabeverzeichnis auswählen", defaultPath);
                if (string.IsNullOrEmpty(outputDir))
                    return Result.Cancel;

                // CSV-Export
                bool exportCsv = false;
                optionResult = RhinoGet.GetBool("CSV-Datei exportieren", false, "Nein", "Ja", ref exportCsv);
                if (optionResult == Result.Success && exportCsv)
                {
                    string csvPath = Path.Combine(outputDir, $"BOM_{instanceObject.InstanceDefinition.Name}_{DateTime.Now:yyyyMMdd}.csv");
                    bomGenerator.ExportToCSV(instanceObject, explode, csvPath);
                    
                    // Blattknoten als separate CSV
                    string leafCsvPath = Path.Combine(outputDir, $"BOM_LeafNodes_{instanceObject.InstanceDefinition.Name}_{DateTime.Now:yyyyMMdd}.csv");
                    bomGenerator.ExportLeafNodesToCSV(instanceObject, explode, leafCsvPath);
                }

                // JSON-Export
                bool exportJson = false;
                optionResult = RhinoGet.GetBool("JSON-Datei exportieren", false, "Nein", "Ja", ref exportJson);
                if (optionResult == Result.Success && exportJson)
                {
                    string jsonPath = Path.Combine(outputDir, $"BOM_{instanceObject.InstanceDefinition.Name}_{DateTime.Now:yyyyMMdd}.json");
                    bomGenerator.ExportToJSON(instanceObject, explode, jsonPath);
                    
                    // Blattknoten als separate JSON
                    string leafJsonPath = Path.Combine(outputDir, $"BOM_LeafNodes_{instanceObject.InstanceDefinition.Name}_{DateTime.Now:yyyyMMdd}.json");
                    bomGenerator.ExportLeafNodesToJSON(instanceObject, explode, leafJsonPath);
                }
            }

            // Anzeige im Browser
            var browserDisplay = new BrowserDisplay(outputDir);
            try
            {
                browserDisplay.DisplayAssemblyTreeInBrowser(treeJson, attachmentFiles);
            }
            finally
            {
                // Temporäre Dateien bereinigen, es sei denn, wir haben einen Ausgabeordner angegeben
                if (string.IsNullOrEmpty(outputDir))
                {
                    browserDisplay.CleanupTempFiles();
                }
            }

            return Result.Success;
        }

        /// <summary>
        /// Lässt den Benutzer PDF- und Bilddateien auswählen
        /// </summary>
        /// <returns>Liste der ausgewählten Dateien oder null bei Abbruch</returns>
        private List<string> GetAttachmentFiles()
        {
            var attachmentFiles = new List<string>();
            
            // PDFs hinzufügen
            bool addPdfs = false;
            var optionResult = RhinoGet.GetBool("PDF-Dateien hinzufügen", false, "Nein", "Ja", ref addPdfs);
            if (optionResult != Result.Success)
                return null;

            if (addPdfs)
            {
                // Mehrere PDF-Dateien auswählen
                string[] pdfFiles = SelectFiles("PDF-Dateien auswählen", "PDF-Dateien (*.pdf)|*.pdf", true);
                if (pdfFiles != null)
                {
                    attachmentFiles.AddRange(pdfFiles);
                }
            }

            // Bilder hinzufügen
            bool addImages = false;
            optionResult = RhinoGet.GetBool("Bilder hinzufügen", false, "Nein", "Ja", ref addImages);
            if (optionResult != Result.Success)
                return null;

            if (addImages)
            {
                // Mehrere Bilddateien auswählen
                string[] imageFiles = SelectFiles("Bilder auswählen", "Bilddateien (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp", true);
                if (imageFiles != null)
                {
                    attachmentFiles.AddRange(imageFiles);
                }
            }

            return attachmentFiles;
        }

        /// <summary>
        /// Lässt den Benutzer eine oder mehrere Dateien auswählen
        /// </summary>
        /// <param name="title">Titel des Dialogs</param>
        /// <param name="filter">Dateifilter</param>
        /// <param name="multiselect">Mehrfachauswahl erlauben</param>
        /// <returns>Array der ausgewählten Dateipfade oder null bei Abbruch</returns>
        private string[] SelectFiles(string title, string filter, bool multiselect = false)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.OpenFileDialog())
                {
                    dialog.Title = title;
                    dialog.Filter = filter;
                    dialog.Multiselect = multiselect;

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        return dialog.FileNames;
                    }
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Öffnen des Dateiauswahldialogs: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Lässt den Benutzer einen Ordner auswählen
        /// </summary>
        /// <param name="title">Titel des Dialogs</param>
        /// <param name="initialFolder">Anfangsordner</param>
        /// <returns>Pfad des ausgewählten Ordners oder null bei Abbruch</returns>
        private string SelectFolder(string title, string initialFolder)
        {
            try
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = title;
                    dialog.SelectedPath = initialFolder;
                    dialog.ShowNewFolderButton = true;

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        return dialog.SelectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Öffnen des Ordnerauswahldialogs: {ex.Message}");
            }

            return null;
        }
    }
}

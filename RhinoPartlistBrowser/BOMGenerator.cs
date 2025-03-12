using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Newtonsoft.Json;

namespace RhinoPartlistBrowser
{
    /// <summary>
    /// Klasse für die Erzeugung der Bill of Materials aus Rhino-Objekten
    /// Basiert auf dem Python-Skript Rhino_AssemblyTree_v0.4.py
    /// </summary>
    public class BOMGenerator
    {
        private readonly RhinoDoc _doc;

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="doc">Das aktuelle Rhino-Dokument</param>
        public BOMGenerator(RhinoDoc doc)
        {
            _doc = doc;
        }

        /// <summary>
        /// Datenstruktur für den Assembly-Baum
        /// </summary>
        public class AssemblyNode
        {
            /// <summary>Name des Knotens</summary>
            public string Name { get; set; }
            
            /// <summary>Name aus dem Namensfeld (wenn vorhanden)</summary>
            public string DisplayName { get; set; }
            
            /// <summary>Typ des Knotens (z.B. "MasterBlock", "BlockInstance", "Geometry")</summary>
            public string NodeType { get; set; }
            
            /// <summary>Anzahl der Vorkommen (Aggregation)</summary>
            public int Count { get; set; }
            
            /// <summary>Benutzerdefinierte Attribute (UserText) für diesen Knoten</summary>
            public Dictionary<string, string> UserAttributes { get; } = new Dictionary<string, string>();
            
            /// <summary>Untergeordnete Knoten</summary>
            public Dictionary<(string, string), AssemblyNode> Children { get; } = new Dictionary<(string, string), AssemblyNode>();

            /// <summary>
            /// Konstruktor
            /// </summary>
            /// <param name="name">Name des Knotens</param>
            /// <param name="nodeType">Typ des Knotens</param>
            public AssemblyNode(string name, string nodeType)
            {
                Name = name;
                NodeType = nodeType;
                Count = 1;
                DisplayName = null;
            }

            /// <summary>
            /// Konstruktor mit DisplayName
            /// </summary>
            /// <param name="name">Name des Knotens</param>
            /// <param name="displayName">Zusätzlicher Anzeigename</param>
            /// <param name="nodeType">Typ des Knotens</param>
            public AssemblyNode(string name, string displayName, string nodeType)
            {
                Name = name;
                DisplayName = displayName;
                NodeType = nodeType;
                Count = 1;
            }

            /// <summary>
            /// Fügt ein Kind hinzu oder gibt ein vorhandenes zurück
            /// </summary>
            /// <param name="name">Name des Kindes</param>
            /// <param name="nodeType">Typ des Kindes</param>
            /// <returns>Das Kind-Node</returns>
            public AssemblyNode AddOrGetChild(string name, string nodeType)
            {
                var key = (name, nodeType);
                if (Children.ContainsKey(key))
                {
                    Children[key].Count++;
                    return Children[key];
                }
                else
                {
                    var newChild = new AssemblyNode(name, nodeType);
                    Children[key] = newChild;
                    return newChild;
                }
            }

            /// <summary>
            /// Fügt ein Kind hinzu oder gibt ein vorhandenes zurück, mit DisplayName
            /// </summary>
            /// <param name="name">Name des Kindes</param>
            /// <param name="displayName">Zusätzlicher Anzeigename</param>
            /// <param name="nodeType">Typ des Kindes</param>
            /// <returns>Das Kind-Node</returns>
            public AssemblyNode AddOrGetChild(string name, string displayName, string nodeType)
            {
                var key = (name, nodeType);
                if (Children.ContainsKey(key))
                {
                    Children[key].Count++;
                    return Children[key];
                }
                else
                {
                    var newChild = new AssemblyNode(name, displayName, nodeType);
                    Children[key] = newChild;
                    return newChild;
                }
            }
        }

        /// <summary>
        /// Baut den Assembly-Baum rekursiv auf
        /// </summary>
        /// <param name="instDefIndex">Index der Instance-Definition</param>
        /// <param name="explode">Ob die termonierten Blöcke in einzelne Geometrieobjekte zerlegt werden sollen</param>
        /// <param name="parentNode">Der übergeordnete Knoten</param>
        private void BuildTree(int instDefIndex, bool explode, AssemblyNode parentNode)
        {
            InstanceDefinition instDef = _doc.InstanceDefinitions[instDefIndex];
            bool nestedFound = instDef.GetObjects().Any(obj => obj is InstanceObject);

            if (nestedFound)
            {
                foreach (var obj in instDef.GetObjects())
                {
                    if (obj is InstanceObject instanceObj)
                    {
                        int childInstDefIndex = instanceObj.InstanceDefinition.Index;
                        InstanceDefinition childInstDef = _doc.InstanceDefinitions[childInstDefIndex];
                        
                        // Namensfeld-Wert extrahieren, falls vorhanden
                        string displayName = null;
                        if (!string.IsNullOrEmpty(instanceObj.Name))
                        {
                            displayName = instanceObj.Name;
                        }
                        
                        // Wenn wir einen DisplayName haben und es sich um einen Leaf-Node handelt,
                        // verwenden wir die Methode mit dem zusätzlichen DisplayName-Parameter
                        bool isLeafNode = !childInstDef.GetObjects().Any(o => o is InstanceObject);
                        
                        AssemblyNode childNode;
                        if (isLeafNode && !string.IsNullOrEmpty(displayName))
                        {
                            childNode = parentNode.AddOrGetChild(childInstDef.Name, displayName, "BlockInstance");
                        }
                        else
                        {
                            childNode = parentNode.AddOrGetChild(childInstDef.Name, "BlockInstance");
                        }
                        
                        // UserText-Attribute extrahieren, falls es sich um einen Leaf-Node handelt,
                        // unabhängig davon, ob ein DisplayName vorhanden ist
                        if (isLeafNode)
                        {
                            ExtractUserTextAttributes(instanceObj, childNode);
                        }
                        
                        BuildTree(childInstDefIndex, explode, childNode);
                    }
                    else
                    {
                        if (explode)
                        {
                            GeometryBase geom = obj.Geometry;
                            string geomType = geom != null ? geom.GetType().Name : "Unknown";
                            
                            // Namensfeld-Wert für normale Geometrie
                            string displayName = null;
                            AssemblyNode geometryNode;
                            
                            if (obj is RhinoObject rhinoObj && !string.IsNullOrEmpty(rhinoObj.Name))
                            {
                                displayName = rhinoObj.Name;
                                geometryNode = parentNode.AddOrGetChild($"Geometry ({geomType})", displayName, "Geometry");
                            }
                            else
                            {
                                geometryNode = parentNode.AddOrGetChild($"Geometry ({geomType})", "Geometry");
                            }
                            
                            // UserText-Attribute extrahieren, falls das Objekt ein RhinoObject ist,
                            // unabhängig davon, ob ein DisplayName vorhanden ist
                            if (obj is RhinoObject rhinoObject)
                            {
                                ExtractUserTextAttributes(rhinoObject, geometryNode);
                            }
                        }
                    }
                }
            }
            else
            {
                if (explode)
                {
                    foreach (var obj in instDef.GetObjects())
                    {
                        if (!(obj is InstanceObject))
                        {
                            GeometryBase geom = obj.Geometry;
                            string geomType = geom != null ? geom.GetType().Name : "Unknown";
                            
                            // Namensfeld-Wert für normale Geometrie
                            string displayName = null;
                            AssemblyNode geometryNode;
                            
                            if (obj is RhinoObject rhinoObj && !string.IsNullOrEmpty(rhinoObj.Name))
                            {
                                displayName = rhinoObj.Name;
                                geometryNode = parentNode.AddOrGetChild($"Geometry ({geomType})", displayName, "Geometry");
                            }
                            else
                            {
                                geometryNode = parentNode.AddOrGetChild($"Geometry ({geomType})", "Geometry");
                            }
                            
                            // UserText-Attribute extrahieren, falls das Objekt ein RhinoObject ist,
                            // unabhängig davon, ob ein DisplayName vorhanden ist
                            if (obj is RhinoObject rhinoObject)
                            {
                                ExtractUserTextAttributes(rhinoObject, geometryNode);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Baut den Assembly-Baum für einen Hauptblock auf
        /// </summary>
        /// <param name="masterInstDefIndex">Index der Hauptblock-Definition</param>
        /// <param name="explode">Ob die termonierten Blöcke in einzelne Geometrieobjekte zerlegt werden sollen</param>
        /// <returns>Der erstellte Assembly-Baum</returns>
        public AssemblyNode BuildAssemblyTree(int masterInstDefIndex, bool explode)
        {
            InstanceDefinition masterInstDef = _doc.InstanceDefinitions[masterInstDefIndex];
            var root = new AssemblyNode(masterInstDef.Name, "MasterBlock");
            BuildTree(masterInstDefIndex, explode, root);
            return root;
        }

        /// <summary>
        /// Berechnet eine effektive Zusammenfassung für den Assembly-Baum
        /// </summary>
        /// <param name="node">Der zu analysierende Knoten</param>
        /// <returns>Ein Dictionary mit der Anzahl der Teile und Untergruppen</returns>
        public Dictionary<string, int> ComputeEffectiveSummary(AssemblyNode node)
        {
            if (node.Children.Count == 0)
            {
                return new Dictionary<string, int>
                {
                    { "parts", node.Count },
                    { "subassemblies", 0 }
                };
            }
            else
            {
                int totalParts = 0;
                int totalSubassemblies = 0;

                foreach (var child in node.Children.Values)
                {
                    if (child.Children.Count > 0)
                    {
                        totalSubassemblies += child.Count;
                    }

                    var childSummary = ComputeEffectiveSummary(child);
                    totalParts += childSummary["parts"];
                    totalSubassemblies += childSummary["subassemblies"];
                }

                return new Dictionary<string, int>
                {
                    { "parts", totalParts },
                    { "subassemblies", totalSubassemblies }
                };
            }
        }

        /// <summary>
        /// Sammelt alle Blattknoten (ohne Kinder) aus dem Assembly-Baum
        /// </summary>
        /// <param name="node">Der zu durchsuchende Knoten</param>
        /// <param name="leafDict">Optional: Ein vorhandenes Dictionary für die Blattknoten</param>
        /// <returns>Ein Dictionary mit den Blattknoten und ihrer Anzahl</returns>
        public Dictionary<(string, string), int> CollectLeafNodes(AssemblyNode node, Dictionary<(string, string), int> leafDict = null)
        {
            if (leafDict == null)
            {
                leafDict = new Dictionary<(string, string), int>();
            }

            if (node.Children.Count == 0)
            {
                var key = (node.Name, node.NodeType);
                if (leafDict.ContainsKey(key))
                {
                    leafDict[key] += node.Count;
                }
                else
                {
                    leafDict[key] = node.Count;
                }
            }
            else
            {
                foreach (var child in node.Children.Values)
                {
                    CollectLeafNodes(child, leafDict);
                }
            }

            return leafDict;
        }

        /// <summary>
        /// Konvertiert einen AssemblyNode in ein Dictionary zur JSON-Serialisierung
        /// </summary>
        /// <param name="node">Der zu konvertierende Knoten</param>
        /// <returns>Ein Dictionary, das mit JSON.NET serialisiert werden kann</returns>
        public Dictionary<string, object> AssemblyNodeToDict(AssemblyNode node)
        {
            var result = new Dictionary<string, object>
            {
                { "name", node.Name },
                { "type", node.NodeType },
                { "count", node.Count },
                { "children", node.Children.Values.Select(child => AssemblyNodeToDict(child)).ToList() }
            };

            // DisplayName hinzufügen, falls vorhanden
            if (!string.IsNullOrEmpty(node.DisplayName))
            {
                result.Add("displayName", node.DisplayName);
            }
            
            // UserAttributes hinzufügen, falls vorhanden
            if (node.UserAttributes.Count > 0)
            {
                result.Add("userAttributes", node.UserAttributes);
            }

            return result;
        }

        /// <summary>
        /// Erstellt eine Stückliste aus dem ausgewählten Block
        /// </summary>
        /// <param name="instanceObject">Das ausgewählte Block-Objekt</param>
        /// <param name="explode">Ob die termonierten Blöcke in einzelne Geometrieobjekte zerlegt werden sollen</param>
        /// <returns>JSON-String mit den BOM-Daten</returns>
        public string GenerateBOM(InstanceObject instanceObject, bool explode)
        {
            try
            {
                // Assembly-Baum erstellen
                int masterInstDefIndex = instanceObject.InstanceDefinition.Index;
                AssemblyNode aggregatedTree = BuildAssemblyTree(masterInstDefIndex, explode);

                // Zusammenfassung berechnen
                Dictionary<string, int> summary = ComputeEffectiveSummary(aggregatedTree);

                // Blattknoten sammeln (für flache Stückliste)
                Dictionary<(string, string), int> leafNodes = CollectLeafNodes(aggregatedTree);
                var leafList = new List<Dictionary<string, object>>();
                foreach (var item in leafNodes)
                {
                    leafList.Add(new Dictionary<string, object>
                    {
                        { "name", item.Key.Item1 },
                        { "type", item.Key.Item2 },
                        { "count", item.Value }
                    });
                }

                // Gesamten Baum in ein Dictionary konvertieren
                var treeDict = AssemblyNodeToDict(aggregatedTree);

                // JSON-String erstellen
                return JsonConvert.SerializeObject(treeDict, Formatting.Indented);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler bei der BOM-Generierung: {ex.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// Erzeugt eine CSV-Datei aus dem Assembly-Baum
        /// </summary>
        /// <param name="instanceObject">Das ausgewählte Block-Objekt</param>
        /// <param name="explode">Ob die termonierten Blöcke in einzelne Geometrieobjekte zerlegt werden sollen</param>
        /// <param name="filePath">Pfad zum Speichern der CSV-Datei</param>
        /// <returns>True, wenn erfolgreich, sonst False</returns>
        public bool ExportToCSV(InstanceObject instanceObject, bool explode, string filePath)
        {
            try
            {
                // Flache Liste der Teile erzeugen
                List<string> outputLines = new List<string>();
                outputLines.Add("Level,Parent,Name,Type");

                // Hilfsfunktion, um rekursiv durch den Baum zu navigieren
                void ProcessBlock(int instDefIndex, int level, List<string> lines, bool explodeBlocks, string parentName = "None", List<int> visited = null)
                {
                    InstanceDefinition instDef;
                    
                    if (visited == null)
                        visited = new List<int>();

                    if (visited.Contains(instDefIndex))
                    {
                        instDef = _doc.InstanceDefinitions[instDefIndex];
                        lines.Add($"{level},{parentName},[Cycle detected: {instDef.Name}],Cycle");
                        return;
                    }

                    visited.Add(instDefIndex);
                    instDef = _doc.InstanceDefinitions[instDefIndex];
                    bool hasNested = instDef.GetObjects().Any(obj => obj is InstanceObject);

                    if (!hasNested)
                    {
                        if (explodeBlocks)
                        {
                            foreach (var obj in instDef.GetObjects())
                            {
                                if (!(obj is InstanceObject))
                                {
                                    GeometryBase geom = obj.Geometry;
                                    string geomType = geom != null ? geom.GetType().Name : "Unknown";
                                    lines.Add($"{level},{parentName},{instDef.Name} (Geometry: {geomType}),Geometry");
                                }
                            }
                        }
                        else
                        {
                            lines.Add($"{level},{parentName},{instDef.Name},Block");
                        }
                        visited.Remove(instDefIndex);
                        return;
                    }

                    foreach (var obj in instDef.GetObjects())
                    {
                        if (obj is InstanceObject instanceObj)
                        {
                            int childInstDefIndex = instanceObj.InstanceDefinition.Index;
                            InstanceDefinition childInstDef = _doc.InstanceDefinitions[childInstDefIndex];
                            lines.Add($"{level},{instDef.Name},{childInstDef.Name},BlockInstance");
                            ProcessBlock(childInstDefIndex, level + 1, lines, explodeBlocks, instDef.Name, new List<int>(visited));
                        }
                        else
                        {
                            GeometryBase geom = obj.Geometry;
                            string geomType = geom != null ? geom.GetType().Name : "Unknown";
                            lines.Add($"{level},{instDef.Name},Geometry ({geomType}),Geometry");
                        }
                    }

                    visited.Remove(instDefIndex);
                }

                // Verarbeitung starten
                ProcessBlock(instanceObject.InstanceDefinition.Index, 0, outputLines, explode);

                // CSV-Datei speichern
                System.IO.File.WriteAllLines(filePath, outputLines);

                RhinoApp.WriteLine($"CSV-Stückliste wurde gespeichert: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Exportieren der CSV-Datei: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Exportiert die Blattknoten (Teile ohne Kinder) als CSV-Datei
        /// </summary>
        /// <param name="instanceObject">Das ausgewählte Block-Objekt</param>
        /// <param name="explode">Ob die termonierten Blöcke in einzelne Geometrieobjekte zerlegt werden sollen</param>
        /// <param name="filePath">Pfad zum Speichern der CSV-Datei</param>
        /// <returns>True, wenn erfolgreich, sonst False</returns>
        public bool ExportLeafNodesToCSV(InstanceObject instanceObject, bool explode, string filePath)
        {
            try
            {
                // Assembly-Baum erstellen
                int masterInstDefIndex = instanceObject.InstanceDefinition.Index;
                AssemblyNode aggregatedTree = BuildAssemblyTree(masterInstDefIndex, explode);

                // Blattknoten sammeln
                Dictionary<(string, string), int> leafNodes = CollectLeafNodes(aggregatedTree);

                // CSV-Zeilen erstellen
                List<string> leafCsvLines = new List<string>();
                leafCsvLines.Add("Name,Type,Count");

                foreach (var item in leafNodes)
                {
                    leafCsvLines.Add($"{item.Key.Item1},{item.Key.Item2},{item.Value}");
                }

                // CSV-Datei speichern
                System.IO.File.WriteAllLines(filePath, leafCsvLines);

                RhinoApp.WriteLine($"Blattknoten-CSV wurde gespeichert: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Exportieren der Blattknoten-CSV: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Exportiert den Assembly-Baum als JSON-Datei
        /// </summary>
        /// <param name="instanceObject">Das ausgewählte Block-Objekt</param>
        /// <param name="explode">Ob die termonierten Blöcke in einzelne Geometrieobjekte zerlegt werden sollen</param>
        /// <param name="filePath">Pfad zum Speichern der JSON-Datei</param>
        /// <returns>True, wenn erfolgreich, sonst False</returns>
        public bool ExportToJSON(InstanceObject instanceObject, bool explode, string filePath)
        {
            try
            {
                string bomData = GenerateBOM(instanceObject, explode);
                System.IO.File.WriteAllText(filePath, bomData);
                RhinoApp.WriteLine($"JSON-Datei wurde gespeichert: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Exportieren der JSON-Datei: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Exportiert die Blattknoten (Teile ohne Kinder) als JSON-Datei
        /// </summary>
        /// <param name="instanceObject">Das ausgewählte Block-Objekt</param>
        /// <param name="explode">Ob die termonierten Blöcke in einzelne Geometrieobjekte zerlegt werden sollen</param>
        /// <param name="filePath">Pfad zum Speichern der JSON-Datei</param>
        /// <returns>True, wenn erfolgreich, sonst False</returns>
        public bool ExportLeafNodesToJSON(InstanceObject instanceObject, bool explode, string filePath)
        {
            try
            {
                // Assembly-Baum erstellen
                int masterInstDefIndex = instanceObject.InstanceDefinition.Index;
                AssemblyNode aggregatedTree = BuildAssemblyTree(masterInstDefIndex, explode);

                // Blattknoten sammeln
                Dictionary<(string, string), int> leafNodes = CollectLeafNodes(aggregatedTree);
                var leafList = new List<Dictionary<string, object>>();
                foreach (var item in leafNodes)
                {
                    leafList.Add(new Dictionary<string, object>
                    {
                        { "name", item.Key.Item1 },
                        { "type", item.Key.Item2 },
                        { "count", item.Value }
                    });
                }

                // JSON-String erstellen und speichern
                string jsonData = JsonConvert.SerializeObject(leafList, Formatting.Indented);
                System.IO.File.WriteAllText(filePath, jsonData);

                RhinoApp.WriteLine($"Blattknoten-JSON wurde gespeichert: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Exportieren der Blattknoten-JSON: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extrahiert UserText-Attribute aus einem Rhino-Objekt und fügt sie zu einem AssemblyNode hinzu
        /// </summary>
        /// <param name="rhinoObj">Das Rhino-Objekt</param>
        /// <param name="node">Der AssemblyNode</param>
        private void ExtractUserTextAttributes(RhinoObject rhinoObj, AssemblyNode node)
        {
            // In Rhino 7/8 werden benutzerdefinierte Attribute über GetUserStrings() abgerufen
            var userStrings = rhinoObj.Attributes.GetUserStrings();
            if (userStrings != null && userStrings.Count > 0)
            {
                foreach (string key in userStrings.AllKeys)
                {
                    // Spezielle interne Keys ignorieren
                    if (key == "$block-instance-original-object-id$")
                        continue;
                        
                    string value = userStrings[key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        node.UserAttributes[key] = value;
                    }
                }
            }
        }
    }
} 
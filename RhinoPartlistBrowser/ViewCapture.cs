using System;
using System.Collections.Generic;
using System.IO;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Display;

namespace RhinoPartlistBrowser
{
    /// <summary>
    /// Klasse für die Aufnahme von Viewport-Ansichten eines Blocks
    /// </summary>
    public class ViewCapture
    {
        private readonly RhinoDoc _doc;
        private readonly string _outputDirectory;
        private readonly List<Guid> _hiddenObjects = new List<Guid>();
        private readonly List<string> _capturedImages = new List<string>();

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="doc">Das aktuelle RhinoDoc</param>
        /// <param name="outputDir">Ausgabeverzeichnis für die Bilder</param>
        public ViewCapture(RhinoDoc doc, string outputDir)
        {
            _doc = doc;
            _outputDirectory = outputDir;
        }

        /// <summary>
        /// Erstellt Ansichten eines Blocks aus verschiedenen Perspektiven
        /// </summary>
        /// <param name="instanceObject">Die Block-Instanz</param>
        /// <returns>Liste der erstellten Bilddateien</returns>
        public List<string> CaptureBlockViews(InstanceObject instanceObject)
        {
            try
            {
                // Alle anderen Objekte ausblenden
                HideOtherObjects(instanceObject);

                // BoundingBox des Blocks ermitteln
                BoundingBox bbox = instanceObject.Geometry.GetBoundingBox(true);
                
                // Verschiedene Ansichten aufnehmen
                CaptureView(instanceObject, ViewportOrientation.Front, "Front");
                CaptureView(instanceObject, ViewportOrientation.Left, "Left");
                CaptureView(instanceObject, ViewportOrientation.Right, "Right");
                CaptureView(instanceObject, ViewportOrientation.Top, "Top");

                // Objekte wieder einblenden
                ShowHiddenObjects();

                return _capturedImages;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Fehler beim Erstellen der Ansichten: {ex.Message}");
                ShowHiddenObjects(); // Sicherstellen, dass Objekte wieder eingeblendet werden
                return new List<string>();
            }
        }

        /// <summary>
        /// Blendet alle Objekte außer dem ausgewählten Block aus
        /// </summary>
        private void HideOtherObjects(InstanceObject selectedBlock)
        {
            _hiddenObjects.Clear();
            
            foreach (RhinoObject obj in _doc.Objects)
            {
                if (obj.Id != selectedBlock.Id)
                {
                    if (obj.Visible)
                    {
                        _doc.Objects.Hide(obj.Id, true);
                        _hiddenObjects.Add(obj.Id);
                    }
                }
            }
            _doc.Views.Redraw();
        }

        /// <summary>
        /// Blendet zuvor ausgeblendete Objekte wieder ein
        /// </summary>
        private void ShowHiddenObjects()
        {
            foreach (Guid id in _hiddenObjects)
            {
                _doc.Objects.Show(id, true);
            }
            _hiddenObjects.Clear();
            _doc.Views.Redraw();
        }

        /// <summary>
        /// Nimmt eine Ansicht des Blocks aus einer bestimmten Perspektive auf
        /// </summary>
        private void CaptureView(InstanceObject instanceObject, ViewportOrientation orientation, string viewName)
        {
            // Aktiven Viewport holen
            RhinoView view = _doc.Views.ActiveView;
            if (view == null) return;

            // Viewport-Einstellungen speichern
            var viewport = view.ActiveViewport;
            var cameraLocation = viewport.CameraLocation;
            var cameraDirection = viewport.CameraDirection;
            var cameraUp = viewport.CameraUp;
            
            // Selektionszustand speichern und Block deselektieren
            bool wasSelected = instanceObject.IsSelected(true) > 0;
            if (wasSelected)
            {
                instanceObject.Select(false);
                _doc.Views.Redraw();
            }
            
            try
            {
                // Ansicht einstellen
                switch (orientation)
                {
                    case ViewportOrientation.Front:
                        viewport.SetCameraLocation(new Point3d(0, -100, 0), true);
                        viewport.SetCameraDirection(new Vector3d(0, 1, 0), true);
                        viewport.CameraUp = Vector3d.ZAxis;
                        break;
                    case ViewportOrientation.Left:
                        viewport.SetCameraLocation(new Point3d(-100, 0, 0), true);
                        viewport.SetCameraDirection(new Vector3d(1, 0, 0), true);
                        viewport.CameraUp = Vector3d.ZAxis;
                        break;
                    case ViewportOrientation.Right:
                        viewport.SetCameraLocation(new Point3d(100, 0, 0), true);
                        viewport.SetCameraDirection(new Vector3d(-1, 0, 0), true);
                        viewport.CameraUp = Vector3d.ZAxis;
                        break;
                    case ViewportOrientation.Top:
                        viewport.SetCameraLocation(new Point3d(0, 0, 100), true);
                        viewport.SetCameraDirection(new Vector3d(0, 0, -1), true);
                        viewport.CameraUp = Vector3d.YAxis;
                        break;
                }

                // Auf Block zoomen
                viewport.ZoomBoundingBox(instanceObject.Geometry.GetBoundingBox(true));
                
                // Etwas herauszoomen für besseren Überblick
                viewport.Magnify(0.9, true);

                _doc.Views.Redraw();

                // Bild aufnehmen
                string fileName = $"block_view_{viewName}_{DateTime.Now:yyyyMMddHHmmss}.png";
                string filePath = Path.Combine(_outputDirectory, fileName);

                // Viewport-Größe für die Aufnahme anpassen
                var size = new System.Drawing.Size(1024, 768);
                var bitmap = view.CaptureToBitmap(size);
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                bitmap.Dispose();
                
                _capturedImages.Add(filePath);

                RhinoApp.WriteLine($"Ansicht {viewName} wurde gespeichert: {filePath}");
            }
            finally
            {
                // Kamera-Einstellungen wiederherstellen
                viewport.SetCameraLocation(cameraLocation, true);
                viewport.SetCameraDirection(cameraDirection, true);
                viewport.CameraUp = cameraUp;

                // Selektionszustand wiederherstellen
                if (wasSelected)
                {
                    instanceObject.Select(true);
                }
                
                _doc.Views.Redraw();
            }
        }

        /// <summary>
        /// Definiert die verschiedenen Viewport-Orientierungen
        /// </summary>
        private enum ViewportOrientation
        {
            Front,
            Left,
            Right,
            Top
        }
    }
} 
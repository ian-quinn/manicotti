#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class SketchDWG : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            View active_view = doc.ActiveView;

            // Pick Import Instance
            Reference r = uidoc.Selection.PickObject(ObjectType.Element, new Util.ElementsOfClassSelectionFilter<ImportInstance>());
            var import = doc.GetElement(r) as ImportInstance;

            List<GeometryObject> dwg_geos = Util.TeighaGeometry.ExtractElement(uidoc, import);
            //List<GeometryObject> dwg_geos = CADGeoUtil.ExtractElement(uidoc, import, "WALL", "Line");
            //Debug.Print("Number of geos is " + dwg_geos.Count().ToString());

            // Create ModelCurve for all GeometryElement in DWG
            // Directly by exposed RevitAPI methods
            if (dwg_geos.Count > 0)
            {
                using (var t = new Transaction(doc))
                {
                    t.Start("Extract Geos");

                    // Visualize model lines for debugging (needs SketchPlane)
                    Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                    SketchPlane sketch = SketchPlane.Create(doc, Geomplane);
                    
                    foreach (var obj in dwg_geos)
                    {
                        double tolerance = commandData.Application.Application.ShortCurveTolerance;
                        // The variable will be null if the implicit cast fails, thus...
                        // This will sort the curve(line) and polyline into two categories automatically
                        Curve crv = obj as Curve;
                        PolyLine poly = obj as PolyLine;
                        // Draw a ModelCurve in case the object is a Curve
                        if (null != crv)
                        {
                            ModelCurve modelline = doc.Create.NewModelCurve(crv, sketch) as ModelCurve;
                        }
                        // Draw shattered lines in case the object is a PolyLine
                        if (null != poly)
                        {
                            var vertices = poly.GetCoordinates();
                            CurveArray shatters = new CurveArray();
                            for (int i = 0; i < vertices.Count() - 1; i++)
                            {
                                if ((vertices[i + 1] - vertices[i]).GetLength() >= tolerance)
                                {
                                    shatters.Append(Line.CreateBound(vertices[i], vertices[i + 1]) as Curve);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            foreach (Curve shatter in shatters)
                            {
                                ModelCurve modelline = doc.Create.NewModelCurve(shatter, sketch) as ModelCurve;
                            }
                        }
                    }
                    t.Commit();
                }
            }
            
            // Convert texts info into TextNote by Teigha
            // Revit does not expose any API to parse text info
            string path = Util.TeighaText.GetCADPath(uidoc, import);
            List<Util.TeighaText.CADTextModel> texts = Util.TeighaText.GetCADText(path);

            Debug.Print("The path of linked DWG file is: " + path);
            Debug.Print("Texts.Count: " + texts.Count.ToString());

            if (texts.Count > 0)
            {
                using (var t = new Transaction(doc))
                {
                    t.Start("Extracting Text");

                    // Visualize the text notes just for reference
                    // Do not use NewModelText that only works for family document but not project
                    TextNoteType tnt = new FilteredElementCollector(doc)
                        .OfClass(typeof(TextNoteType)).First() as TextNoteType;
                    foreach (var textmodel in texts)
                    {
                        TextNote txNote = TextNote.Create(doc, active_view.Id, textmodel.Location, textmodel.Text, tnt.Id);
                    }

                    t.Commit();
                }
            }

            return Result.Succeeded;
        }
    }
}

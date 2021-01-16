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
    public class Distribute : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            View active_view = doc.ActiveView;

            // Pick Import Instance
            Reference r = uidoc.Selection.PickObject(ObjectType.Element, new JtElementsOfClassSelectionFilter<ImportInstance>());
            var import = doc.GetElement(r) as ImportInstance;

            // Data preparations

            // Prepare frames and levels
            // Cluster all geometry elements and texts into datatrees
            // Two procedures are intertwined
            List<GeometryObject> dwg_frames = CADGeoUtil.ExtractElement(uidoc, import, "FRAME", "PolyLine");
            List<GeometryObject> dwg_geos = CADGeoUtil.ExtractElement(uidoc, import);
            // Terminate if no geometry has been found
            if (dwg_geos == null)
                return Result.Failed;

            List<PolyLine> closedPolys = new List<PolyLine>();
            List<PolyLine> parentPolys = new List<PolyLine>();
            double tolerance = commandData.Application.Application.ShortCurveTolerance;
            Debug.Print("Number of geos is " + dwg_frames.Count().ToString());
            
            if (dwg_frames.Count > 0)
            {
                foreach (var obj in dwg_frames)
                {
                    PolyLine poly = obj as PolyLine;
                    // Draw shattered lines in case the object is a PolyLine
                    if (null != poly)
                    {
                        var vertices = poly.GetCoordinates();
                        if (vertices[0].IsAlmostEqualTo(vertices.Last()))
                        {
                            closedPolys.Add(poly);
                        }
                    }
                }

                for (int i = 0; i < closedPolys.Count(); i++)
                {
                    int judgement = 0;
                    int counter = 0;
                    for (int j = 0; j < closedPolys.Count(); j++)
                    {
                        if (i != j)
                        {
                            if (!RegionDetect.PolyInPoly(closedPolys[i], RegionDetect.PolyLineToCurveArray(closedPolys[j], tolerance)))
                            {
                                Debug.Print("Poly inside poly detected");
                                judgement += 1;
                            }
                            counter += 1;
                        }
                    }
                    if (judgement == counter)
                    {
                        parentPolys.Add(closedPolys[i]);
                    }
                }
            }
            else
            {
                Debug.Print("There is no returning of geometries");
            }
            Debug.Print("Got closedPolys: " + closedPolys.Count().ToString());
            Debug.Print("Got parentPolys: " + parentPolys.Count().ToString());

            string path = CADTextUtil.GetCADPath(uidoc, import);
            Debug.Print("The path of linked CAD file is: " + path);
            List<CADTextUtil.CADTextModel> texts = CADTextUtil.GetCADText(path);

            int level;
            int levelCounter = 0;
            double floorHeight = 13.1233596;  // (in feet) = 4.0 m
            Dictionary<int, PolyLine> frameDict= new Dictionary<int, PolyLine>();
            Dictionary<int, XYZ> transDict= new Dictionary<int, XYZ>();
            Dictionary<int, List<GeometryObject>> geoDict = new Dictionary<int, List<GeometryObject>>();
            Dictionary<int, List<CADTextUtil.CADTextModel>> textDict = new Dictionary<int, List<CADTextUtil.CADTextModel>>();

            if (texts.Count > 0)
            {
                foreach (var textmodel in texts)
                {
                    level = Util.GetLevel(textmodel.Text, "平面图");
                    Debug.Print("Got target label " + textmodel.Text);
                    if (level != -1)
                    {
                        foreach (PolyLine frame in parentPolys)
                        {
                            if (RegionDetect.PointInPoly(RegionDetect.PolyLineToCurveArray(frame, tolerance), textmodel.Location))
                            {
                                XYZ basePt = Algorithm.BubbleSort(frame.GetCoordinates().ToList())[0];
                                XYZ transVec = level * floorHeight * XYZ.BasisZ - basePt;
                                Debug.Print("Add level " + level.ToString() + " with transaction (" +
                                    transVec.X.ToString() + ", " + transVec.Y.ToString() + ", " + transVec.Z.ToString() + ")");
                                if (!frameDict.Values.ToList().Contains(frame))
                                {
                                    frameDict.Add(level, frame);
                                    transDict.Add(level, transVec);
                                    levelCounter += 1;
                                }
                            }
                        }
                    }
                }
                // Too complicated using 2 iterations... uplift needed
                for (int i = 1; i <= levelCounter; i++)
                {
                    textDict.Add(i, new List<CADTextUtil.CADTextModel>());
                    geoDict.Add(i, new List<GeometryObject>());
                    CurveArray tempPolyArray = RegionDetect.PolyLineToCurveArray(frameDict[i], tolerance);
                    foreach (var textmodel in texts)
                    {
                        if (RegionDetect.PointInPoly(tempPolyArray, textmodel.Location))
                        {
                            textDict[i].Add(textmodel);
                        }
                    }
                    foreach (GeometryObject go in dwg_geos)
                    {
                        XYZ centPt = XYZ.Zero;
                        if (go is Line)
                        {
                            //get the revit model coordinates.
                            Line go_line = go as Line;
                            centPt = (go_line.GetEndPoint(0) + go_line.GetEndPoint(1)).Divide(2);
                        }
                        else if (go is Arc)
                        {
                            Arc go_arc = go as Arc;
                            centPt = go_arc.Center;
                        }
                        if (RegionDetect.PointInPoly(tempPolyArray, centPt))
                        {
                            geoDict[i].Add(go);
                        }
                    }
                }
            }
            Debug.Print("All levels: " + levelCounter.ToString());
            Debug.Print("frameDict: " + frameDict.Count().ToString());
            Debug.Print("transDict: " + transDict.Count().ToString());
            Debug.Print("geoDict: " + geoDict.Count().ToString() + " " + geoDict[1].Count().ToString());
            Debug.Print("textDict: " + textDict.Count().ToString() + " " + textDict[1].Count().ToString());
            

            // Transactions to add stuffs to the document
            using (var t = new Transaction(doc))
            {
                t.Start("Allocate information to levels");
                
                // Prepare a family and configurations for TextNote
                TextNoteType tnt = new FilteredElementCollector(doc)
                    .OfClass(typeof(TextNoteType)).First() as TextNoteType;
                TextNoteOptions options = new TextNoteOptions();
                options.HorizontalAlignment = HorizontalTextAlignment.Center;
                options.TypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
                BuiltInParameter paraIndex = BuiltInParameter.TEXT_SIZE;
                Parameter textSize = tnt.get_Parameter(paraIndex);
                textSize.Set(0.3); // in feet

                // Prepare a family for ViewPlan creation
                // It may be a coincidence that the 1st ViewFamilyType is for the FloorPlan
                // Uplift needed here (doomed if it happends to be a CeilingPlan)
                ViewFamilyType floorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType)).First() as ViewFamilyType;

                for (int i = 1; i <= levelCounter; i++)
                {
                    XYZ centPt = RegionDetect.PolyCentPt(frameDict[i]);
                    TextNoteOptions opts = new TextNoteOptions(tnt.Id);
                    Transform alignment = Transform.CreateTranslation(transDict[i]);

                    // The note may only show in the current view
                    // no matter we still need it anyway
                    TextNote txNote = TextNote.Create(doc, active_view.Id, centPt, "F"+(transDict[i].Z/floorHeight).ToString(), options);
                    txNote.ChangeTypeId(tnt.Id);

                    // Draw model lines of frames as notation
                    Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero + transDict[i]);
                    SketchPlane sketch = SketchPlane.Create(doc, Geomplane);
                    CurveArray shatters = RegionDetect.PolyLineToCurveArray(frameDict[i], tolerance);
                    foreach (Curve shatter in shatters)
                    {
                        Curve alignedCrv = shatter.CreateTransformed(alignment);
                        ModelCurve modelline = doc.Create.NewModelCurve(alignedCrv, sketch) as ModelCurve;
                    }

                    // Create additional levels (ignore what's present)
                    Level floor = Level.Create(doc, transDict[i].Z);
                    ViewPlan floorView = ViewPlan.Create(doc, floorType.Id, floor.Id);
                    floorView.Name = "F-" + (transDict[i].Z / floorHeight).ToString();
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}

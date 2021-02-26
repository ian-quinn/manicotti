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
            Reference r = uidoc.Selection.PickObject(ObjectType.Element, new UtilElementsOfClassSelectionFilter<ImportInstance>());
            var import = doc.GetElement(r) as ImportInstance;

            // DATA PREPARATIONS

            // Prepare frames and levels
            // Cluster all geometry elements and texts into datatrees
            // Two procedures are intertwined
            List<GeometryObject> dwg_frames = UtilGetCADGeometry.ExtractElement(uidoc, import, "FRAME", "PolyLine");
            List<GeometryObject> dwg_geos = UtilGetCADGeometry.ExtractElement(uidoc, import);
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
                                //Debug.Print("Poly inside poly detected");
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
            string path = UtilGetCADText.GetCADPath(uidoc, import);
            Debug.Print("The path of linked CAD file is: " + path);
            List<UtilGetCADText.CADTextModel> texts = UtilGetCADText.GetCADText(path);

            int level;
            int levelCounter = 0;
            double floorHeight = Util.MmToFoot(Properties.Settings.Default.floorHeight);
            Dictionary<int, PolyLine> frameDict= new Dictionary<int, PolyLine>(); // cache drawing borders
            Dictionary<int, XYZ> transDict= new Dictionary<int, XYZ>();
            // cache transform vector from the left-bottom corner of the drawing border to the Origin
            Dictionary<int, List<GeometryObject>> geoDict = new Dictionary<int, List<GeometryObject>>();
            // cache geometries of each floorplan
            Dictionary<int, List<UtilGetCADText.CADTextModel>> textDict = new Dictionary<int, List<UtilGetCADText.CADTextModel>>();
            // cache text info of each floorplan

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
                                XYZ transVec = XYZ.Zero - basePt;
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
                    textDict.Add(i, new List<UtilGetCADText.CADTextModel>());
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
                        else if (go is PolyLine)
                        {
                            PolyLine go_poly = go as PolyLine;
                            centPt = go_poly.GetCoordinate(0);
                        }
                        // Assignment
                        if (RegionDetect.PointInPoly(tempPolyArray, centPt) && centPt != XYZ.Zero)
                        {
                            geoDict[i].Add(go);
                        }
                    }
                }
            }
            Debug.Print("All levels: " + levelCounter.ToString());
            Debug.Print("frameDict: " + frameDict.Count().ToString());
            Debug.Print("transDict: " + transDict.Count().ToString());
            for (int i = 1; i <= levelCounter; i++)
            {
                Debug.Print("geoDict-{0}: {1}", i, geoDict[i].Count().ToString());
            }
            Debug.Print("textDict: " + textDict.Count().ToString() + " " + textDict[1].Count().ToString());


            // MAIN TRANSACTIONS

            // Prepare a family for ViewPlan creation
            // It may be a coincidence that the 1st ViewFamilyType is for the FloorPlan
            // Uplift needed here (doomed if it happends to be a CeilingPlan)
            ViewFamilyType floorType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType)).First() as ViewFamilyType;

            // Prepare a family and configurations for TextNote (Put it inside transactions)
            /*
            TextNoteType tnt = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNoteType)).First() as TextNoteType;
            TextNoteOptions options = new TextNoteOptions();
            options.HorizontalAlignment = HorizontalTextAlignment.Center;
            options.TypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
            BuiltInParameter paraIndex = BuiltInParameter.TEXT_SIZE;
            Parameter textSize = tnt.get_Parameter(paraIndex);
            textSize.Set(0.3); // in feet

            XYZ centPt = RegionDetect.PolyCentPt(frameDict[i]);
            TextNoteOptions opts = new TextNoteOptions(tnt.Id);
            
            // The note may only show in the current view
            // no matter we still need it anyway
            TextNote txNote = TextNote.Create(doc, active_view.Id, centPt, floorView.Name, options);
            txNote.ChangeTypeId(tnt.Id);

            // Draw model lines of frames as notation
            Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero + transDict[i] + XYZ.BasisZ * (i - 1) * floorHeight);
            SketchPlane sketch = SketchPlane.Create(doc, Geomplane);
            CurveArray shatters = RegionDetect.PolyLineToCurveArray(frameDict[i], tolerance);
            Transform alignModelLine = Transform.CreateTranslation(transDict[i] + XYZ.BasisZ * (i - 1) * floorHeight);
            foreach (Curve shatter in shatters)
            {
                Curve alignedCrv = shatter.CreateTransformed(alignModelLine);
                ModelCurve modelline = doc.Create.NewModelCurve(alignedCrv, sketch) as ModelCurve;
            }
            */
            

            // Main process
            for (int i = 1; i <= levelCounter; i++)
            {
                Transform alignment = Transform.CreateTranslation(transDict[i]);

                // Align the labels to the origin
                foreach (UtilGetCADText.CADTextModel label in textDict[i])
                {
                    label.Location = label.Location + transDict[i];
                }

                // Sort out lines
                List<Line> doubleLines = new List<Line>();
                List<Curve> columnLines = new List<Curve>();
                List<Curve> doorCrvs = new List<Curve>();
                List<Curve> windowCrvs = new List<Curve>();
                foreach (GeometryObject go in geoDict[i])
                {
                    var gStyle = doc.GetElement(go.GraphicsStyleId) as GraphicsStyle;
                    if (gStyle.GraphicsStyleCategory.Name == "WALL")
                    {
                        if (go.GetType().Name == "Line")
                        {
                            Curve wallLine = go as Curve;
                            doubleLines.Add(wallLine.CreateTransformed(alignment) as Line);
                        }
                        if (go.GetType().Name == "PolyLine")
                        {
                            CurveArray wallPolyLine_shattered = RegionDetect.PolyLineToCurveArray(go as PolyLine, tolerance);
                            foreach (Curve crv in wallPolyLine_shattered)
                            {
                                doubleLines.Add(crv.CreateTransformed(alignment) as Line);
                            }
                        }
                    }
                    if (gStyle.GraphicsStyleCategory.Name == "COLUMN")
                    {
                        if (go.GetType().Name == "Line")
                        {
                            Curve columnLine = go as Curve;
                            columnLines.Add(columnLine.CreateTransformed(alignment));
                        }
                        if (go.GetType().Name == "PolyLine")
                        {
                            CurveArray columnPolyLine_shattered = RegionDetect.PolyLineToCurveArray(go as PolyLine, tolerance);
                            foreach (Curve crv in columnPolyLine_shattered)
                            {
                                columnLines.Add(crv.CreateTransformed(alignment));
                            }
                        }
                    }
                    if (gStyle.GraphicsStyleCategory.Name == "DOOR")
                    {
                        Curve doorCrv = go as Curve;
                        PolyLine poly = go as PolyLine;
                        if (null != doorCrv)
                        {
                            doorCrvs.Add(doorCrv.CreateTransformed(alignment));
                        }
                        if (null != poly)
                        {
                            CurveArray columnPolyLine_shattered = RegionDetect.PolyLineToCurveArray(poly, tolerance);
                            foreach (Curve crv in columnPolyLine_shattered)
                            {
                                doorCrvs.Add(crv.CreateTransformed(alignment) as Line);
                            }
                        }
                    }
                    if (gStyle.GraphicsStyleCategory.Name == "WINDOW")
                    {
                        Curve windowCrv = go as Curve;
                        PolyLine poly = go as PolyLine;
                        if (null != windowCrv)
                        {
                            windowCrvs.Add(windowCrv.CreateTransformed(alignment));
                        }
                        if (null != poly)
                        {
                            CurveArray columnPolyLine_shattered = RegionDetect.PolyLineToCurveArray(poly, tolerance);
                            foreach (Curve crv in columnPolyLine_shattered)
                            {
                                windowCrvs.Add(crv.CreateTransformed(alignment) as Line);
                            }
                        }
                    }
                }


                // Create additional levels (ignore what's present)
                // Consider to use sub-transaction here
                using (var t_level = new Transaction(doc))
                {
                    t_level.Start("Create levels");
                    Level floor = Level.Create(doc, (i - 1) * floorHeight);
                    ViewPlan floorView = ViewPlan.Create(doc, floorType.Id, floor.Id);
                    floorView.Name = "F-" + i.ToString();
                    t_level.Commit();
                }

                // Grab the current building level
                FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.INVALID)
                    .OfClass(typeof(Level));
                Level currentLevel = colLevels.LastOrDefault() as Level;

                // Sub-transactions are packed within these functions.
                CreateWall.Execute(uiapp, doubleLines, currentLevel);
                CreateColumn.Execute(uiapp, columnLines, currentLevel);
                CreateOpening.Execute(uiapp, doorCrvs, windowCrvs, doubleLines, textDict[i], currentLevel);
                
            }

            return Result.Succeeded;
        }
    }
}
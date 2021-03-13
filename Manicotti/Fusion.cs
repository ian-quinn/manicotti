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
    public class Fusion : IExternalCommand
    {
        /// <summary>
        /// Recreate floorplan mesh based on Basic & Curtain Wall. WIP
        /// </summary>
        public Tuple<List<XYZ>, List<Curve>> Orphans(List<Curve> crvs)
        {
            List<Curve> Crvs = new List<Curve>();
            for (int CStart = 0; CStart <= crvs.Count - 1; CStart++)
            {
                List<double> breakParams = new List<double>();
                for (int CCut = 0; CCut <= crvs.Count - 1; CCut++)
                {
                    if (CStart != CCut)
                    {
                        SetComparisonResult result = crvs[CStart].Intersect(RegionDetect.ExtendCrv(crvs[CCut], 0.01),
                            out IntersectionResultArray results);
                        if (result != SetComparisonResult.Disjoint)
                        {
                            double breakParam = results.get_Item(0).UVPoint.U;
                            breakParams.Add(breakParam);
                            //Debug.Print("Projection parameter is: " + breakParam.ToString());
                        }
                    }
                }
                Crvs.AddRange(RegionDetect.SplitCrv(crvs[CStart], breakParams));
            }

            List<XYZ> ptPool = new List<XYZ>();
            List<int> ptIndex = new List<int>();
            List<XYZ> ptOrphan = new List<XYZ>();
            List<Curve> crvOrphan = new List<Curve>();
            for (int Cid = 0; Cid <= Crvs.Count - 1; Cid++)
            {
                ptPool.Add(Crvs[Cid].GetEndPoint(0));
                ptPool.Add(Crvs[Cid].GetEndPoint(1));
            }
            for (int Pid = 0; Pid <= ptPool.Count - 1; Pid++)
            {
                foreach (XYZ pt in ptPool)
                {
                    if (ptPool[Pid].IsAlmostEqualTo(pt))
                    {
                        continue;
                    }
                    ptIndex.Add(Pid);
                    ptOrphan.Add(ptPool[Pid]);
                    crvOrphan.Add(Crvs[Pid / 2]);
                }
            }

            return new Tuple<List<XYZ>, List<Curve>>(ptOrphan, crvOrphan);
        }


        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;


            // Filter all basic walls
            FilteredElementCollector BasicWallCollector = new FilteredElementCollector(doc).OfClass(typeof(Wall));
            IEnumerable<Wall> BasicWalls = BasicWallCollector.Cast<Wall>().Where(w => w.WallType.Kind == WallKind.Basic);
            List<Wall> walls = new List<Wall>();
            foreach (Element wall in BasicWalls)
            {
                walls.Add(wall as Wall);
            }


            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;


            // Grab the building floortype
            FloorType floorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .First<Element>(e => e.Name.Equals("Generic 150mm")) as FloorType;


            // Modify document within a transaction
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate floors");

                List<Curve> strayLines = new List<Curve>();
                foreach (Wall wall in walls)
                {
                    LocationCurve centerLine = wall.Location as LocationCurve;
                    strayLines.Add(centerLine.Curve);
                }
                Debug.Print("The number of wall axes: " + strayLines.Count().ToString());


                // Visualize model lines for debugging
                Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(doc, Geomplane);
                foreach (Line axis in strayLines)
                {
                    ModelLine line = doc.Create.NewModelCurve(axis, sketch) as ModelLine;
                    //Debug.Print("Model line generated");
                }

                List<CurveArray> curveGroup = RegionDetect.RegionCluster(strayLines);

                /*
                // Check containment. permanent abandoned
                var (ptOrphans, crvOrphans) = Orphans(strayLines);
                List<Curve> drawinglist = new List<Curve>();
                foreach (XYZ pt in ptOrphans)
                {
                    foreach (CurveArray poly in curveGroup)
                    {
                        if (RegionDetect.PointInPoly(poly, pt))
                        {
                            drawinglist.Add(crvOrphans[ptOrphans.IndexOf(pt)]);
                        }
                    }
                }
                foreach (Line axis in drawinglist)
                {
                    ModelLine line = doc.Create.NewModelCurve(axis, sketch) as ModelLine;
                    //Debug.Print("Model line generated");
                }
                */

                var mesh = RegionDetect.FlattenLines(curveGroup).Item1;
                var perimeter = RegionDetect.FlattenLines(curveGroup).Item2;

                Floor newFloor = doc.Create.NewFloor(RegionDetect.AlignCrv(perimeter), floorType, firstLevel, false, XYZ.BasisZ);
                newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(0);

                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}

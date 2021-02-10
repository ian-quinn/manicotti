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
    public class TestSubsrf : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            // Access current selection
            Selection sel = uidoc.Selection;

            // Extraction of CurveElements by LineStyle COLUMN
            CurveElementFilter filter = new CurveElementFilter(CurveElementType.ModelCurve);
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> founds = collector.WherePasses(filter).ToElements();
            List<CurveElement> importCurves = new List<CurveElement>();

            foreach (CurveElement ce in founds)
            {
                importCurves.Add(ce);
            }

            var doorCurves = importCurves.Where(x => x.LineStyle.Name == "DOOR").ToList();
            List<Curve> doorLines = new List<Curve>();
            foreach (CurveElement ce in doorCurves)
            {
                doorLines.Add(ce.GeometryCurve as Curve);
            }


            var wallCurves = importCurves.Where(x => x.LineStyle.Name == "WALL").ToList();
            List<Curve> wallLines = new List<Curve>();
            foreach (CurveElement ce in wallCurves)
            {
                wallLines.Add(ce.GeometryCurve as Curve);
            }


            // type check
            // basically it should be an arc or a line
            foreach (Curve crv in doorLines)
            {
                Debug.Print("Type checking... " + crv.GetType().ToString());
            }

            var doorClusters = Algorithm.ClusterByIntersection(doorLines);
            List<Curve> boundingLines = new List<Curve> { };
            foreach (List<Curve> cluster in doorClusters)
            {
                var pts = Algorithm.FlattenToPts(cluster);
                if (null != Algorithm.CreateBoundingBox2D(pts))
                {
                    boundingLines.AddRange(Algorithm.CreateBoundingBox2D(pts));
                }
            }
            var boundingClusters = Algorithm.ClusterByIntersection(boundingLines);
            List<List<Curve>> doorBlocks = new List<List<Curve>> { };
            foreach (List<Curve> cluster in boundingClusters)
            {
                var pts = Algorithm.FlattenToPts(cluster);
                if (null != Algorithm.CreateBoundingBox2D(pts))
                {
                    doorBlocks.Add(Algorithm.CreateBoundingBox2D(pts));
                }
            }
            Debug.Print("{0} clustered door blocks in total", doorBlocks.Count);


            List<Curve> doorAxes = new List<Curve> { };
            foreach (List<Curve> doorBlock in doorBlocks)
            {
                List<Curve> doorFrame = new List<Curve> { };
                for (int i = 0; i < doorBlock.Count; i++)
                {
                    int sectCount = 0;
                    foreach (Line line in wallLines)
                    {
                        Curve testCrv = doorBlock[i].Clone();
                        SetComparisonResult result = RegionDetect.ExtendCrv(testCrv, 0.01).Intersect(line,
                                                   out IntersectionResultArray results);
                        if (result == SetComparisonResult.Overlap)
                        {
                            sectCount += 1;
                            //doorFrame.Add(line);
                        }
                    }
                    if (sectCount == 2) { doorAxes.Add(doorBlock[i]); }
                }
                //Debug.Print("Curves adjoning the box: " + doorFrame.Count.ToString());
                /*
                if (doorFrame.Count > 0)
                {
                    XYZ ptStart = (doorFrame[0].GetEndPoint(0).Add(doorFrame[0].GetEndPoint(1))).Divide(2);
                    XYZ ptEnd = (doorFrame[1].GetEndPoint(0).Add(doorFrame[1].GetEndPoint(1))).Divide(2);
                    Curve doorAxis = Line.CreateBound(ptStart, ptEnd) as Curve;
                }
                */
                //doorAxes.Add(doorFrame.Last());
            }
            Debug.Print("We got {0} door axes. ", doorAxes.Count);


            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate boundingbos");

                Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(doc, Geomplane);
                
                foreach (List<Curve> doorBlock in doorBlocks)
                {
                    Debug.Print("Creating new bounding box");
                    foreach (Curve edge in doorBlock)
                    {
                        ModelCurve modelline = doc.Create.NewModelCurve(edge, sketch) as ModelCurve;
                    }
                }

                foreach (Curve doorAxis in doorAxes)
                {
                    DetailLine axis = doc.Create.NewDetailCurve(view, doorAxis) as DetailLine;
                    GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor= new Color(202, 51, 82);
                    gs.GraphicsStyleCategory.SetLineWeight(6, gs.GraphicsStyleType);
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}

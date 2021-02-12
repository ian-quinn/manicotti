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
        public static FamilySymbol CreateDoor(UIApplication uiapp, String familyName, double width, double height)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            Family f = Util.GetFirstElementOfTypeNamed(doc, typeof(Family), familyName) as Family;
            if (null == f)
            {
                // add default path and error handling here
                if (!doc.LoadFamily("C:\\ProgramData\\Autodesk\\RVT 2020\\Libraries\\US Metric\\Doors\\M_Door-Single-Panel.rfa", out f))
                {
                    Debug.Print("Unable to load M_Door-Single-Panel.rfa");
                }
            }

            if (null != f)
            {
                Debug.Print("Family name={0}", f.Name);

                // Pick any symbol for duplication (for iteration convenient choose the last):
                FamilySymbol s = null;
                foreach (ElementId id in f.GetFamilySymbolIds())
                {
                    s = doc.GetElement(id) as FamilySymbol;
                    if (s.Name == width.ToString() + " x " + height.ToString() + "mm")
                    {
                        return s;
                    }
                }

                Debug.Assert(null != s, "expected at least one symbol to be defined in family");

                // Duplicate the existing symbol:
                s = s.Duplicate(width.ToString() + " x " + height.ToString() + "mm") as FamilySymbol;

                // Analyse the symbol parameters:
                foreach (Parameter param in s.Parameters)
                {
                    Debug.Print("Parameter name={0}, value={1}", param.Definition.Name, param.AsValueString());
                }

                // Define new dimensions for our new type;
                // the specified parameter name is case sensitive:
                s.LookupParameter("Width").Set(Util.MmToFoot(width));
                s.LookupParameter("Height").Set(Util.MmToFoot(height));

                return s;
            }
            else
            {
                return null;
            }
        }



        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            // Access current selection
            Selection sel = uidoc.Selection;

            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;

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
            List<Line> wallLines = new List<Line>();
            foreach (CurveElement ce in wallCurves)
            {
                wallLines.Add(ce.GeometryCurve as Line);
            }

            var windowCurves = importCurves.Where(x => x.LineStyle.Name == "WINDOW").ToList();
            List<Curve> windowLines = new List<Curve>();
            foreach (CurveElement ce in windowCurves)
            {
                windowLines.Add(ce.GeometryCurve as Curve);
            }


            // type check
            // basically it should be an arc or a line
            foreach (Curve crv in doorLines)
            {
                Debug.Print("Type checking... " + crv.GetType().ToString());
            }

            var doorClusters = Algorithm.ClusterByIntersection(doorLines);
            // Sometimes ClusterByIntersection will not return ideal result
            // because the intersected lines are not detected by the program for no reason
            // sometimes it goes well
            // iterate the bounding box generation 2 times may gurantee this but lacking efficiency
            /*
            List<Curve> boundingLines = new List<Curve> { };
            foreach (List<Curve> cluster in doorClusters)
            {
                if (null != Algorithm.CreateBoundingBox2D(cluster))
                {
                    boundingLines.AddRange(Algorithm.CreateBoundingBox2D(cluster));
                }
            }
            var boundingClusters = Algorithm.ClusterByIntersection(boundingLines);
            */

            List<List<Curve>> doorBlocks = new List<List<Curve>> { };
            foreach (List<Curve> cluster in doorClusters)
            {
                if (null != Algorithm.CreateBoundingBox2D(cluster))
                {
                    doorBlocks.Add(Algorithm.CreateBoundingBox2D(cluster));
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
                
            }
            Debug.Print("We got {0} door axes. ", doorAxes.Count);


            /////////////////////////////////////
            // Collect window blocks
            var windowClusters = Algorithm.ClusterByIntersection(windowLines);

            List<List<Curve>> windowBlocks = new List<List<Curve>> { };
            foreach (List<Curve> cluster in windowClusters)
            {
                if (null != Algorithm.CreateBoundingBox2D(cluster))
                {
                    windowBlocks.Add(Algorithm.CreateBoundingBox2D(cluster));
                }
            }
            Debug.Print("{0} clustered window blocks in total", windowBlocks.Count);
            
            List<Curve> windowAxes = new List<Curve> { };
            foreach (List<Curve> windowBlock in windowBlocks)
            {
                Line axis1 = Line.CreateBound((windowBlock[0].GetEndPoint(0) + windowBlock[0].GetEndPoint(1)).Divide(2),
                    (windowBlock[2].GetEndPoint(0) + windowBlock[2].GetEndPoint(1)).Divide(2));
                Line axis2 = Line.CreateBound((windowBlock[1].GetEndPoint(0) + windowBlock[1].GetEndPoint(1)).Divide(2),
                    (windowBlock[3].GetEndPoint(0) + windowBlock[3].GetEndPoint(1)).Divide(2));
                if (axis1.Length > axis2.Length)
                {
                    windowAxes.Add(axis1);
                }
                else
                {
                    windowAxes.Add(axis2);
                }
            }
            Debug.Print("We got {0} window axes. ", windowAxes.Count);



            // Plot bounding box and axis
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate boundingbox");

                Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(doc, Geomplane);
                
                // Create door & window blocks
                foreach (List<Curve> doorBlock in doorBlocks)
                {
                    Debug.Print("Creating new bounding box");
                    foreach (Curve edge in doorBlock)
                    {
                        ModelCurve modelline = doc.Create.NewModelCurve(edge, sketch) as ModelCurve;
                    }
                }
                foreach (List<Curve> windowBlock in windowBlocks)
                {
                    Debug.Print("Creating new bounding box");
                    foreach (Curve edge in windowBlock)
                    {
                        ModelCurve modelline = doc.Create.NewModelCurve(edge, sketch) as ModelCurve;
                    }
                }

                // Create door & window axes
                foreach (Curve doorAxis in doorAxes)
                {
                    DetailLine axis = doc.Create.NewDetailCurve(view, doorAxis) as DetailLine;
                    GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor= new Color(202, 51, 82);
                    gs.GraphicsStyleCategory.SetLineWeight(7, gs.GraphicsStyleType);

                    Wall wl = Wall.Create(doc, doorAxis, firstLevel.Id, true);

                    double width = Math.Round(Util.FootToMm(doorAxis.Length), 0);
                    double height = 2000;

                    FamilySymbol cs = CreateDoor(uiapp, "M_Single-Flush", width, height);
                    XYZ doorInsertPt = (doorAxis.GetEndPoint(0) + doorAxis.GetEndPoint(1)).Divide(2);
                    // z pointing down to apply a clockwise rotation
                    FamilyInstance fi = doc.Create.NewFamilyInstance(doorInsertPt, cs, wl, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                }

                foreach (Curve windowAxis in windowAxes)
                {
                    DetailLine axis = doc.Create.NewDetailCurve(view, windowAxis) as DetailLine;
                    GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor = new Color(202, 51, 82);
                    gs.GraphicsStyleCategory.SetLineWeight(7, gs.GraphicsStyleType);
                    Wall.Create(doc, windowAxis, firstLevel.Id, true);
                }
                
                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}

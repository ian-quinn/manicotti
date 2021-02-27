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
    public class TestIntersect : IExternalCommand
    {
        
        // Main execution
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;
            Selection sel = uidoc.Selection;


            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;


            // LineStyle filter
            CurveElementFilter filter = new CurveElementFilter(CurveElementType.ModelCurve);
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> founds = collector.WherePasses(filter).ToElements();
            List<CurveElement> importCurves = new List<CurveElement>();

            foreach (CurveElement ce in founds)
            {
                importCurves.Add(ce);
            }
            var wallCurves = importCurves.Where(x => x.LineStyle.Name == "WALL").ToList();
            List<Curve> wallLines = new List<Curve>();  // Algorithm only support walls of line type
            foreach (CurveElement ce in wallCurves)
            {
                wallLines.Add(ce.GeometryCurve as Curve);
            }

            //double parasStart = wallLines[0].GetEndParameter(0);
            //double parasEnd = wallLines[0].GetEndParameter(1);
            //wallLines[0].MakeUnbound();

            //double _parasStart = wallLines[1].GetEndParameter(0);
            //double _parasEnd = wallLines[1].GetEndParameter(1);
            //wallLines[1].MakeUnbound();

            SetComparisonResult result = wallLines[0].Intersect(wallLines[1], out IntersectionResultArray results);
            Debug.Assert(result != SetComparisonResult.Overlap, "Overlap");
            Debug.Assert(result != SetComparisonResult.BothEmpty, "BothEmpty");
            Debug.Assert(result != SetComparisonResult.Disjoint, "Disjoint");
            Debug.Assert(result != SetComparisonResult.Equal, "Equal");
            Debug.Assert(result != SetComparisonResult.LeftEmpty, "LeftEmpty");
            Debug.Assert(result != SetComparisonResult.RightEmpty, "RightEmpty");
            Debug.Assert(result != SetComparisonResult.Subset, "Subset");
            Debug.Assert(result != SetComparisonResult.Superset, "Superset");

            

            double radius = Util.MmToFoot(50);
            XYZ ptStart = wallLines[0].GetEndPoint(0);
            XYZ ptEnd = wallLines[0].GetEndPoint(1);
            XYZ xAxis = new XYZ(1, 0, 0);   // The x axis to define the arc plane. Must be normalized
            XYZ yAxis = new XYZ(0, 1, 0);   // The y axis to define the arc plane. Must be normalized
            Curve knob1 = Arc.Create(ptStart, radius, 0, 2 * Math.PI, xAxis, yAxis);
            Curve knob2 = Arc.Create(ptEnd, radius, 0, 2 * Math.PI, xAxis, yAxis);
            SetComparisonResult result1 = knob1.Intersect(wallLines[1], out IntersectionResultArray results1);
            SetComparisonResult result2 = knob2.Intersect(wallLines[1], out IntersectionResultArray results2);
            // if (result1 == SetComparisonResult.Disjoint && result2 == SetComparisonResult.Disjoint)
            if ((result1 == SetComparisonResult.Overlap || result1 == SetComparisonResult.Subset ||
                    result1 == SetComparisonResult.Superset || result1 == SetComparisonResult.Equal) ||
                    (result2 == SetComparisonResult.Overlap || result2 == SetComparisonResult.Subset ||
                    result2 == SetComparisonResult.Superset || result2 == SetComparisonResult.Equal))
            { Debug.Print("INTERSECTED!"); }
            else
            { Debug.Print("DISJOINT!"); }


            XYZ ptStart1 = wallLines[0].GetEndPoint(0);
            XYZ ptEnd1 = wallLines[0].GetEndPoint(1);
            XYZ ptStart2 = wallLines[1].GetEndPoint(0);
            XYZ ptEnd2 = wallLines[1].GetEndPoint(1);
            Line baseline = wallLines[1].Clone() as Line;
            baseline.MakeUnbound();
            XYZ _ptStart = baseline.Project(ptStart1).XYZPoint;
            XYZ _ptEnd = baseline.Project(ptEnd1).XYZPoint;
            Debug.Print("_start: " + Util.PrintXYZ(_ptStart));
            Debug.Print("_end: " + Util.PrintXYZ(_ptEnd));
            Line checkline = Line.CreateBound(_ptStart, _ptEnd);
            SetComparisonResult projection = checkline.Intersect(wallLines[1] as Line, out IntersectionResultArray projections);
            Debug.Print("Shadowing?" + projection.ToString());
            if (projection == SetComparisonResult.Equal)
            {
                Debug.Print("Shadowing");
            }
            {
                Debug.Print("Departed");
            }
            Curve proj1 = Arc.Create(_ptStart, radius, 0, 2 * Math.PI, xAxis, yAxis);
            Curve proj2 = Arc.Create(_ptEnd, radius, 0, 2 * Math.PI, xAxis, yAxis);


            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate sub-surface and its mark");

                Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(doc, Geomplane);

                ModelCurve modelline1 = doc.Create.NewModelCurve(proj1, sketch) as ModelCurve;
                ModelCurve modelline2 = doc.Create.NewModelCurve(proj2, sketch) as ModelCurve;

                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}

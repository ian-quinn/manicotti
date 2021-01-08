using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class FloorRegen : IExternalCommand
    {
        /// <summary>
        /// Extend a curve by 10% (centroid based)
        /// </summary>
        /// <param name="crv">A Curve</param>
        /// <returns>An Extended Curve</returns>
        public Curve ExtendCrv(Curve crv)
        {
            double pstart = crv.GetEndParameter(0);
            double pend = crv.GetEndParameter(1);
            double pdelta = 0.05 * (pend - pstart);

            crv.MakeUnbound();
            crv.MakeBound(pstart - pdelta, pend + pdelta);
            return crv;
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
                    strayLines.Add(ExtendCrv(centerLine.Curve));
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
                var (mesh, perimeter) = RegionDetect.FlattenLines(curveGroup);

                Floor newFloor = doc.Create.NewFloor(RegionDetect.AlignCrv(perimeter), floorType, firstLevel, false, XYZ.BasisZ);
                newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(0);

                tx.Commit();
            }
            
            return Result.Succeeded;
        }
    }
}

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
    public class TestWall : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Access current selection
            Selection sel = uidoc.Selection;

            // Extraction of CurveElements by LineStyle WALL
            CurveElementFilter filter = new CurveElementFilter(CurveElementType.ModelCurve);
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> founds = collector.WherePasses(filter).ToElements();
            List<CurveElement> importCurves = new List<CurveElement>();

            foreach (CurveElement ce in founds)
            {
                importCurves.Add(ce);
            }
            var doubleCurves = importCurves.Where(x => x.LineStyle.Name == "WALL").ToList();
            List<Line> doubleLines = new List<Line>();
            foreach (CurveElement ce in doubleCurves)
            {
                doubleLines.Add(ce.GeometryCurve as Line);
            }


            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate Walls");
                CreateWall.Execute(uiapp, doubleLines, firstLevel);
                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}

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
    public class TestOpening : IExternalCommand
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

            double tolerance = commandData.Application.Application.ShortCurveTolerance;

            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;

            // Pick Import Instance
            Reference r = uidoc.Selection.PickObject(ObjectType.Element, new UtilElementsOfClassSelectionFilter<ImportInstance>());
            var import = doc.GetElement(r) as ImportInstance;

            List<Curve> doorCrvs = UtilGetCADGeometry.ShatterCADGeometry(uidoc, import, Properties.Settings.Default.doorLayer, tolerance);
            List<Curve> windowCrvs = UtilGetCADGeometry.ShatterCADGeometry(uidoc, import, Properties.Settings.Default.windowLayer, tolerance);
            List<Curve> wallCrvs = UtilGetCADGeometry.ShatterCADGeometry(uidoc, import, Properties.Settings.Default.wallLayer, tolerance);
            

            // Convert texts info into TextNote by Teigha
            // Revit does not expose any API to parse text info
            string path = UtilGetCADText.GetCADPath(uidoc, import);
            List<UtilGetCADText.CADTextModel> labels = UtilGetCADText.GetCADText(path);
            /*
            List<UtilGetCADText.CADTextModel> labels = new List<UtilGetCADText.CADTextModel>();
            foreach (UtilGetCADText.CADTextModel text in UtilGetCADText.GetCADText(path))
            {
                if (UtilGetCADText.IsLabel(text.Text, out string label))
                {
                    labels.Add(text);
                }
            }
            */

            Debug.Print("The path of linked DWG file is: " + path);
            Debug.Print("Lables in total: " + labels.Count.ToString());
            foreach (UtilGetCADText.CADTextModel label in labels)
            {
                Debug.Print(label.Text);
            }


            /*
            // LineStyle filter
            CurveElementFilter filter = new CurveElementFilter(CurveElementType.ModelCurve);
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> founds = collector.WherePasses(filter).ToElements();
            List<CurveElement> importCurves = new List<CurveElement>();

            foreach (CurveElement ce in founds)
            {
                importCurves.Add(ce);
            }
            
            var doorCurves = importCurves.Where(x => x.LineStyle.Name == "DOOR").ToList();
            List<Curve> doorCrvs = new List<Curve>();  // The door block has one arc at least
            foreach (CurveElement ce in doorCurves)
            {
                doorCrvs.Add(ce.GeometryCurve as Curve);
            }
            
            var wallCurves = importCurves.Where(x => x.LineStyle.Name == "WALL").ToList();
            List<Line> wallLines = new List<Line>();  // Algorithm only support walls of line type
            foreach (CurveElement ce in wallCurves)
            {
                wallLines.Add(ce.GeometryCurve as Line);
            }
            var windowCurves = importCurves.Where(x => x.LineStyle.Name == "WINDOW").ToList();
            List<Curve> windowCrvs = new List<Curve>();
            foreach (CurveElement ce in windowCurves)
            {
                windowCrvs.Add(ce.GeometryCurve as Curve);
            }
            */
            

            CreateOpening.Execute(uiapp, doorCrvs, windowCrvs, Util.CrvsToLines(wallCrvs), labels, firstLevel);

            return Result.Succeeded;
        }
    }
}

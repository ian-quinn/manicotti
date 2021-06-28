#region Namespaces
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class CmdCreateOpening : IExternalCommand
    {

        // Main execution
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(Misc.LoadFromSameFolder);

            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;
            Selection sel = uidoc.Selection;

            double tolerance = commandData.Application.Application.ShortCurveTolerance;

            
            // Pick Import Instance
            ImportInstance import = null;
            try
            {
                Reference r = uidoc.Selection.PickObject(ObjectType.Element, new Util.ElementsOfClassSelectionFilter<ImportInstance>());
                import = doc.GetElement(r) as ImportInstance;
            }
            catch
            {
                return Result.Cancelled;
            }
            if (import == null)
            {
                System.Windows.MessageBox.Show("CAD not found", "Tips");
                return Result.Cancelled;
            }


            // Fetch baselines
            List<Curve> doorCrvs, windowCrvs, wallCrvs;
            try
            {
                doorCrvs = Util.TeighaGeometry.ShatterCADGeometry(uidoc, import, Properties.Settings.Default.layerDoor, tolerance);
                windowCrvs = Util.TeighaGeometry.ShatterCADGeometry(uidoc, import, Properties.Settings.Default.layerWindow, tolerance);
                wallCrvs = Util.TeighaGeometry.ShatterCADGeometry(uidoc, import, Properties.Settings.Default.layerWall, tolerance);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message, "Tips");
                return Result.Cancelled;
            }
            if (doorCrvs == null || windowCrvs == null || wallCrvs == null || doorCrvs.Count * windowCrvs.Count * wallCrvs.Count == 0)
            {
                System.Windows.MessageBox.Show("Baselines not found", "Tips");
                return Result.Cancelled;
            }


            // Grab the current building level
            FilteredElementCollector docLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            ICollection<Element> levels = docLevels.OfClass(typeof(Level)).ToElements();
            Level defaultLevel = null;
            foreach (Level level in levels)
            {
                if (level.Id == import.LevelId)
                {
                    defaultLevel = level;
                }
            }
            if (defaultLevel == null)
            {
                System.Windows.MessageBox.Show("Please make sure there's a base level in current view", "Tips");
                return Result.Cancelled;
            }


            // Convert texts info into TextNote by Teigha
            // Revit does not expose any API to parse text info
            string path = Util.TeighaText.GetCADPath(uidoc, import);
            List<Util.TeighaText.CADTextModel> labels = Util.TeighaText.GetCADText(path);
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
            foreach (Util.TeighaText.CADTextModel label in labels)
            {
                Debug.Print(label.Text);
            }


            // Check if the families are ready
            if (!File.Exists(Properties.Settings.Default.url_door) && File.Exists(Properties.Settings.Default.url_window))
            {
                System.Windows.MessageBox.Show("Please check the family path is solid", "Tips");
                return Result.Cancelled;
            }
            Family fDoor, fWindow = null;
            using (Transaction tx = new Transaction(doc, "Load necessary families"))
            {
                tx.Start();
                if (!doc.LoadFamily(Properties.Settings.Default.url_door, out fDoor) ||
                !doc.LoadFamily(Properties.Settings.Default.url_window, out fWindow))
                {
                    System.Windows.MessageBox.Show("Loading family failed", "Tips");
                    return Result.Cancelled;
                }
                tx.Commit();
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

            TransactionGroup tg = new TransactionGroup(doc, "Create openings");
            try
            {
                tg.Start();
                CreateOpening.Execute(uiapp, doorCrvs, windowCrvs, wallCrvs, labels, 
                    fDoor.Name, fWindow.Name, defaultLevel, false);
                tg.Assimilate();
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
                tg.RollBack();
                return Result.Cancelled;
            }

            return Result.Succeeded;
        }
    }
}

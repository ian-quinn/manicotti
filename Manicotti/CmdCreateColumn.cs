#region Namespaces
using System;
using System.IO;
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
    public class CmdCreateColumn : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

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
            List<Curve> columnCrvs = new List<Curve>();
            try
            {
                columnCrvs = Util.TeighaGeometry.ShatterCADGeometry(uidoc, import, Properties.Settings.Default.layerColumn, tolerance);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message, "Tips");
                return Result.Cancelled;
            }
            if (columnCrvs == null || columnCrvs.Count == 0)
            {
                System.Windows.MessageBox.Show("Baseline not found", "Tips");
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


            // Check if the families are ready
            if (!File.Exists(Properties.Settings.Default.url_column))
            {
                System.Windows.MessageBox.Show("Please check the family path is solid", "Tips");
                return Result.Cancelled;
            }
            Family fColumn = null;
            using (Transaction tx = new Transaction(doc, "Load necessary families"))
            {
                tx.Start();
                if (!doc.LoadFamily(Properties.Settings.Default.url_column, out fColumn))
                {
                    System.Windows.MessageBox.Show("Loading family failed", "Tips");
                    return Result.Cancelled;
                }
                tx.Commit();
            }
            


            // Start batching
            TransactionGroup tg = new TransactionGroup(doc, "Create columns");
            try
            {
                tg.Start();
                CreateColumn.Execute(uiapp, columnCrvs, fColumn.Name, defaultLevel, false);
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

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
    public class ExtrudeColumn : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

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

            var columnCurves = importCurves.Where(x => x.LineStyle.Name == "COLUMN").ToList();
            List<Line> columnLines = new List<Line>();
            foreach (CurveElement ce in columnCurves)
            {
                columnLines.Add(ce.GeometryCurve as Line);
            }


            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;


            // Grab the columntype
            FilteredElementCollector colColumns = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol));
            //    .OfCategory(BuiltInCategory.OST_Columns);
            // OST handles the internal family types, maybe?
            FamilySymbol column_demo = colColumns.FirstElement() as FamilySymbol;
            // Use default setting to avoid error handling, which is a lack of the line below
            //FamilySymbol column_demo = columnTypes.Find((FamilySymbol fs) => { return fs.Name == "Column_demo"});
            foreach (FamilySymbol columnType in colColumns)
            {
                //Debug.Print(columnType.Name);
                if (columnType.Name == "Column_demo")
                {
                    column_demo = columnType as FamilySymbol;
                    break;
                }
            }


            // Modify document within a transaction
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate Columns");

                // Column basepoint
                List<List<Line>> columnGroups = new List<List<Line>>();
                columnGroups.Add(new List<Line>() { });
                while (columnLines.Count != 0)
                {
                    foreach (Line element in columnLines)
                    {
                        int iterCounter = 0;
                        foreach (List<Line> sublist in columnGroups)
                        {
                            iterCounter += 1;
                            if (Algorithm.IsCrossing(element, sublist))
                            {
                                sublist.Add(element);
                                columnLines.Remove(element);
                                goto a;
                            }
                            if (iterCounter == columnGroups.Count)
                            {
                                columnGroups.Add(new List<Line>() { element });
                                columnLines.Remove(element);
                                goto a;
                            }
                        }
                    }
                a:;
                }

                // activate
                if (!column_demo.IsActive)
                {
                    column_demo.Activate();
                }

                // Column generation
                foreach (List<Line> baselines in columnGroups)
                {
                    XYZ columnCenterPt = Algorithm.GrabCenterPt(baselines);
                    doc.Create.NewFamilyInstance(columnCenterPt, column_demo, firstLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}

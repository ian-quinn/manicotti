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
    public static class ExtrudeColumn
    {
        public static void Execute(UIApplication uiapp, List<Line> columnLines, Level level)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            

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
                doc.Create.NewFamilyInstance(columnCenterPt, column_demo, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                //Autodesk.Revit.DB.Structure.StructuralType.Column
            }

        }
    }
}

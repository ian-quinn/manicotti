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
        // add new type of a family instance if not exist
        // M_Rectangular Column.rfa loaded by default is used here
        public static FamilySymbol CreateColumn(UIApplication uiapp, String familyName, double width, double depth)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            Family f = Util.GetFirstElementOfTypeNamed(doc, typeof(Family), familyName) as Family;
            if (null == f)
            {
                // add default path and error handling here
                if (!doc.LoadFamily("C:\\ProgramData\\Autodesk\\RVT 2020\\Libraries\\US Metric\\Columns\\M_Rectangular Column.rfa", out f))
                {
                    Debug.Print("Unable to load M_Rectangular Column.rfa");
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
                    if (s.Name == width.ToString() + " x " + depth.ToString() + "mm")
                    {
                        return s;
                    }
                }

                Debug.Assert(null != s, "expected at least one symbol to be defined in family");

                // Duplicate the existing symbol:
                s = s.Duplicate(width.ToString() + " x " + depth.ToString() + "mm") as FamilySymbol;

                // Analyse the symbol parameters:
                foreach (Parameter param in s.Parameters)
                {
                    Debug.Print("Parameter name={0}, value={1}", param.Definition.Name, param.AsValueString());
                }

                // Define new dimensions for our new type;
                // the specified parameter name is case sensitive:
                s.LookupParameter("Width").Set(Util.MmToFoot(width));
                s.LookupParameter("Depth").Set(Util.MmToFoot(depth));

                return s;
            }
            else
            {
                return null;
            }
        }

        public static void Execute(UIApplication uiapp, List<Line> columnLines, Level level)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

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

            // Grab the columntype
            FilteredElementCollector colColumns = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol));
            //    .OfCategory(BuiltInCategory.OST_Columns);
            // OST handles the internal family types, maybe?
            FamilySymbol rectangularColumn = colColumns.FirstElement() as FamilySymbol;
            // Use default setting to avoid error handling, which is a lack of the line below
            //FamilySymbol column_demo = columnTypes.Find((FamilySymbol fs) => { return fs.Name == "Column_demo"});
            foreach (FamilySymbol columnType in colColumns)
            {
                if (columnType.Name == "M_Rectangular Column")
                {
                    rectangularColumn = columnType as FamilySymbol;
                    break;
                }
            }
            
            /*
            // activate
            if (!rectangularColumn.IsActive)
            {
                rectangularColumn.Activate();
            }
            */

            // Column generation
            foreach (List<Line> baselines in columnGroups)
            {
                if (baselines.Count > 4) { continue; }  // can only process rectangular column for now
                var (width, depth, angle) = Algorithm.GrabSizeOfRectangle(baselines);
                FamilySymbol cs = CreateColumn(uiapp, "M_Rectangular Column", width, depth);
                XYZ columnCenterPt = Algorithm.GrabCenterPt(baselines);
                Line columnCenterAxis = Line.CreateBound(columnCenterPt, columnCenterPt.Add(-XYZ.BasisZ));
                // z pointing down to apply a clockwise rotation
                FamilyInstance fi = doc.Create.NewFamilyInstance(columnCenterPt, cs, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                ElementTransformUtils.RotateElement(doc, fi.Id, columnCenterAxis, angle);
                //Autodesk.Revit.DB.Structure.StructuralType.Column
            }
        }
    }
}

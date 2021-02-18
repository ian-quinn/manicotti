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
    public static class CreateColumn
    {
        // add new type of a family instance if not exist
        // M_Rectangular Column.rfa loaded by default is used here
        public static FamilySymbol NewRectColumnType(UIApplication uiapp, String familyName, double width, double depth)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            Family f = Util.GetFirstElementOfTypeNamed(doc, typeof(Family), familyName) as Family;
            if (null == f)
            {
                // add default path and error handling here
                if (!doc.LoadFamily(@"C:\ProgramData\Autodesk\RVT 2020\Libraries\US Metric\Columns\M_Rectangular Column.rfa", out f))
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

        public static FamilySymbol NewSpecialShapedColumnType(UIApplication uiapp, CurveArray boundary)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            
            Document familyDoc = app.NewFamilyDocument(@"C:\ProgramData\Autodesk\RVT 2020\Family Templates\English\Metric Column.rft");
            using (Transaction tx_createFamily = new Transaction(familyDoc))
            {
                tx_createFamily.Start("Create family");

                Plane familyGeomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(familyDoc, familyGeomplane);

                CurveArrArray curveArrArray = new CurveArrArray();
                curveArrArray.Append(boundary);
                // The end has to be 4000mm in metric which is in align with the Metric Column.rft
                Extrusion extrusion = familyDoc.FamilyCreate.NewExtrusion(true, curveArrArray, sketch, Util.MmToFoot(4000));
                familyDoc.FamilyManager.NewType("Type 0");
                familyDoc.Regenerate();

                Reference topFaceRef = null;
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.DetailLevel = ViewDetailLevel.Fine;
                GeometryElement gelm = extrusion.get_Geometry(opt);
                foreach (GeometryObject gobj in gelm)
                {
                    if (gobj is Solid)
                    {
                        Solid solid = gobj as Solid;
                        foreach (Face face in solid.Faces)
                        {
                            if (face.ComputeNormal(UV.Zero).IsAlmostEqualTo(XYZ.BasisZ))
                            {
                                topFaceRef = face.Reference;
                            }
                        }
                    }
                }
                View v = GetView(familyDoc);
                Reference r = GetUpperRefLevel(familyDoc);
                Dimension d = familyDoc.FamilyCreate.NewAlignment(v, r, topFaceRef);
                d.IsLocked = true;

                tx_createFamily.Commit();
            }
            //familyDoc.SaveAs("Special Shaped Column - " + boundary.Size.ToString() + ".rfa");
            //familyDoc.Close();

            Family f = familyDoc.LoadFamily(doc) as Family;
            FamilySymbol s = null;
            foreach (ElementId id in f.GetFamilySymbolIds())
            {
                s = doc.GetElement(id) as FamilySymbol;
            }
            return s;
        }

        public static View GetView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            View v = collector.OfClass(typeof(View)).First(m => m.Name == "Front") as View;
            return v;
        }

        public static Reference GetUpperRefLevel(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            Level lvl = collector.OfClass(typeof(Level)).First(m => m.Name == "Upper Ref Level") as Level;
            return new Reference(lvl);
        }
        


        // Main transaction
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
                        if (Algorithm.IsLineIntersectLines(element, sublist))
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
            

            // Column generation
            foreach (List<Line> baselines in columnGroups)
            {
                if (baselines.Count <= 1) { continue; }
                if (baselines.Count == 4) // SHIT CODE. Should check if the polygon is rectangle
                {
                    using (Transaction tx = new Transaction(doc))
                    {
                        tx.Start("Generate Columns");
                        var (width, depth, angle) = Algorithm.GetSizeOfRectangle(baselines);
                        FamilySymbol fs = NewRectColumnType(uiapp, "M_Rectangular Column", width, depth);
                        if (!fs.IsActive) { fs.Activate(); }
                        XYZ columnCenterPt = Algorithm.GetCenterPt(baselines);
                        Line columnCenterAxis = Line.CreateBound(columnCenterPt, columnCenterPt.Add(-XYZ.BasisZ));
                        // z pointing down to apply a clockwise rotation
                        FamilyInstance fi = doc.Create.NewFamilyInstance(columnCenterPt, fs, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        ElementTransformUtils.RotateElement(doc, fi.Id, columnCenterAxis, angle);
                        //Autodesk.Revit.DB.Structure.StructuralType.Column
                        tx.Commit();
                    }
                }
                if (baselines.Count > 4)
                {
                    var boundary = Algorithm.RectifyPolygon(baselines);
                    FamilySymbol fs = NewSpecialShapedColumnType(uiapp, boundary);
                    if (null == fs)
                    {
                        Debug.Print("Generic Model Family returns no symbol");
                        continue;
                    }
                    using (Transaction tx = new Transaction(doc))
                    {
                        tx.Start("Generate Columns");
                        if (!fs.IsActive) { fs.Activate(); }
                        XYZ basePt = XYZ.Zero;
                        Line columnBaseAxis = Line.CreateBound(basePt, basePt.Add(-XYZ.BasisZ));
                        FamilyInstance fi = doc.Create.NewFamilyInstance(basePt, fs, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        // DANGEROUS! the coordination transformation should be added here
                        tx.Commit();
                    }
                }
            }
        }
    }
}

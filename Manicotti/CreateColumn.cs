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
        public static FamilySymbol NewRectColumnType(UIApplication uiapp, string familyName, double width, double depth)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            
            Family f = Misc.GetFirstElementOfTypeNamed(doc, typeof(Family), familyName) as Family;
            if (null == f)
            {
                // add default path and error handling here
                if (!doc.LoadFamily(Properties.Settings.Default.url_column, out f))
                {
                    Debug.Print("Unable to load the default column");
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
                s.LookupParameter("Width").Set(Misc.MmToFoot(width));
                s.LookupParameter("Depth").Set(Misc.MmToFoot(depth));

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
            
            Document familyDoc = app.NewFamilyDocument(Properties.Settings.Default.url_columnFamily);
            using (Transaction tx_createFamily = new Transaction(familyDoc, "Create family"))
            {
                tx_createFamily.Start();

                Plane familyGeomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(familyDoc, familyGeomplane);

                CurveArrArray curveArrArray = new CurveArrArray();
                curveArrArray.Append(boundary);
                // The end has to be 4000mm in metric which is in align with the Metric Column.rft
                Extrusion extrusion = familyDoc.FamilyCreate.NewExtrusion(true, curveArrArray, sketch, Misc.MmToFoot(4000));
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
        public static void Execute(UIApplication uiapp, List<Curve> columnLines, string nameCollumn, Level level, bool IsSilent)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Sort out column boundary
            // Consider using CurveArray to store the data
            List<List<Curve>> columnGroups = Algorithm.ClusterByIntersect(columnLines);
            List<List<Curve>> columnRect = new List<List<Curve>>();
            List<List<Curve>> columnSpecialShaped = new List<List<Curve>>();
            foreach (List<Curve> columnGroup in columnGroups)
            {
                if (Algorithm.GetPtsOfCrvs(columnGroup).Count() == columnGroup.Count())
                {
                    if (Algorithm.IsRectangle(columnGroup))
                    {
                        columnRect.Add(columnGroup);
                    }
                    else
                    {
                        columnSpecialShaped.Add(columnGroup);
                    }
                }
            }
            Debug.Print("Got rectangle {0}, and unique shape {1}", columnRect.Count(), columnSpecialShaped.Count());


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
                if (columnType.Name == nameCollumn)
                {
                    rectangularColumn = columnType as FamilySymbol;
                    break;
                }
            }

            // I don't know if there's a better way to split this...
            // Column generation
            if (IsSilent == true)
            {
                using (Transaction tx = new Transaction(doc, "Generate rectangular columns"))
                {
                    FailureHandlingOptions options = tx.GetFailureHandlingOptions();
                    options.SetFailuresPreprocessor(new Util.FailureSwallower(false, false));
                    tx.SetFailureHandlingOptions(options);

                    tx.Start();
                    foreach (List<Curve> baselines in columnRect)
                    {
                        double width = Algorithm.GetSizeOfRectangle(Misc.CrvsToLines(baselines)).Item1;
                        double depth = Algorithm.GetSizeOfRectangle(Misc.CrvsToLines(baselines)).Item2;
                        double angle = Algorithm.GetSizeOfRectangle(Misc.CrvsToLines(baselines)).Item3;

                        FamilySymbol fs = NewRectColumnType(uiapp, nameCollumn, width, depth);
                        if (!fs.IsActive) { fs.Activate(); }
                        XYZ columnCenterPt = Algorithm.GetCenterPt(baselines);
                        Line columnCenterAxis = Line.CreateBound(columnCenterPt, columnCenterPt.Add(-XYZ.BasisZ));
                        // z pointing down to apply a clockwise rotation
                        FamilyInstance fi = doc.Create.NewFamilyInstance(columnCenterPt, fs, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        ElementTransformUtils.RotateElement(doc, fi.Id, columnCenterAxis, angle);
                        //Autodesk.Revit.DB.Structure.StructuralType.Column
                    }
                    tx.Commit();
                }

                // To generate the special shaped column you have to do it transaction by transaction
                // due to the family document reload operation must be out of one.
                foreach (List<Curve> baselines in columnSpecialShaped)
                {
                    var boundary = Algorithm.RectifyPolygon(Misc.CrvsToLines(baselines));
                    FamilySymbol fs = NewSpecialShapedColumnType(uiapp, boundary);
                    if (null == fs)
                    {
                        Debug.Print("Generic Model Family returns no symbol");
                        continue;
                    }
                    using (Transaction tx = new Transaction(doc, "Generate a special shaped column"))
                    {
                        tx.Start();
                        if (!fs.IsActive) { fs.Activate(); }
                        XYZ basePt = XYZ.Zero;
                        Line columnBaseAxis = Line.CreateBound(basePt, basePt.Add(-XYZ.BasisZ));
                        FamilyInstance fi = doc.Create.NewFamilyInstance(basePt, fs, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        // DANGEROUS! the coordination transformation should be added here
                        tx.Commit();
                    }
                }
            }

            else
            {
                string caption = "Extrude columns";
                string task = "Creating rectangular columns...";

                Views.ProgressBar pb1 = new Views.ProgressBar(caption, task, columnRect.Count);

                foreach (List<Curve> baselines in columnRect)
                {
                    using (Transaction tx = new Transaction(doc, "Generate a rectangular column"))
                    {
                        tx.Start();
                        
                        double width = Algorithm.GetSizeOfRectangle(Misc.CrvsToLines(baselines)).Item1;
                        double depth = Algorithm.GetSizeOfRectangle(Misc.CrvsToLines(baselines)).Item2;
                        double angle = Algorithm.GetSizeOfRectangle(Misc.CrvsToLines(baselines)).Item3;

                        FamilySymbol fs = NewRectColumnType(uiapp, nameCollumn, width, depth);
                        if (!fs.IsActive) { fs.Activate(); }
                        XYZ columnCenterPt = Algorithm.GetCenterPt(baselines);
                        Line columnCenterAxis = Line.CreateBound(columnCenterPt, columnCenterPt.Add(-XYZ.BasisZ));
                        // z pointing down to apply a clockwise rotation
                        FamilyInstance fi = doc.Create.NewFamilyInstance(columnCenterPt, fs, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        ElementTransformUtils.RotateElement(doc, fi.Id, columnCenterAxis, angle);
                        //Autodesk.Revit.DB.Structure.StructuralType.Column

                        tx.Commit();
                    }
                    pb1.Increment();
                    if (pb1.ProcessCancelled) { break; }
                }
                pb1.Close();

                task = "Creating special shaped columns...";
                Views.ProgressBar pb2 = new Views.ProgressBar(caption, task, columnSpecialShaped.Count);

                foreach (List<Curve> baselines in columnSpecialShaped)
                {
                    var boundary = Algorithm.RectifyPolygon(Misc.CrvsToLines(baselines));
                    FamilySymbol fs = NewSpecialShapedColumnType(uiapp, boundary);
                    if (null == fs)
                    {
                        Debug.Print("Generic Model Family returns no symbol");
                        continue;
                    }
                    using (Transaction tx = new Transaction(doc, "Generate a special shaped column"))
                    {
                        tx.Start();
                        if (!fs.IsActive) { fs.Activate(); }
                        XYZ basePt = XYZ.Zero;
                        Line columnBaseAxis = Line.CreateBound(basePt, basePt.Add(-XYZ.BasisZ));
                        FamilyInstance fi = doc.Create.NewFamilyInstance(basePt, fs, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        // DANGEROUS! the coordination transformation should be added here
                        tx.Commit();
                    }
                    pb2.Increment();
                    if (pb2.ProcessCancelled) { break; }
                }
                pb2.JobCompleted();
            }
            
        }
    }
}

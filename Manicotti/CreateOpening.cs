#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public static class CreateOpening
    {
        /// <summary>
        /// Create a new opening type of the default family. Metric here.
        /// </summary>
        /// <param name="uiapp"></param>
        /// <param name="familyName"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static FamilySymbol NewOpeningType(UIApplication uiapp, string familyName, 
            double width, double height, string type = "")
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            Family f = Util.GetFirstElementOfTypeNamed(doc, typeof(Family), familyName) as Family;
            if (null == f)
            {
                // add default path and error handling here
                if (type == "Door")
                {
                    if (!doc.LoadFamily(Properties.Settings.Default.url_door, out f))
                    {
                        Debug.Print("Unable to load M_Door-Single-Panel.rfa");
                    }
                }
                if (type == "Window")
                {
                    if (!doc.LoadFamily(Properties.Settings.Default.url_window, out f))
                    {
                        Debug.Print("Unable to load M_Window-Fixed.rfa");
                    }
                }
                if (type == "")
                {
                    Debug.Print("Please specify the type to load a default setting");
                }
            }

            if (null != f)
            {
                //Debug.Print("Family name={0}", f.Name);

                // Pick any symbol for duplication (for iteration convenient choose the last):
                FamilySymbol s = null;
                foreach (ElementId id in f.GetFamilySymbolIds())
                {
                    s = doc.GetElement(id) as FamilySymbol;
                    if (s.Name == width.ToString() + " x " + height.ToString() + "mm")
                    {
                        return s;
                    }
                }

                //Debug.Assert(null != s, "expected at least one symbol to be defined in family");

                // Duplicate the existing symbol:
                s = s.Duplicate(width.ToString() + " x " + height.ToString() + "mm") as FamilySymbol;

                // Analyse the symbol parameters:
                /*
                foreach (Parameter param in s.Parameters)
                {
                    Debug.Print("Parameter name={0}, value={1}", param.Definition.Name, param.AsValueString());
                }
                */

                // Define new dimensions for our new type;
                // the specified parameter name is case sensitive:
                s.LookupParameter("Width").Set(Util.MmToFoot(width));
                s.LookupParameter("Height").Set(Util.MmToFoot(height));

                return s;
            }
            else
            {
                return null;
            }
        }
        

        public static void Execute(UIApplication uiapp, List<Curve> doorCrvs, List<Curve> windowCrvs, 
            List<Curve> wallCrvs, List<UtilGetCADText.CADTextModel> labels, Level level)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            var doorClusters = Algorithm.ClusterByIntersect(doorCrvs);
            // Sometimes ClusterByIntersect will not return ideal result
            // because the intersected lines are not detected by the program for no reason
            // sometimes it goes well
            // iterate the bounding box generation 2 times may gurantee this but lacking efficiency
            List<List<Curve>> doorBlocks = new List<List<Curve>> { };
            foreach (List<Curve> cluster in doorClusters)
            {
                if (null != Algorithm.CreateBoundingBox2D(cluster))
                {
                    doorBlocks.Add(Algorithm.CreateBoundingBox2D(cluster));
                }
            }
            //Debug.Print("{0} clustered door blocks in total", doorBlocks.Count);

            List<Curve> doorAxes = new List<Curve> { };
            foreach (List<Curve> doorBlock in doorBlocks)
            {
                List<Curve> doorFrame = new List<Curve> { };
                for (int i = 0; i < doorBlock.Count; i++)
                {
                    int sectCount = 0;
                    List<Curve> fenses = new List<Curve>();
                    foreach (Curve line in wallCrvs)
                    {
                        Curve testCrv = doorBlock[i].Clone();
                        SetComparisonResult result = RegionDetect.ExtendCrv(testCrv, 0.01).Intersect(line,
                                                   out IntersectionResultArray results);
                        if (result == SetComparisonResult.Overlap)
                        {
                            sectCount += 1;
                            fenses.Add(line);
                        }
                    }
                    if (sectCount == 2)
                    {
                        XYZ projecting = fenses[0].Evaluate(0.5, true);
                        XYZ projected = fenses[1].Project(projecting).XYZPoint;
                        if (fenses[0].Length > fenses[1].Length)
                        {
                            projecting = fenses[1].Evaluate(0.5, true);
                            projected = fenses[0].Project(projecting).XYZPoint;
                        }
                        Line doorAxis = Line.CreateBound(projecting, projected);
                        doorAxes.Add(doorAxis);
                        //doorAxes.Add(doorBlock[i]);
                    }
                }
                //Debug.Print("Curves adjoning the box: " + doorFrame.Count.ToString());
            }
            Debug.Print("We got {0} door axes. ", doorAxes.Count);


            // Collect window blocks
            var windowClusters = Algorithm.ClusterByIntersect(windowCrvs);

            List<List<Curve>> windowBlocks = new List<List<Curve>> { };
            foreach (List<Curve> cluster in windowClusters)
            {
                if (null != Algorithm.CreateBoundingBox2D(cluster))
                {
                    windowBlocks.Add(Algorithm.CreateBoundingBox2D(cluster));
                }
            }
            //Debug.Print("{0} clustered window blocks in total", windowBlocks.Count);

            List<Curve> windowAxes = new List<Curve> { };
            foreach (List<Curve> windowBlock in windowBlocks)
            {
                Line axis1 = Line.CreateBound((windowBlock[0].GetEndPoint(0) + windowBlock[0].GetEndPoint(1)).Divide(2),
                    (windowBlock[2].GetEndPoint(0) + windowBlock[2].GetEndPoint(1)).Divide(2));
                Line axis2 = Line.CreateBound((windowBlock[1].GetEndPoint(0) + windowBlock[1].GetEndPoint(1)).Divide(2),
                    (windowBlock[3].GetEndPoint(0) + windowBlock[3].GetEndPoint(1)).Divide(2));
                if (axis1.Length > axis2.Length)
                {
                    windowAxes.Add(axis1);
                }
                else
                {
                    windowAxes.Add(axis2);
                }
            }
            Debug.Print("We got {0} window axes. ", windowAxes.Count);


            // Main transaction
            // Plot axis of doors/windows and create the instance
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate sub-surface and its mark");

                /*
                Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(doc, Geomplane);

                // Create door & window blocks
                
                foreach (List<Curve> doorBlock in doorBlocks)
                {
                    //Debug.Print("Creating new bounding box");
                    foreach (Curve edge in doorBlock)
                    {
                        ModelCurve modelline = doc.Create.NewModelCurve(edge, sketch) as ModelCurve;
                    }
                }
                foreach (List<Curve> windowBlock in windowBlocks)
                {
                    //Debug.Print("Creating new bounding box");
                    foreach (Curve edge in windowBlock)
                    {
                        ModelCurve modelline = doc.Create.NewModelCurve(edge, sketch) as ModelCurve;
                    }
                }
                */
                


                // Create door axis & instance
                foreach (Curve doorAxis in doorAxes)
                {
                    /*
                    DetailLine axis = doc.Create.NewDetailCurve(view, doorAxis) as DetailLine;
                    GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor = new Color(202, 51, 82);
                    gs.GraphicsStyleCategory.SetLineWeight(7, gs.GraphicsStyleType);
                    */

                    Wall hostWall = Wall.Create(doc, doorAxis, level.Id, true);

                    double width = Math.Round(Util.FootToMm(doorAxis.Length), 0);
                    double height = 2000;
                    XYZ basePt = (doorAxis.GetEndPoint(0) + doorAxis.GetEndPoint(1)).Divide(2);
                    XYZ insertPt = basePt + XYZ.BasisZ * level.Elevation; // Absolute height
                    double span = Double.PositiveInfinity;
                    int labelId = -1;
                    foreach (UtilGetCADText.CADTextModel text in labels)
                    {
                        double distance = basePt.DistanceTo(text.Location);
                        if (distance < span)
                        {
                            span = distance;
                            labelId = labels.IndexOf(text);
                        }
                    }
                    if (labelId > -1)
                    {
                        width = UtilGetCADText.DecodeLabel(labels[labelId].Text, width, height).Item1;
                        height = UtilGetCADText.DecodeLabel(labels[labelId].Text, width, height).Item2;
                        //Debug.Print("Raw window label: {0}", labels[labelId].Text);
                    }

                    Debug.Print("Create new door with dimension {0}x{1}", width.ToString(), height.ToString());
                    FamilySymbol fs = NewOpeningType(uiapp, "M_Single-Flush", width, height, "Door");
                    if (null == fs) { continue; }
                    if (!fs.IsActive) { fs.Activate(); }

                    FamilyInstance fi = doc.Create.NewFamilyInstance(insertPt, fs, hostWall, level,
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                }

                // Create window axis & instance
                foreach (Curve windowAxis in windowAxes)
                {
                    /*
                    DetailLine axis = doc.Create.NewDetailCurve(view, windowAxis) as DetailLine;
                    GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.LineColor = new Color(202, 51, 82);
                    gs.GraphicsStyleCategory.SetLineWeight(7, gs.GraphicsStyleType);
                    */

                    Wall hostWall = Wall.Create(doc, windowAxis, level.Id, true);

                    double width = Math.Round(Util.FootToMm(windowAxis.Length), 0);
                    double height = 2500;

                    XYZ basePt = (windowAxis.GetEndPoint(0) + windowAxis.GetEndPoint(1)).Divide(2); // On world plane
                    XYZ insertPt = basePt + XYZ.BasisZ * (Util.MmToFoot(Properties.Settings.Default.sillHeight) + level.Elevation); // Absolute height
                    double span = Util.MmToFoot(2000);
                    int labelId = -1;
                    foreach (UtilGetCADText.CADTextModel text in labels)
                    {
                        double distance = basePt.DistanceTo(text.Location);
                        //Debug.Print("Compare the two pts: " + Util.PrintXYZ(basePt) + " " + Util.PrintXYZ(text.Location));
                        if (distance < span)
                        {
                            span = distance;
                            labelId = labels.IndexOf(text);
                        }
                    }
                    if (labelId > -1)
                    {
                        width = UtilGetCADText.DecodeLabel(labels[labelId].Text, width, height).Item1;
                        height = UtilGetCADText.DecodeLabel(labels[labelId].Text, width, height).Item2;
                        if (height + Properties.Settings.Default.sillHeight > Properties.Settings.Default.floorHeight)
                        { height = Properties.Settings.Default.floorHeight - Properties.Settings.Default.sillHeight; }
                        //Debug.Print("Raw window label: {0}", labels[labelId].Text);
                    }

                    Debug.Print("Create new window with dimension {0}x{1}", width.ToString(), height.ToString());
                    FamilySymbol fs = NewOpeningType(uiapp, "M_Fixed", width, height, "Window");
                    if (null == fs) { continue; }
                    if (!fs.IsActive) { fs.Activate(); }

                    FamilyInstance fi = doc.Create.NewFamilyInstance(insertPt, fs, hostWall, level,
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                }

                tx.Commit();
            }
        }
    }
}

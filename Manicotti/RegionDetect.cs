using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class RegionDetect : IExternalCommand
    {
        // Print list for debug
        public static string printo(List<int> list)
        {
            string fusion = "";
            for (int index = 0; index < list.Count(); index++)
            {
                fusion = fusion + list[index].ToString() + " ";
            }
            return fusion;
        }

        public static string printb(List<bool> list)
        {
            string fusion = "";
            for (int index = 0; index < list.Count(); index++)
            {
                fusion = fusion + list[index].ToString() + " ";
            }
            return fusion;
        }

        public static string printd(Dictionary<int, List<Curve>> dictionary)
        {
            string fusion = "";
            foreach (KeyValuePair<int, List<Curve>> kvp in dictionary)
            {
                fusion = fusion + kvp.Key.ToString() + "-" + kvp.Value.Count().ToString() + " ";
            }
            return fusion;
        }

        // Perform curve splitting according to raw intersection parameters
        public static List<Curve> SplitCrv(Curve parent, List<double> parameters)
        {
            double threshold = 0.00001;
            List<Curve> segments = new List<Curve>();
            parameters.Add(parent.GetEndParameter(1));
            parameters.Insert(0, parent.GetEndParameter(0));
            double[] params_ordered = parameters.ToArray();
            Array.Sort(params_ordered);
            List<double> params_rectified = new List<double>();
            params_rectified.Add(params_ordered[0]);
            // Cautiously add this judgement in case too small segements
            // If a parameter inside the array too close to the previous one
            // Delete it
            for (int paraId = 1; paraId < params_ordered.Length; paraId++)
            {
                if (params_ordered[paraId] - params_rectified.Last() > threshold)
                {
                    params_rectified.Add(params_ordered[paraId]);
                }
            }
            for (int index = 0; index < params_rectified.Count - 1; index++)
            {
                Curve segment = parent.Clone();
                segment.MakeBound(params_rectified[index], params_rectified[index + 1]);
                segments.Add(segment);
            }
            //Debug.Print("Add new shatters " + segments.Count().ToString());
            return segments;
        }

        // Absolute angle from curve to curve ONLY on basic horizontal plan
        public static double AngleBetweenCrv(Curve crv1, Curve crv2, XYZ axis)
        {
            XYZ pt_origin = new XYZ();
            Line line1 = crv1 as Line;
            Line line2 = crv2 as Line;
            XYZ vec1 = line1.Direction.Normalize();
            XYZ vec2 = line2.Direction.Normalize();
            //XYZ vec1 = crv1.ComputeDerivatives(crv1.GetEndParameter(0), true).OfPoint(pt_origin);
            //XYZ vec2 = crv2.ComputeDerivatives(crv2.GetEndParameter(0), true).OfPoint(pt_origin);
            XYZ testVec = vec1.CrossProduct(vec2).Normalize();
            double testAngle = vec1.AngleTo(vec2);
            if (testVec.IsAlmostEqualTo(axis.Normalize()))
            {
                return testAngle;
            }
            else
            {
                return 2 * Math.PI - testAngle;
            }
        }

        // Shatter a bunch of curves according to their intersections 
        public static List<Curve> ExplodeCrv(List<Curve> C)
        {
            // Start by Shattering all of your input curves by intersecting them with each other
            List<Curve> shatters = new List<Curve>();
            for (int CStart = 0; CStart <= C.Count - 1; CStart++)
            {
                List<double> breakParams = new List<double>();
                for (int CCut = 0; CCut <= C.Count - 1; CCut++)
                {
                    if (CStart != CCut)
                    {
                        SetComparisonResult result = C[CStart].Intersect(C[CCut], out IntersectionResultArray results);
                        if (result != SetComparisonResult.Disjoint)
                        {
                            //XYZ breakPt = results.get_Item(0).XYZPoint;
                            //Debug.Print("The intersection point is (" + breakPt.X.ToString() + ", " + breakPt.Y.ToString() + ")");
                            //double breakParam = C[CStart].Project(breakPt).Parameter;
                            // Another way to get the intersection parameter
                            double breakParam = results.get_Item(0).UVPoint.U;
                            breakParams.Add(breakParam);
                            //Debug.Print("Projection parameter is: " + breakParam.ToString());
                            //Debug.Print("The raw parameter of the No." + CStart.ToString() + " intersection is " + breakParam.ToString());
                        }
                    }
                }
                shatters.AddRange(SplitCrv(C[CStart], breakParams));
            }
            return shatters;
        }

        // Check if 2 lines are the same by comparing endpoints
        public static bool CompareLines(Curve crv1, Curve crv2)
        {
            double bias = 0.00001;
            Line line1 = crv1 as Line;
            Line line2 = crv2 as Line;
            XYZ startPt1 = line1.GetEndPoint(0);
            XYZ endPt1 = line1.GetEndPoint(1);
            XYZ startPt2 = line2.GetEndPoint(0);
            XYZ endPt2 = line2.GetEndPoint(1);
            if ((startPt1.IsAlmostEqualTo(startPt2, bias) && endPt1.IsAlmostEqualTo(endPt2, bias)) 
                || (startPt1.IsAlmostEqualTo(endPt2, bias) && endPt1.IsAlmostEqualTo(startPt2, bias)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Flatten a list of CurveArray and dump the duplicate ones
        // not a universal method. just a after-process of regionCluster()
        public static Tuple<List<Curve>, List<Curve>> FlattenLines(List<CurveArray> polys)
        {
            List<Curve> curvePool = new List<Curve>();
            List<Curve> curveMesh = new List<Curve>();
            List<Curve> curveBoundary = new List<Curve>();
            foreach (CurveArray poly in polys)
            {
                foreach (Curve polyline in poly)
                {
                    curvePool.Add(polyline);
                }
            }
            List<int> meshList = new List<int>();
            List<int> killList = new List<int>();
            for (int i = 0; i < curvePool.Count(); i++)
            {
                for (int j = i + 1; j < curvePool.Count(); j++)
                {
                    if (CompareLines(curvePool[i], curvePool[j]))
                    {
                        meshList.Add(i);
                        killList.Add(i);
                        killList.Add(j);
                    }
                }
            }
            for (int i = 0; i < curvePool.Count(); i++)
            {
                if (meshList.Contains(i) || !killList.Contains(i)) { curveMesh.Add(curvePool[i]); }
                if (!killList.Contains(i)) { curveBoundary.Add(curvePool[i]); }
            }
            return Tuple.Create(curveMesh, curveBoundary);
        }

        public static List<Curve> GetBoundary(List<CurveArray> polys)
        {
            bool IsSame(Curve crv1, Curve crv2)
            {
                XYZ start1 = crv1.GetEndPoint(0);
                XYZ start2 = crv2.GetEndPoint(0);
                XYZ end1 = crv1.GetEndPoint(1);
                XYZ end2 = crv2.GetEndPoint(1);
                if (start1.IsAlmostEqualTo(start2) && end1.IsAlmostEqualTo(end2) || 
                    start1.IsAlmostEqualTo(end2) && start2.IsAlmostEqualTo(end1))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            List<Curve> shatters = new List<Curve>();
            List<Curve> boundary = new List<Curve>();
            foreach (CurveArray poly in polys)
            {
                for (int i = 0; i < poly.Size; i++)
                {
                    shatters.Add(poly.get_Item(i));
                }
            }
            Debug.Print("Shatters in all: " + shatters.Count.ToString());
            for (int i = 0; i < shatters.Count; i++)
            {
                for (int j = 0; j < shatters.Count; j++)
                {
                    if (j != i)
                    {
                        if (IsSame(shatters[i], shatters[j]))
                        {
                            goto a;
                        }
                    }
                }
                boundary.Add(shatters[i]);
            a:;
            }
            Debug.Print("Boundry size: " + boundary.Count.ToString());
            return boundary;
        }

        // Create CurveArray from List<Curve>. not a universal method. only after regionCluster()
        public static CurveArray AlignCrv(List<Curve> polylines)
        {
            int lineNum = polylines.Count;
            CurveArray polygon = new CurveArray();
            polygon.Append(polylines[0]);
            polylines.RemoveAt(0);
            while (polygon.Size < lineNum)
            {
                XYZ endPt = polygon.get_Item(polygon.Size - 1).GetEndPoint(1);
                for (int i = 0; i < polylines.Count; i++)
                {
                    if (polylines[i].GetEndPoint(0).IsAlmostEqualTo(endPt))
                    {
                        polygon.Append(polylines[i]);
                        polylines.Remove(polylines[i]);
                        break;
                    }
                    if (polylines[i].GetEndPoint(1).IsAlmostEqualTo(endPt))
                    {
                        polygon.Append(polylines[i].CreateReversed());
                        polylines.Remove(polylines[i]);
                        break;
                    }
                }
            }
            return polygon;
        }

        /// <summary>
        /// Check point in polygon containment by vector crossproduct
        /// Only apply to cw or ccw polycurve array
        /// </summary>
        /// <param name="crvArr"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static bool PointInPoly(CurveArray crvArr, XYZ pt)
        {
            int judgement = 0;
            int counter = 0;
            foreach(Curve crv in crvArr)
            {
                XYZ ptstart = crv.GetEndPoint(0);
                XYZ ptend = crv.GetEndPoint(1);
                XYZ direction = (pt - ptstart).CrossProduct(pt - ptend).Normalize();
                if (direction.IsAlmostEqualTo(new XYZ(0,0,1)))
                {
                    judgement += 1;
                }
                counter += 1;
            }
            if (judgement < counter){ return false; }
            else { return true; }
        }

        public static CurveArray PolyLineToCurveArray(PolyLine poly, double tolerance)
        {
            var vertices = poly.GetCoordinates();
            CurveArray shatters = new CurveArray();
            for (int i = 0; i < vertices.Count() - 1; i++)
            {
                if ((vertices[i + 1] - vertices[i]).GetLength() >= tolerance)
                {
                    shatters.Append(Line.CreateBound(vertices[i], vertices[i + 1]) as Curve);
                }
                else
                {
                    continue;
                }
            }
            return shatters;
        }

        public static bool PolyInPoly(PolyLine test, CurveArray target)
        {
            int judgement = 0;
            int counter = 0;
            var testPts = test.GetCoordinates();
            foreach(XYZ testPt in testPts)
            {
                if (PointInPoly(target, testPt))
                {
                    judgement += 1;
                }
                counter += 1;
            }
            if (judgement < counter) { return false; }
            else { return true; }
        }

        public static XYZ PolyCentPt(PolyLine poly)
        {
            var vertices = poly.GetCoordinates();
            int counter;
            XYZ sumPt = XYZ.Zero;
            if (vertices[0].IsAlmostEqualTo(vertices.Last()))
            {
                counter = vertices.Count() - 1;
            }
            else
            {
                counter = vertices.Count();
            }
            
            for (int i = 0; i < counter; i++)
            {
                sumPt += vertices[i];
            }
            return sumPt.Divide(counter);
        }

        /// <summary>
        /// Extend a curve by 1% (centroid based)
        /// </summary>
        /// <param name="crv">A Curve</param>
        /// <returns>An Extended Curve</returns>
        public static Curve ExtendCrv(Curve crv, double ratio)
        {
            double pstart = crv.GetEndParameter(0);
            double pend = crv.GetEndParameter(1);
            double pdelta = ratio * (pend - pstart);

            crv.MakeUnbound();
            crv.MakeBound(pstart - pdelta, pend + pdelta);
            return crv;
        }


        /// <summary>
        /// Generate conter-clockwise ordered CurveArray representing enclosed areas based on a bunch of intersected lines
        /// </summary>
        /// <param name="C"></param>
        /// <returns></returns>

        public static List<CurveArray> RegionCluster(List<Curve> C) // , Plane P, ref object CN
        {
            List<Curve> Crvs = new List<Curve>();

            for (int CStart = 0; CStart <= C.Count - 1; CStart++)
            {
                List<double> breakParams = new List<double>();
                for (int CCut = 0; CCut <= C.Count - 1; CCut++)
                {
                    if (CStart != CCut)
                    {
                        // ExtendCrv(C[CCut], 0.005)
                        SetComparisonResult result = C[CStart].Intersect(C[CCut], out IntersectionResultArray results);
                        if (result != SetComparisonResult.Disjoint)
                        {
                            double breakParam = results.get_Item(0).UVPoint.U;
                            breakParams.Add(breakParam);
                            //Debug.Print("Projection parameter is: " + breakParam.ToString());
                        }
                    }
                }
                Crvs.AddRange(SplitCrv(C[CStart], breakParams));
            }

            List<XYZ> Vtc = new List<XYZ>(); // all unique vertices
            List<Curve> HC = new List<Curve>(); // list of all shattered half-curves
            List<int> HCI = new List<int>(); // half curve indices
            List<int> HCO = new List<int>(); // half curve reversed
            List<int> HCN = new List<int>(); // next index for each half-curve (conter-clockwise)
            List<int> HCV = new List<int>(); // vertex representing this half-curve
            List<int> HCF = new List<int>(); // half-curve face
            List<bool> HCK = new List<bool>(); // mark if a half-curve needs to be killed
            // (if it either starts or ends hanging, but does not exclude redundant curves that not exclosing a room)
            Dictionary<int, List<Curve>> F = new Dictionary<int, List<Curve>>(); // data tree for faces
            Dictionary<int, List<int>> VOut = new Dictionary<int, List<int>>(); // data tree of outgoing half-curves from each vertex

            foreach (Curve Crv in Crvs) // cycle through each curve
            {
                for (int CRun = 0; CRun <= 2; CRun += 2) // create two half-curves: first in one direction, and then the other...
                {
                    XYZ testedPt = new XYZ();
                    if (CRun == 0) {
                        HC.Add(Crv);
                        testedPt = Crv.GetEndPoint(0);
                    } else {
                        HC.Add(Crv.CreateReversed());
                        testedPt = Crv.GetEndPoint(1);
                    }
                    HCI.Add(HCI.Count); // count this iteration
                    HCO.Add(HCI.Count - CRun); // a little index trick
                    HCN.Add(-1);
                    HCF.Add(-1);
                    HCK.Add(false);

                    int VtcSet = -1;

                    for (int VtxCheck = 0; VtxCheck <= Vtc.Count - 1; VtxCheck++)
                    {
                        if (Vtc[VtxCheck].DistanceTo(testedPt) < 0.0000001)
                        {
                            VtcSet = VtxCheck; // get the vertex index, if it already exists
                            break;
                        }
                    }

                    if (VtcSet > -1)
                    {
                        HCV.Add(VtcSet); // If the vertex already exists, set the half-curve vertex
                        VOut[VtcSet].Add(HCI.Last());
                    }
                    else
                    {
                        HCV.Add(Vtc.Count); // if the vertex doesn't already exist, add a new vertex index
                        VOut.Add(Vtc.Count, new List<int>() { HCI.Last() });
                        // add the new half-curve index to the list of outgoing half-curves associated with the vertex
                        Vtc.Add(testedPt);
                        // add the new vertex to the vertex list
                    }
                    Crv.CreateReversed(); // reverse the curve for creating the opposite half-curve in the second part of the loop
                    //Debug.Print("Tested point is (" + testedPt.X.ToString() + ", " + testedPt.Y.ToString() + ")");
                }
            }


            // For each Vertex that has only one outgoing half-curve, kill the half-curve and its opposite
            foreach (KeyValuePair<int, List<int>> path in VOut)
            {
                //Debug.Print("This point has been connected to " + path.Value.Count.ToString() + " curves");
                if (path.Value.Count == 1)
                {
                    HCK[path.Value[0]] = true;
                    HCK[HCO[path.Value[0]]] = true;
                }
            }
            Debug.Print("Elements inside Crvs are " + Crvs.Count.ToString());
            Debug.Print("Elements inside HC are " + HC.Count.ToString());
            Debug.Print("Elements inside VOut are " + VOut.Count.ToString());
            Debug.Print("Elements inside HCK are " + HCK.Count.ToString());
            //Debug.Print(printb(HCK));


            // Find the "next" half-curve for each starting half curve by identifying the outgoing half-curve from the end vertex
            // that presents the smallest angle by calculating its plane's x-axis angle from x-axis of the starting half-curve's opposite plane
            foreach (int HCIdx in HCI)
            {
                int minIdx = -1;
                double minAngle = 2 * Math.PI;
                //Debug.Print(VOut[HCV[HCO[HCIdx]]].Count().ToString());
                foreach (int HCOut in VOut[HCV[HCO[HCIdx]]])
                {
                    if (HCOut != HCO[HCIdx] & HCK[HCIdx] == false & HCK[HCOut] == false)
                    {
                        double testAngle = AngleBetweenCrv(HC[HCOut], HC[HCO[HCIdx]], new XYZ(0, 0, 1));
                        // The comparing order is important to ensure a right-hand angle under z-axis
                        if (testAngle < minAngle)
                        {
                            minIdx = HCOut;
                            minAngle = testAngle;
                        }
                    }
                }
                HCN[HCIdx] = minIdx;
            }

            Debug.Print("Elements inside HCI are " + HCI.Count.ToString());
            Debug.Print("Elements inside HCN are " + HCN.Count.ToString());
            Debug.Print("Elements in HCN: " + printo(HCN));

            // Sequence half-curves into faces by running along "next" half-curves in order until the starting half-curve is returned to
            List<int> FaceEdges = new List<int>();
            List<int> DeleteEdges = new List<int>();

            // cycle through each half-curve
            foreach (int HCIdx in HCI)
            {
                int EmExit = 0;
                if (HCF[HCIdx] == -1)
                {
                    int EdgeCounter = 1;
                    int FaceIdx = F.Count();
                    int CurrentIdx = HCIdx;
                    F.Add(FaceIdx, new List<Curve>() { HC[CurrentIdx] });
                    HCF[CurrentIdx] = FaceIdx;
                    do
                    {
                        if (HCN[CurrentIdx] == -1)
                        {
                            DeleteEdges.Add(FaceIdx);
                            break;
                        }

                        CurrentIdx = HCN[CurrentIdx];
                        F[FaceIdx].Add(HC[CurrentIdx]);
                        EdgeCounter += 1;
                        HCF[CurrentIdx] = FaceIdx;
                        if (HCN[CurrentIdx] == HCIdx)
                            break;
                        EmExit += 1;
                        if (EmExit == Crvs.Count - 1)
                            break;
                    }
                    while (true);
                    // exit once the starting half-curve is reached again
                    // emergency exit prevents infinite loops
                    FaceEdges.Add(EdgeCounter);
                }
            }
            //Debug.Print("Elements in FaceEdges: " + printo(FaceEdges));
            //Debug.Print("Elements in DeleteEdges: " + printo(DeleteEdges));
            //Debug.Print("Elements in F dict: " + printd(F));

            // Find the perimeter by counting edges of a region
            int Perim = -1;
            int PerimCount = -1;
            for (int FE = 0; FE <= FaceEdges.Count - 1; FE++)
            {
                if (FaceEdges[FE] > PerimCount)
                {
                    Perim = FE;
                    PerimCount = FaceEdges[FE];
                }
            }
            DeleteEdges.Add(Perim);

            int NewPath = 0;
            List<CurveArray> OutputFaces = new List<CurveArray>();

            // only output those faces that haven't been identified as either the perimeter or open
            foreach (KeyValuePair<int, List<Curve>> kvp in F)
            {
                if (DeleteEdges.Contains(kvp.Key) == false)
                {
                    CurveArray tempLoop = new CurveArray();
                    foreach (Curve element in kvp.Value)
                    {
                        tempLoop.Append(element);
                    }
                    OutputFaces.Add(tempLoop);
                    NewPath += 1;
                }
            }
            // Debug.Print("Elements in OutputFaces dict: " + printd(OutputFaces));

            return OutputFaces;
        }


        /// <summary>
        /// Create wall system, floor, and allocate rooms
        /// </summary>

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            Autodesk.Revit.Creation.Application appCreation = app.Create;
            Autodesk.Revit.Creation.Document docCreation = doc.Create;

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
            var strayCurves = importCurves.Where(x => x.LineStyle.Name == "WALL").ToList();
            List<Curve> strayLines = new List<Curve>();
            foreach (CurveElement ce in strayCurves)
            {
                strayLines.Add(ce.GeometryCurve as Line);
            }


            // Grab the current building level
            FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.INVALID)
                .OfClass(typeof(Level));
            Level firstLevel = colLevels.FirstElement() as Level;


            // Grab the building view
            // This may be useful when handling room allocation on separate levels
            //FilteredElementCollector colViews = new FilteredElementCollector(doc)
            //    .OfClass(typeof(View));
            //View firstView = colViews.FirstElement() as View;


            // Grab the building floortype
            FloorType floorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .First<Element>(e => e.Name.Equals("Generic 150mm")) as FloorType;


            // Modify document within a transaction
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Generate Walls");

                List<CurveArray> curveGroup = RegionCluster(strayLines);
                //var (mesh, perimeter) = FlattenLines(curveGroup);

                Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(doc, Geomplane);

                foreach (CurveArray group in curveGroup)
                {
                    foreach (Curve edge in group)
                    {
                        DetailLine axis = doc.Create.NewDetailCurve(view, edge) as DetailLine;
                        GraphicsStyle gs = axis.LineStyle as GraphicsStyle;
                        gs.GraphicsStyleCategory.LineColor = new Color(202, 51, 82);
                        gs.GraphicsStyleCategory.SetLineWeight(7, gs.GraphicsStyleType);
                    }
                }
                
                
                /*
                 * Region detect and boolean union all have bugs to fix
                 * DO NOT TEST THIS ONE
                 * 
                 * 
                 * 
                // Wall generation
                foreach (Curve wallAxis in mesh)
                {
                    Wall.Create(doc, wallAxis, firstLevel.Id, true);
                }

                // Create.NewRoom will automatically detect the topology according to the wall system
                // Don't bother sketching boundary lines unless you're handling a virtual room
                //docCreation.NewSpaceBoundaryLines(doc.ActiveView.SketchPlane, mesh, doc.ActiveView);
                doc.Regenerate();

                PlanTopology planTopology = doc.get_PlanTopology(firstLevel);
                if (doc.ActiveView.ViewType == ViewType.FloorPlan)
                {
                    foreach (PlanCircuit circuit in planTopology.Circuits)
                    {
                        if (null != circuit && !circuit.IsRoomLocated)
                        {
                            var room = doc.Create.NewRoom(null, circuit);
                            //Debug.Print("New room created!");
                        }
                    }
                }
                /* // add error handling here
                else
                {
                    System.Windows.Forms.MessageBox.Show("You can not create spaces in this plan view");
                }
                */
                /*
                Floor newFloor = doc.Create.NewFloor(AlignCrv(perimeter), floorType, firstLevel, false, XYZ.BasisZ);
                newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(0);
                */

                tx.Commit();
            }
            

            return Result.Succeeded;
        }

    }
}
